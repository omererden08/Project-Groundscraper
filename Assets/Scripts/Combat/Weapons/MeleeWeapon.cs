using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class MeleeWeapon : MonoBehaviour, IWeapon
{
    [Header("Data")]
    [SerializeField] private WeaponData data;

    [Header("Drop Physics")]
    [SerializeField] private float dragResetDelay = 1f;

    [Header("Equip Bonus")]
    [SerializeField] private float meleeRangeBonus = 1f;
    [SerializeField] private float meleeRadiusBonus = 1f;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Collider2D weaponCollider;

    private bool isEquipped;
    private Transform originalParent;

    public string WeaponID => data != null ? data.weaponID : string.Empty;
    public bool IsRanged => false;
    public WeaponData Data => data;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        weaponCollider = GetComponent<Collider2D>();

        if (data == null)
        {
            Debug.LogError($"{name}: WeaponData not assigned.");
            enabled = false;
        }
    }

    private void Start()
    {
        originalParent = transform.parent;
    }

    private void OnDisable()
    {
        PlayerEvents.OnMeleeAttack -= HandleMeleeAttack;
    }

    private void OnDestroy()
    {
        PlayerEvents.OnMeleeAttack -= HandleMeleeAttack;
    }

    private void HandleMeleeAttack(Vector2 position, Vector2 direction)
    {
        if (!isEquipped)
            return;

        Use();
    }

    public void Use()
    {
        if (!isEquipped || transform.parent == null)
        {
            Debug.LogWarning("Tried to use unequipped weapon.");
            return;
        }

        StopAllCoroutines();
        StartCoroutine(DoAttackWithDelay(0.25f));
    }

    private IEnumerator DoAttackWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (!isEquipped || transform.parent == null)
            yield break;

        IMeleeAttacker attacker = transform.parent.GetComponentInParent<IMeleeAttacker>();
        if (attacker != null)
            MeleeAttackHandler.DoAttack(attacker);
    }

    public void OnEquip(Transform weaponHolder)
    {
        transform.SetParent(weaponHolder);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.isKinematic = true;
        }

        if (weaponCollider != null)
            weaponCollider.enabled = false;

        if (spriteRenderer != null)
            spriteRenderer.enabled = false;

        isEquipped = true;

        PlayerController player = weaponHolder.GetComponentInParent<PlayerController>();
        if (player != null)
            player.AddMeleeBonus(meleeRangeBonus, meleeRadiusBonus);

        PlayerEvents.OnMeleeAttack -= HandleMeleeAttack;
        PlayerEvents.OnMeleeAttack += HandleMeleeAttack;
    }

    public void OnDrop(Vector2 dropDirection)
    {
        PlayerController player = GetComponentInParent<PlayerController>();
        if (player != null)
            player.ResetMeleeBonus();

        isEquipped = false;
        PlayerEvents.OnMeleeAttack -= HandleMeleeAttack;

        transform.SetParent(originalParent);

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.isKinematic = false;
            rb.rotation = 0f;

            if (data != null)
            {
                rb.AddForce(dropDirection.normalized * data.dropForce, ForceMode2D.Impulse);
                rb.linearDamping = data.drag;
                rb.angularDamping = data.drag;
            }

            StopAllCoroutines();
            StartCoroutine(ResetDrag(rb, dragResetDelay));
        }

        if (weaponCollider != null)
            weaponCollider.enabled = true;

        if (spriteRenderer != null)
            spriteRenderer.enabled = true;

        PlayerEvents.RaiseWeaponDropped(WeaponID);
    }

    private IEnumerator ResetDrag(Rigidbody2D targetRb, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (targetRb != null)
        {
            targetRb.linearDamping = 0f;
            targetRb.angularDamping = 0f;
        }
    }
}