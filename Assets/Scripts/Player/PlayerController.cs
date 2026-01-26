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

    [Header("Weapon Settings")]
    [SerializeField] private Transform weaponHoldPoint;
    [SerializeField] private float pickupRadius = 1.5f;

    [Header("Debug")]
    [SerializeField] private string currentStateName;

    private Rigidbody2D rb;
    private Vector2 cachedAimDir;
    private bool hasAim;

    private IWeapon currentWeapon;

    // === Public API ===
    public Transform Transform => transform;
    public Vector2 AimDirection => cachedAimDir;
    public float MeleeRange => meleeRange;
    public float MeleeRadius => meleeRadius;
    public float MoveSpeed => moveSpeed;
    public Rigidbody2D Rigidbody => rb;
    public bool HasWeapon => currentWeapon != null;
    public IWeapon CurrentWeapon => currentWeapon;
    public float ShootDuration => shootDuration;
    public float MeleeDuration => meleeDuration;

    // === State Machine ===
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

        if (weaponHoldPoint == null)
        {
            Transform found = transform.Find("WeaponHoldPoint");
            if (found != null)
            {
                weaponHoldPoint = found;
            }
            else
            {
                Debug.LogWarning(" WeaponHoldPoint bulunamadı. Lütfen Player altına 'WeaponHoldPoint' adında bir child obje ekleyin.");
            }
        }

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

    private void OnEnable()
    {
        PlayerEvents.OnWeaponDropped += HandleWeaponDropped;
    }

    private void OnDisable()
    {
        PlayerEvents.OnWeaponDropped -= HandleWeaponDropped;
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

    private void HandleWeaponInteraction()
    {
        if (!InputManager.Instance.InteractPressed) return;

        InputManager.Instance.ConsumeInteractInput();

        if (HasWeapon)
        {
            DropWeapon();
        }
        else
        {
            TryPickupWeapon();
        }
    }

    private void TryPickupWeapon()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, pickupRadius);

        foreach (var col in colliders)
        {
            if (col.TryGetComponent(out IWeapon weapon))
            {
                Debug.Log($"🟢 Weapon found: {weapon.WeaponID}");
                PickupWeapon(weapon);
                break;
            }
        }
    }

    private void PickupWeapon(IWeapon newWeapon)
    {
        currentWeapon = newWeapon;
        newWeapon.OnEquip(weaponHoldPoint);
        PlayerEvents.RaiseWeaponPickedUp(newWeapon.WeaponID);

        // 🎯 UI bağlantısı
        if (newWeapon is RangedWeapon rangedWeapon)
        {
            FindObjectOfType<AmmoUI>().SetWeapon(rangedWeapon);
        }
    }

    private void DropWeapon()
    {
        if (currentWeapon == null) return;

        currentWeapon.OnDrop(AimDirection);

    }


    private void HandleWeaponDropped(string weaponId)
    {
        if (currentWeapon != null && currentWeapon.WeaponID == weaponId)
        {
            FindObjectOfType<AmmoUI>()?.Clear();  
            currentWeapon = null;                 
        }
    }


    private void OnDrawGizmosSelected()
    {
        // Pickup Area
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);

        // Melee Attack Area
        Gizmos.color = Color.red;

        Vector2 direction = Application.isPlaying && AimDirection != Vector2.zero
            ? AimDirection
            : Vector2.right;

        Vector2 center = (Vector2)transform.position + direction * meleeRange;
        Gizmos.DrawWireSphere(center, meleeRadius);
    }
}
