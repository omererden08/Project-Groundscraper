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

    [Header("Animation")]
    [SerializeField] private string legsObjectName = "PlayerLegs";
    [SerializeField] private string isMovedParam = "isMoved";
    [SerializeField] private float moveAnimThreshold = 0.01f;
    [SerializeField] private float legsRotationOffset = 0f;

    [Header("Body Sprite")]
    [SerializeField] private string bodyObjectName = "PlayerBody";
    [SerializeField] private Sprite unarmedBodySprite;
    [SerializeField] private Sprite armedBodySprite;

    [Header("Debug")]
    [SerializeField] private string currentStateName;

    private Rigidbody2D rb;

    // Legs
    private Transform legsTransform;
    private Animator legsAnimator;
    private int isMovedHash;

    // Body
    private SpriteRenderer bodyRenderer;

    // Aim
    private Vector2 aimDirection;         // weaponHoldPoint -> mouse
    private bool hasAim;
    private Vector2 dirToMouseFromPlayer; // player -> mouse (rotation için)

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

        CacheLegsRefs();
        CacheBodyRenderer();

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
        ApplyBodySprite(); // başlangıçta silahsız/silahlı neyse
    }

    private void Update()
    {
        CacheAimDirection();
        HandleWeaponInteraction();
        UpdateLegsMoveAnimAndRotation();

        StateMachine.CurrentState.HandleInput();
        StateMachine.CurrentState.Update();

        currentStateName = StateMachine.CurrentState?.Name;
    }

    private void FixedUpdate()
    {
        StateMachine.CurrentState.PhysicsUpdate();
    }

    // =========================
    // Legs
    // =========================
    private void CacheLegsRefs()
    {
        isMovedHash = Animator.StringToHash(isMovedParam);

        Transform legs = FindChildByName(transform, legsObjectName);
        if (legs != null)
        {
            legsTransform = legs;
            legsAnimator = legs.GetComponent<Animator>();
        }
    }

    private void UpdateLegsMoveAnimAndRotation()
    {
        if (legsAnimator == null || legsTransform == null) return;

        Vector2 v = rb.linearVelocity;
        float thrSqr = moveAnimThreshold * moveAnimThreshold;

        bool isMoving = v.sqrMagnitude > thrSqr;
        legsAnimator.SetBool(isMovedHash, isMoving);

        if (!isMoving) return;

        // input ile dönsün istiyorsan rb velocity yerine MoveInput kullan:
        Vector2 input = InputManager.Instance.MoveInput;
        if (input.sqrMagnitude < 0.0001f) input = v;

        float angle = Mathf.Atan2(input.y, input.x) * Mathf.Rad2Deg + legsRotationOffset;
        legsTransform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    // =========================
    // Body Sprite
    // =========================
    private void CacheBodyRenderer()
    {
        Transform body = FindChildByName(transform, bodyObjectName);
        if (body != null)
            bodyRenderer = body.GetComponent<SpriteRenderer>();
    }

    private void ApplyBodySprite()
    {
        if (bodyRenderer == null) return;

        // sprite verilmediyse ellemeyelim
        if (HasWeapon)
        {
            if (armedBodySprite != null) bodyRenderer.sprite = armedBodySprite;
        }
        else
        {
            if (unarmedBodySprite != null) bodyRenderer.sprite = unarmedBodySprite;
        }
    }

    private static Transform FindChildByName(Transform root, string childName)
    {
        if (root == null) return null;

        Transform t = root.Find(childName);
        if (t != null) return t;

        var all = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
            if (all[i].name == childName)
                return all[i];

        return null;
    }

    // =========================
    // Aim
    // =========================
    private void CacheAimDirection()
    {
        if (Camera.main == null || weaponHoldPoint == null) return;

        float depth = -Camera.main.transform.position.z;

        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(
            new Vector3(
                InputManager.Instance.LookInput.x,
                InputManager.Instance.LookInput.y,
                depth
            )
        );
        mouseWorld.z = 0f;

        Vector2 fromWeaponToMouse = (Vector2)mouseWorld - (Vector2)weaponHoldPoint.position;
        if (fromWeaponToMouse.sqrMagnitude < 0.0001f) return;

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

        float targetAngle = mouseAngle - weaponLocalAngle;
        rb.rotation = targetAngle;
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

        ApplyBodySprite();

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

        ApplyBodySprite();
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

        if (weaponHoldPoint == null) return;

        Gizmos.color = Color.red;
        Vector2 center = (Vector2)weaponHoldPoint.position;
        Gizmos.DrawWireSphere(center, meleeRadius);
    }
}
