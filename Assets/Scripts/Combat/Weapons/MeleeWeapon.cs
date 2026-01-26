using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class MeleeWeapon : MonoBehaviour, IWeapon
{
    [Header("Weapon Settings")]
    [SerializeField] private string weaponID = "melee_default";

    [Header("Drop Physics")]
    [SerializeField] private float dropForce = 6f;
    [SerializeField] private float dropLinearDrag = 7f;
    [SerializeField] private float dropAngularDrag = 7f;
    [SerializeField] private float dragResetDelay = 1f;


    public string WeaponID => weaponID;
    public bool IsRanged => false;


    public void Use()
    {
        // Weapon elde takılı mı kontrolü
        if (transform.parent == null)
        {
            Debug.LogWarning("MeleeWeapon.Use() called but weapon is not equipped.");
            return;
        }

        // Weapon holder = player veya enemy olabilir
        IMeleeAttacker attacker = transform.parent.GetComponentInParent<IMeleeAttacker>();
        if (attacker == null)
        {
            Debug.LogWarning("No IMeleeAttacker found for melee weapon.");
            return;
        }

        // 🔥 Asıl vuruş burada
        MeleeAttackHandler.DoAttack(attacker);

        Debug.Log("🪓 Melee Attack executed");
    }


    public void OnEquip(Transform weaponHolder)
    {
        transform.SetParent(weaponHolder);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        var rb = GetComponent<Rigidbody2D>();
        if (rb)
        {
            rb.linearVelocity = Vector2.zero;
            rb.isKinematic = true;
        }

        var col = GetComponent<Collider2D>();
        if (col)
            col.enabled = false; // 🟢 ÖNEMLİ: Silah eldeyken collider kapalı
    }


    public void OnDrop(Vector2 dropDirection)
    {
        transform.SetParent(null);

        var rb = GetComponent<Rigidbody2D>();
        if (rb)
        {
            rb.isKinematic = false;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;

            rb.AddForce(dropDirection.normalized * dropForce, ForceMode2D.Impulse);
            rb.linearDamping = dropLinearDrag;
            rb.angularDamping = dropAngularDrag;

            StartCoroutine(ResetDrag(rb, dragResetDelay));
        }

        var col = GetComponent<Collider2D>();
        if (col)
            col.enabled = true; // 🟢 Drop edilince collider tekrar açılır

        Debug.Log("📦 Weapon Dropped");
    }


    private IEnumerator ResetDrag(Rigidbody2D rb, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (rb != null)
        {
            rb.linearDamping = 0f;
            rb.angularDamping = 0f;
        }
    }
}
