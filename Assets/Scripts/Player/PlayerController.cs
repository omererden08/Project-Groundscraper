using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour, IMeleeAttacker, IDamageable
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Attack Durations")]
    [SerializeField] private float shootDuration = 0.15f;
    [SerializeField] private float meleeDuration = 0.25f;

    [Header("Melee Settings")]
    [SerializeField] private float meleeRange = 1.2f;
    [SerializeField] private float meleeRadius = 0.75f;

    [Header("Weapon")]
    [SerializeField] private Transform weaponHoldPoint;
    [SerializeField] private float pickupRadius = 1.5f;

    [Header("Debug")]
    [SerializeField] private string currentStateName;

    private Rigidbody2D rb;
    private Vector2 aimDirection;
    private bool hasAim;

    private IWeapon currentWeapon;

    // =========================
    // Public API
    // =========================
    public Transform Transform => transform;
    public Vector2 AimDirection => aimDirection;
    public float MeleeRange => meleeRange;
    public float MeleeRadius => meleeRadius;
    public float MoveSpeed => moveSpeed;
    public Rigidbody2D Rigidbody => rb;

    public bool HasWeapon => currentWeapon != null;
    public IWeapon CurrentWeapon => currentWeapon;

    public float ShootDuration => shootDuration;
    public float MeleeDuration => meleeDuration;

    // =========================
    // FSM
    // =========================
    public PlayerStateMachine StateMachine { get; private set; }

    public PlayerIdleState IdleState { get; private set; }
    public PlayerMoveState MoveState { get; private set; }
    public PlayerShootState ShootState { get; private set; }
    public PlayerMeleeState MeleeState { get; private set; }
    public PlayerDeadState DeadState { get; private set; }

    // =========================
    // Unity
    // =========================
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        if (weaponHoldPoint == null)
            weaponHoldPoint = transform.Find("WeaponHoldPoint");

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
        HandleWeaponInteraction();

        StateMachine.CurrentState.HandleInput();
        StateMachine.CurrentState.Update();

        currentStateName = StateMachine.CurrentState?.Name;
    }

    private void FixedUpdate()
    {
        StateMachine.CurrentState.PhysicsUpdate();
    }

    // =========================
    // Aim
    // =========================
    private void CacheAimDirection()
    {
        if (Camera.main == null) return;

        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(
            new Vector3(
                InputManager.Instance.LookInput.x,
                InputManager.Instance.LookInput.y,
                Mathf.Abs(Camera.main.transform.position.z)
            )
        );

        Vector2 dir = mouseWorld - transform.position;
        if (dir.sqrMagnitude < 0.001f) return;

        aimDirection = dir.normalized;
        hasAim = true;
    }

    public void RotateTowardsAim()
    {
        if (!hasAim) return;

        float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
        rb.rotation = angle;
    }

    // =========================
    // Weapon
    // =========================
    private void HandleWeaponInteraction()
    {
        if (!InputManager.Instance.InteractPressed)
            return;

        InputManager.Instance.ConsumeInteractInput();

        if (HasWeapon)
            DropWeapon();
        else
            TryPickupWeapon();
    }

    private void TryPickupWeapon()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, pickupRadius);

        foreach (var hit in hits)
        {
            if (hit.TryGetComponent(out IWeapon weapon))
            {
                EquipWeapon(weapon);
                break;
            }
        }
    }

    private void EquipWeapon(IWeapon weapon)
    {
        if (currentWeapon != null)
            DropWeapon();

        currentWeapon = weapon;
        weapon.OnEquip(weaponHoldPoint);

        PlayerEvents.RaiseWeaponPickedUp(weapon.WeaponID);

        if (weapon is RangedWeapon ranged)
            FindObjectOfType<AmmoUI>()?.SetWeapon(ranged);
    }

    private void DropWeapon()
    {
        if (currentWeapon == null) return;

        currentWeapon.OnDrop(aimDirection);
        PlayerEvents.RaiseWeaponDropped(currentWeapon.WeaponID);

        if (currentWeapon is RangedWeapon)
            FindObjectOfType<AmmoUI>()?.Clear();

        currentWeapon = null;
    }

    // =========================
    // Damage
    // =========================
    public void Die()
    {
        StateMachine.ChangeState(DeadState);
        PlayerEvents.RaisePlayerDied();
    }

    // =========================
    // Debug
    // =========================
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);

        Gizmos.color = Color.red;
        Vector2 dir = Application.isPlaying && aimDirection != Vector2.zero
            ? aimDirection
            : Vector2.right;

        Vector2 center = (Vector2)transform.position + dir * meleeRange;
        Gizmos.DrawWireSphere(center, meleeRadius);
    }
}
