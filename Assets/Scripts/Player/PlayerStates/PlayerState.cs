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

    protected bool WantsToShoot()
    {
        if (InputManager.Instance == null)
            return false;

        if (player.CurrentWeapon is not RangedWeapon rangedWeapon)
            return false;

        return rangedWeapon.IsAutomatic
            ? InputManager.Instance.AttackHeld
            : InputManager.Instance.AttackPressed;
    }

    protected bool WantsToMelee()
    {
        if (InputManager.Instance == null)
            return false;

        if (player.CurrentWeapon is RangedWeapon)
            return false;

        return InputManager.Instance.AttackPressed;
    }

    protected void ConsumeSemiAutoInputIfNeeded()
    {
        if (InputManager.Instance == null)
            return;

        if (player.CurrentWeapon is RangedWeapon rangedWeapon)
        {
            if (!rangedWeapon.IsAutomatic)
                InputManager.Instance.ConsumeAttackInput();
        }
        else
        {
            InputManager.Instance.ConsumeAttackInput();
        }
    }

    protected void GoToLocomotionState()
    {
        if (InputManager.Instance != null && InputManager.Instance.MoveInput != Vector2.zero)
            stateMachine.ChangeState(player.MoveState);
        else
            stateMachine.ChangeState(player.IdleState);
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
        if (WantsToShoot())
        {
            stateMachine.ChangeState(player.ShootState);
            return;
        }

        if (WantsToMelee())
        {
            stateMachine.ChangeState(player.MeleeState);
            return;
        }

        if (InputManager.Instance != null && InputManager.Instance.MoveInput != Vector2.zero)
        {
            stateMachine.ChangeState(player.MoveState);
            return;
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
        if (WantsToShoot())
        {
            stateMachine.ChangeState(player.ShootState);
            return;
        }

        if (WantsToMelee())
        {
            stateMachine.ChangeState(player.MeleeState);
            return;
        }

        if (InputManager.Instance != null && InputManager.Instance.MoveInput == Vector2.zero)
        {
            stateMachine.ChangeState(player.IdleState);
            return;
        }
    }

    public override void PhysicsUpdate()
    {
        Vector2 input = InputManager.Instance != null
            ? InputManager.Instance.MoveInput.normalized
            : Vector2.zero;

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
        ConsumeSemiAutoInputIfNeeded();
    }

    public override void HandleInput()
    {
        // Shoot state içinde ayrı input geçişi gerekmiyor.
        // Süre bitince Update karar verecek.
    }

    public override void Update()
    {
        timer += Time.deltaTime;

        if (timer < player.ShootDuration)
            return;

        // Otomatik silahta tuş basılı tutuluyorsa tekrar ateşe devam et
        if (player.CurrentWeapon is RangedWeapon rangedWeapon && rangedWeapon.IsAutomatic)
        {
            if (InputManager.Instance != null && InputManager.Instance.AttackHeld)
            {
                stateMachine.ChangeState(player.ShootState);
                return;
            }
        }

        // Semi-auto silah için yeni tık geldiyse yeniden ateş et
        if (player.CurrentWeapon is RangedWeapon semiWeapon && !semiWeapon.IsAutomatic)
        {
            if (InputManager.Instance != null && InputManager.Instance.AttackPressed)
            {
                stateMachine.ChangeState(player.ShootState);
                return;
            }
        }

        GoToLocomotionState();
    }

    public override void PhysicsUpdate()
    {
        Vector2 input = InputManager.Instance != null
            ? InputManager.Instance.MoveInput.normalized
            : Vector2.zero;

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

        ConsumeSemiAutoInputIfNeeded();

        if (player.HasWeapon && player.CurrentWeapon is IWeapon weapon && player.CurrentWeapon is not RangedWeapon)
        {
            weapon.Use();
        }
        else
        {
            MeleeAttackHandler.DoAttack(player);
        }

        PlayerEvents.RaiseMeleeAttack(player.transform.position, player.AimDirection);
    }

    public override void Update()
    {
        timer += Time.deltaTime;

        if (timer < player.MeleeDuration)
            return;

        // Melee sonrası atak tuşuna tekrar basıldıysa yeniden vur
        if (WantsToMelee())
        {
            stateMachine.ChangeState(player.MeleeState);
            return;
        }

        // Melee state'ten direkt ranged shoot'a geçişe de izin ver
        if (WantsToShoot())
        {
            stateMachine.ChangeState(player.ShootState);
            return;
        }

        GoToLocomotionState();
    }

    public override void PhysicsUpdate()
    {
        Vector2 input = InputManager.Instance != null
            ? InputManager.Instance.MoveInput.normalized
            : Vector2.zero;

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