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

    private Camera cachedCam;
    private Vector2 aimDirection;
    private bool hasAim;
    private bool wasMovingLastFrame;
    private Vector2 dirToMouseFromPlayer;

    private IWeapon currentWeapon;
    private AmmoUI ammoUI;


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
            weaponHoldPoint = transform.Find("WeaponHoldPoint");

        cachedCam = Camera.main;
        ammoUI = FindFirstObjectByType<AmmoUI>();

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
        PlayerEvents.RaisePlayerMoveStateChanged(false);
        PlayerEvents.RaiseWeaponUnequipped();
    }

    private void Update()
    {
        if (cachedCam == null)
            cachedCam = Camera.main;

        CacheAimDirection();
        HandleWeaponInteraction();

        StateMachine.CurrentState.HandleInput();
        StateMachine.CurrentState.Update();

        UpdateMovementEvent();

        currentStateName = StateMachine.CurrentState?.Name;
    }

    private void FixedUpdate()
    {
        StateMachine.CurrentState.PhysicsUpdate();
    }

    private void UpdateMovementEvent()
    {
        bool isMoving = rb.linearVelocity.sqrMagnitude > 0.0001f;

        if (wasMovingLastFrame == isMoving)
            return;

        wasMovingLastFrame = isMoving;
        PlayerEvents.RaisePlayerMoveStateChanged(isMoving);
    }

    private void CacheAimDirection()
    {
        if (cachedCam == null || weaponHoldPoint == null || InputManager.Instance == null)
        {
            hasAim = false;
            return;
        }

        float depth = -cachedCam.transform.position.z;

        Vector2 look = InputManager.Instance.LookInput;
        Vector3 mouseWorld = cachedCam.ScreenToWorldPoint(new Vector3(look.x, look.y, depth));
        mouseWorld.z = 0f;

        Vector2 fromWeaponToMouse = (Vector2)mouseWorld - (Vector2)weaponHoldPoint.position;
        if (fromWeaponToMouse.sqrMagnitude < 0.0001f)
        {
            hasAim = false;
            return;
        }

        aimDirection = fromWeaponToMouse.normalized;
        hasAim = true;

        dirToMouseFromPlayer = (Vector2)mouseWorld - rb.position;
    }

    public void RotateTowardsAim()
    {
        if (!hasAim || weaponHoldPoint == null) return;
        if (dirToMouseFromPlayer.sqrMagnitude < 0.0001f) return;

        float mouseAngle = Mathf.Atan2(dirToMouseFromPlayer.y, dirToMouseFromPlayer.x) * Mathf.Rad2Deg;

        Vector2 weaponLocal = weaponHoldPoint.localPosition;
        float weaponLocalAngle = Mathf.Atan2(weaponLocal.y, weaponLocal.x) * Mathf.Rad2Deg;

        rb.rotation = mouseAngle - weaponLocalAngle;
    }

    private void HandleWeaponInteraction()
    {
        if (InputManager.Instance == null) return;
        if (!InputManager.Instance.InteractPressed) return;

        InputManager.Instance.ConsumeInteractInput();

        if (HasWeapon) DropWeapon();
        else TryPickupWeapon();
    }

    private void TryPickupWeapon()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, pickupRadius);

        for (int i = 0; i < hits.Length; i++)
        {
            var hit = hits[i];
            if (hit != null && hit.TryGetComponent(out IWeapon weapon))
            {
                EquipWeapon(weapon);
                break;
            }
        }
    }

    private void EquipWeapon(IWeapon weapon)
    {
        if (weapon == null) return;

        if (currentWeapon != null)
            DropWeapon();

        currentWeapon = weapon;
        weapon.OnEquip(weaponHoldPoint);

        if (weapon is RangedWeapon ranged)
        {
            PlayerEvents.RaiseWeaponEquipped(ranged.Data);

            if (ammoUI == null)
                ammoUI = FindFirstObjectByType<AmmoUI>();

            ammoUI?.SetWeapon(ranged);
        }
        else if (weapon is MeleeWeapon melee)
        {
            PlayerEvents.RaiseWeaponEquipped(melee.Data);
        }
        else
        {
            PlayerEvents.RaiseWeaponUnequipped();
        }
    }

    private void DropWeapon()
    {
        if (currentWeapon == null) return;

        currentWeapon.OnDrop(aimDirection);

        if (currentWeapon is RangedWeapon)
        {
            if (ammoUI == null)
                ammoUI = FindFirstObjectByType<AmmoUI>();

            ammoUI?.Clear();
        }

        currentWeapon = null;
        PlayerEvents.RaiseWeaponUnequipped();
    }
    public void Die()
    {
        StateMachine.ChangeState(DeadState);
        PlayerEvents.RaisePlayerDied();
    }

    public void ResetForRestart()
    {
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        hasAim = false;
        currentWeapon = null;
        wasMovingLastFrame = false;

        PlayerEvents.RaisePlayerMoveStateChanged(false);
        PlayerEvents.RaiseWeaponUnequipped();

        StateMachine.Initialize(IdleState);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);

        if (weaponHoldPoint == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere((Vector2)weaponHoldPoint.position, meleeRadius);
    }
}