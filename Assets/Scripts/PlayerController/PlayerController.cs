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
    private Camera cachedCam;
    private Vector2 aimDirection;               // weaponHoldPoint -> mouse
    private bool hasAim;
    private Vector2 dirToMouseFromPlayer;       // player -> mouse

    private IWeapon currentWeapon;

    // UI cache
    private AmmoUI ammoUI;

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

        isMovedHash = Animator.StringToHash(isMovedParam);

        CacheLegsRefs();
        CacheBodyRenderer();

        // Camera cache (main camera unload/load olabilir, Update’te de kontrol ediyoruz)
        cachedCam = Camera.main;

        // AmmoUI cache (GamePlay HUD içinde olmalı)
        ammoUI = FindFirstObjectByType<AmmoUI>(); // Unity 2022+ ; eskiyse FindObjectOfType<AmmoUI>()

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
        ApplyBodySprite();
    }

    private void Update()
    {
        // Additive geçişlerde kamera değişebilir -> null ise yeniden al
        if (cachedCam == null)
            cachedCam = Camera.main;

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
        var legs = FindChildByName(transform, legsObjectName);
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

        // Daha iyi his için input yönünü tercih et
        Vector2 input = InputManager.Instance != null ? InputManager.Instance.MoveInput : Vector2.zero;
        if (input.sqrMagnitude < 0.0001f) input = v;

        float angle = Mathf.Atan2(input.y, input.x) * Mathf.Rad2Deg + legsRotationOffset;
        legsTransform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    // =========================
    // Body Sprite
    // =========================
    private void CacheBodyRenderer()
    {
        var body = FindChildByName(transform, bodyObjectName);
        if (body != null)
            bodyRenderer = body.GetComponent<SpriteRenderer>();
    }

    private void ApplyBodySprite()
    {
        if (bodyRenderer == null) return;

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

    // =========================
    // Weapon
    // =========================
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
        // Not: OverlapCircleAll alloc yapar. İstersen NonAlloc sürümünü de yazarım.
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

        ApplyBodySprite();

        PlayerEvents.RaiseWeaponPickedUp(weapon.WeaponID);

        if (weapon is RangedWeapon ranged)
        {
            if (ammoUI == null) ammoUI = FindFirstObjectByType<AmmoUI>();
            ammoUI?.SetWeapon(ranged);
        }
    }

    private void DropWeapon()
    {
        if (currentWeapon == null) return;

        currentWeapon.OnDrop(aimDirection);
        PlayerEvents.RaiseWeaponDropped(currentWeapon.WeaponID);

        if (currentWeapon is RangedWeapon)
        {
            if (ammoUI == null) ammoUI = FindFirstObjectByType<AmmoUI>();
            ammoUI?.Clear();
        }

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

    public void ResetForRestart()
    {
        // hareket / fizik temizle
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        // aim flag reset (opsiyonel ama iyi)
        hasAim = false;
        currentWeapon = null;

        // FSM'i yeniden başlat (kritik)
        StateMachine.Initialize(IdleState);

        // body sprite tekrar uygula (opsiyonel)
        ApplyBodySprite();
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
        Gizmos.DrawWireSphere((Vector2)weaponHoldPoint.position, meleeRadius);
    }
}