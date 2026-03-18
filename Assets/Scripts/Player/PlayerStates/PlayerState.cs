using UnityEngine;

// ===============================
// 🔵 Base Player State
// ===============================
public abstract class PlayerState
{
    public virtual string Name => GetType().Name;

    protected PlayerController player;
    protected PlayerStateMachine stateMachine;
    protected Rigidbody2D rb;

    protected PlayerState(PlayerController player, PlayerStateMachine stateMachine)
    {
        this.player = player;
        this.stateMachine = stateMachine;
        this.rb = player.Rigidbody;
    }

    public virtual void Enter() { }
    public virtual void Exit() { }
    public virtual void HandleInput() { }
    public virtual void Update() { }
    public virtual void PhysicsUpdate() { }
}

// ===============================
// 🟢 Idle State
// ===============================
public class PlayerIdleState : PlayerState
{
    public PlayerIdleState(PlayerController player, PlayerStateMachine stateMachine)
        : base(player, stateMachine) { }

    public override void HandleInput()
    {
        if (InputManager.Instance.MoveInput != Vector2.zero)
        {
            stateMachine.ChangeState(player.MoveState);
            return;
        }

        if (InputManager.Instance.AttackPressed)
        {
            InputManager.Instance.ConsumeAttackInput();

            if (player.CurrentWeapon is RangedWeapon)
            {
                stateMachine.ChangeState(player.ShootState);
            }
            // Eğer hiç silah yoksa veya sadece melee weapon varsa MeleeState'e geç
            else
            {
                stateMachine.ChangeState(player.MeleeState);
            }

        }

    }

    public override void PhysicsUpdate()
    {
        rb.linearVelocity = Vector2.zero;
        player.RotateTowardsAim();
    }
}

// ===============================
// 🔵 Move State
// ===============================
public class PlayerMoveState : PlayerState
{
    public PlayerMoveState(PlayerController player, PlayerStateMachine stateMachine)
        : base(player, stateMachine) { }

    public override void HandleInput()
    {
        if (InputManager.Instance.MoveInput == Vector2.zero)
        {
            stateMachine.ChangeState(player.IdleState);
            return;
        }

        if (InputManager.Instance.AttackPressed)
        {
            InputManager.Instance.ConsumeAttackInput();

            if (player.CurrentWeapon is RangedWeapon)
            {
                stateMachine.ChangeState(player.ShootState);
            }
            else
            {
                stateMachine.ChangeState(player.MeleeState);
            }

        }

    }

    public override void PhysicsUpdate()
    {
        Vector2 input = InputManager.Instance.MoveInput.normalized;
        rb.linearVelocity = input * player.MoveSpeed;
        player.RotateTowardsAim();
    }
}

// ===============================
// 🔫 Shoot State
// ===============================
public class PlayerShootState : PlayerState
{
    private float timer;

    public PlayerShootState(PlayerController player, PlayerStateMachine stateMachine)
        : base(player, stateMachine) { }

    public override void Enter()
    {
        timer = 0f;
        PlayerEvents.RaiseShoot(player.transform.position, player.AimDirection);
    }

    public override void Update()
    {
        timer += Time.deltaTime;

        if (timer >= player.ShootDuration)
        {
            if (InputManager.Instance.MoveInput != Vector2.zero)
                stateMachine.ChangeState(player.MoveState);
            else
                stateMachine.ChangeState(player.IdleState);
        }
    }

    public override void PhysicsUpdate()
    {
        Vector2 input = InputManager.Instance.MoveInput.normalized;
        rb.linearVelocity = input * player.MoveSpeed;
        player.RotateTowardsAim();
    }

}

// ===============================
// 🪓 Melee State
// ===============================
public class PlayerMeleeState : PlayerState
{
    private float timer;

    public PlayerMeleeState(PlayerController player, PlayerStateMachine stateMachine)
        : base(player, stateMachine) { }

    public override void Enter()
    {
        timer = 0f;

        if (player.HasWeapon && player.CurrentWeapon is IWeapon weapon)
        {
            weapon.Use(); // Silah varsa kullan
        }
        else
        {
            MeleeAttackHandler.DoAttack(player); // Silah yoksa el ile saldır
        }

        PlayerEvents.RaiseMeleeAttack(player.transform.position, player.AimDirection);
    }



    public override void Update()
    {
        timer += Time.deltaTime;

        if (timer >= player.MeleeDuration)
        {
            if (InputManager.Instance.MoveInput != Vector2.zero)
                stateMachine.ChangeState(player.MoveState);
            else
                stateMachine.ChangeState(player.IdleState);
        }
    }

    public override void PhysicsUpdate()
    {
        Vector2 input = InputManager.Instance.MoveInput.normalized;
        rb.linearVelocity = input * player.MoveSpeed;
        player.RotateTowardsAim();
    }

}

// ===============================
// ⚫ Dead State
// ===============================
public class PlayerDeadState : PlayerState
{
    public PlayerDeadState(PlayerController player, PlayerStateMachine stateMachine)
        : base(player, stateMachine) { }

    public override void Enter()
    {
        rb.linearVelocity = Vector2.zero;

        Collider2D col = player.GetComponent<Collider2D>();
        if (col) col.enabled = false;

        Debug.Log("Player died.");
        LevelTransitionController.Instance.RestartLevel();
    }
}
