using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerAnimationController : MonoBehaviour
{
    [Header("Object Names")]
    [SerializeField] private string bodyObjectName = "PlayerBody";
    [SerializeField] private string legsObjectName = "PlayerLegs";

    [Header("Animator Parameters")]
    [SerializeField] private string isMovedParam = "IsMoved";
    [SerializeField] private string isArmedParam = "IsArmed";
    [SerializeField] private string meleeTriggerName = "Melee";
    [SerializeField] private string pistolTriggerName = "Pistol";
    [SerializeField] private string rifleTriggerName = "Rifle";
    [SerializeField] private string shotgunTriggerName = "Shotgun";

    [Header("Controllers")]
    [SerializeField] private RuntimeAnimatorController unarmedBodyController;

    [Header("Movement")]
    [SerializeField] private float moveAnimThreshold = 0.01f;
    private float legsRotationOffset = 90f;

    private Rigidbody2D rb;

    private Transform bodyTransform;
    private Animator bodyAnimator;

    private Transform legsTransform;
    private Animator legsAnimator;

    private WeaponData currentWeaponData;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        CacheRefs();
    }

    private void OnEnable()
    {
        PlayerEvents.OnWeaponEquipped += HandleWeaponEquipped;
        PlayerEvents.OnWeaponUnequipped += HandleWeaponUnequipped;
        PlayerEvents.OnPlayerMoveStateChanged += HandleMoveStateChanged;
        PlayerEvents.OnShoot += HandleShoot;
        PlayerEvents.OnMeleeAttack += HandleMeleeAttack;
        PlayerEvents.OnPlayerDied += HandlePlayerDied;
    }

    private void OnDisable()
    {
        PlayerEvents.OnWeaponEquipped -= HandleWeaponEquipped;
        PlayerEvents.OnWeaponUnequipped -= HandleWeaponUnequipped;
        PlayerEvents.OnPlayerMoveStateChanged -= HandleMoveStateChanged;
        PlayerEvents.OnShoot -= HandleShoot;
        PlayerEvents.OnMeleeAttack -= HandleMeleeAttack;
        PlayerEvents.OnPlayerDied -= HandlePlayerDied;
    }

    private void Start()
    {
        SetController(unarmedBodyController);
        SetBodyBool(isArmedParam, false);
        SetBodyBool(isMovedParam, false);
        SetLegsBool(isMovedParam, false);
    }

    private void LateUpdate()
    {
        UpdateLegsRotation();
    }

    private void HandleWeaponEquipped(WeaponData weaponData)
    {
        currentWeaponData = weaponData;

        RuntimeAnimatorController controllerToUse = unarmedBodyController;
        if (weaponData != null && weaponData.bodyOverrideController != null)
            controllerToUse = weaponData.bodyOverrideController;

        SetController(controllerToUse);

        SetBodyBool(isArmedParam, weaponData != null);

        bool isMoving = IsCurrentlyMoving();
        SetBodyBool(isMovedParam, isMoving);
        SetLegsBool(isMovedParam, isMoving);

        Debug.Log($"Weapon equipped -> Controller: {controllerToUse.name}, IsArmed: {GetBodyBool(isArmedParam)}, IsMoved: {GetBodyBool(isMovedParam)}");
    }

    private void HandleWeaponUnequipped()
    {
        currentWeaponData = null;

        SetController(unarmedBodyController);
        SetBodyBool(isArmedParam, false);

        bool isMoving = IsCurrentlyMoving();
        SetBodyBool(isMovedParam, isMoving);
        SetLegsBool(isMovedParam, isMoving);

        Debug.Log($"Weapon unequipped -> IsArmed: {GetBodyBool(isArmedParam)}");
    }

    private void HandleMoveStateChanged(bool isMoving)
    {
        SetLegsBool(isMovedParam, isMoving);
        SetBodyBool(isMovedParam, isMoving);
    }

    private void HandleShoot(Vector2 pos, Vector2 dir)
    {
        if (bodyAnimator == null || currentWeaponData == null)
            return;

        ResetAllFireTriggers();

        switch (currentWeaponData.FireAnimationType)
        {
            case WeaponFireAnimationType.Melee:
                SetBodyTrigger(meleeTriggerName);
                break;
            case WeaponFireAnimationType.Pistol:
                SetBodyTrigger(pistolTriggerName);
                break;
            case WeaponFireAnimationType.Rifle:
                SetBodyTrigger(rifleTriggerName);
                break;
            case WeaponFireAnimationType.Shotgun:
                SetBodyTrigger(shotgunTriggerName);
                break;
        }
    }

    private void HandleMeleeAttack(Vector2 pos, Vector2 dir)
    {
        if (bodyAnimator == null)
            return;

        ResetAllFireTriggers();
        SetBodyTrigger(meleeTriggerName);
    }

    private void ResetAllFireTriggers()
    {
        ResetBodyTrigger(meleeTriggerName);
        ResetBodyTrigger(pistolTriggerName);
        ResetBodyTrigger(rifleTriggerName);
        ResetBodyTrigger(shotgunTriggerName);
    }

    private void HandlePlayerDied()
    {
        SetBodyBool(isMovedParam, false);
        SetBodyBool(isArmedParam, false);
        SetLegsBool(isMovedParam, false);
    }

    private void SetController(RuntimeAnimatorController controller)
    {
        if (bodyAnimator == null || controller == null)
            return;

        bodyAnimator.runtimeAnimatorController = controller;
        bodyAnimator.Rebind();
        bodyAnimator.Update(0f);
    }

    private void SetBodyBool(string paramName, bool value)
    {
        if (bodyAnimator == null || !HasBoolParameter(bodyAnimator, paramName))
            return;

        bodyAnimator.SetBool(paramName, value);
    }

    private bool GetBodyBool(string paramName)
    {
        if (bodyAnimator == null || !HasBoolParameter(bodyAnimator, paramName))
            return false;

        return bodyAnimator.GetBool(paramName);
    }

    private void SetLegsBool(string paramName, bool value)
    {
        if (legsAnimator == null || !HasBoolParameter(legsAnimator, paramName))
            return;

        legsAnimator.SetBool(paramName, value);
    }

    private void SetBodyTrigger(string triggerName)
    {
        if (bodyAnimator == null || !HasTriggerParameter(bodyAnimator, triggerName))
            return;

        bodyAnimator.SetTrigger(triggerName);
    }

    private void ResetBodyTrigger(string triggerName)
    {
        if (bodyAnimator == null || !HasTriggerParameter(bodyAnimator, triggerName))
            return;

        bodyAnimator.ResetTrigger(triggerName);
    }

    private static bool HasBoolParameter(Animator animator, string paramName)
    {
        foreach (var p in animator.parameters)
        {
            if (p.name == paramName && p.type == AnimatorControllerParameterType.Bool)
                return true;
        }

        return false;
    }

    private static bool HasTriggerParameter(Animator animator, string paramName)
    {
        foreach (var p in animator.parameters)
        {
            if (p.name == paramName && p.type == AnimatorControllerParameterType.Trigger)
                return true;
        }

        return false;
    }

    private bool IsCurrentlyMoving()
    {
        return rb != null && rb.linearVelocity.sqrMagnitude > moveAnimThreshold * moveAnimThreshold;
    }

    private void UpdateLegsRotation()
    {
        if (legsTransform == null || rb == null)
            return;

        Vector2 velocity = rb.linearVelocity;
        bool isMoving = velocity.sqrMagnitude > moveAnimThreshold * moveAnimThreshold;
        if (!isMoving)
            return;

        Vector2 input = InputManager.Instance != null ? InputManager.Instance.MoveInput : Vector2.zero;
        if (input.sqrMagnitude < 0.0001f)
            input = velocity;

        float angle = Mathf.Atan2(input.y, input.x) * Mathf.Rad2Deg + legsRotationOffset;
        legsTransform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void CacheRefs()
    {
        bodyTransform = FindChildByName(transform, bodyObjectName);
        if (bodyTransform != null)
            bodyAnimator = bodyTransform.GetComponent<Animator>();

        legsTransform = FindChildByName(transform, legsObjectName);
        if (legsTransform != null)
            legsAnimator = legsTransform.GetComponent<Animator>();
    }

    private static Transform FindChildByName(Transform root, string childName)
    {
        if (root == null) return null;

        Transform direct = root.Find(childName);
        if (direct != null) return direct;

        var all = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i].name == childName)
                return all[i];
        }

        return null;
    }
}