using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour, IMeleeAttacker
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Attack Durations")]
    [SerializeField] private float shootDuration = 0.15f;
    [SerializeField] private float meleeDuration = 0.25f;

    [Header("Melee Settings")]
    [SerializeField] private float meleeRange = 1.2f;
    [SerializeField] private float meleeRadius = 0.75f;

    [Header("Debug")]
    [SerializeField] private string currentStateName;
    [SerializeField] private bool hasWeapon = false;

    private Rigidbody2D rb;
    private Vector2 cachedAimDir;
    private bool hasAim;

    // ===== Interface + Public API =====
    public Transform Transform => transform;
    public Vector2 AimDirection => cachedAimDir;
    public float MeleeRange => meleeRange;
    public float MeleeRadius => meleeRadius;

    public float MoveSpeed => moveSpeed;
    public Rigidbody2D Rigidbody => rb;
    public bool HasWeapon => hasWeapon;
    public float ShootDuration => shootDuration;
    public float MeleeDuration => meleeDuration;

    // ===== FSM =====
    public PlayerStateMachine StateMachine { get; private set; }
    public PlayerIdleState IdleState { get; private set; }
    public PlayerMoveState MoveState { get; private set; }
    public PlayerShootState ShootState { get; private set; }
    public PlayerMeleeState MeleeState { get; private set; }
    public PlayerDeadState DeadState { get; private set; }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        StateMachine = new PlayerStateMachine();

        IdleState = new PlayerIdleState(this, StateMachine);
        MoveState = new PlayerMoveState(this, StateMachine);
        ShootState = new PlayerShootState(this, StateMachine);
        MeleeState = new PlayerMeleeState(this, StateMachine);
        DeadState = new PlayerDeadState(this, StateMachine);
    }

    private void Start()
    {
        StateMachine.Initialize(IdleState);
    }

    private void Update()
    {
        CacheAimDirection();

        StateMachine.CurrentState.HandleInput();
        StateMachine.CurrentState.Update();

        currentStateName = StateMachine.CurrentState?.Name;
    }

    private void FixedUpdate()
    {
        StateMachine.CurrentState.PhysicsUpdate();
    }

    private void CacheAimDirection()
    {
        if (Camera.main == null) return;

        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(new Vector3(
            InputManager.Instance.LookInput.x,
            InputManager.Instance.LookInput.y,
            Mathf.Abs(Camera.main.transform.position.z)
        ));

        Vector2 dir = mouseWorld - transform.position;

        if (dir.sqrMagnitude < 0.001f) return;

        cachedAimDir = dir.normalized;
        hasAim = true;
    }

    public void RotateTowardsAim()
    {
        if (!hasAim) return;

        float angle = Mathf.Atan2(cachedAimDir.y, cachedAimDir.x) * Mathf.Rad2Deg;
        rb.rotation = angle;
    }

    public void Die()
    {
        StateMachine.ChangeState(DeadState);
        PlayerEvents.RaisePlayerDied();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;

        Vector2 origin = transform.position;
        Vector2 direction = AimDirection != Vector2.zero ? AimDirection : Vector2.right;
        Vector2 center = origin + direction * meleeRange;

        Gizmos.DrawWireSphere(center, meleeRadius);
    }

}
