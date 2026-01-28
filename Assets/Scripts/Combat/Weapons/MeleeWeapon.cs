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
        if (transform.parent == null)
        {
            Debug.LogWarning("Tried to use unequipped weapon.");
            return;
        }

        IMeleeAttacker attacker = transform.parent.GetComponentInParent<IMeleeAttacker>();
        if (attacker != null)
        {
            MeleeAttackHandler.DoAttack(attacker);
            Debug.Log("🪓 Melee Attack executed");
        }
    }

    public void OnEquip(Transform weaponHolder)
    {
        transform.SetParent(weaponHolder);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.isKinematic = true;
        }

        Collider2D col = GetComponent<Collider2D>();
        if (col)
            col.enabled = false;
    }


    public void OnDrop(Vector2 dropDirection)
    {
        transform.SetParent(null);

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;

            // Rigidbody önce resetlenmeli
            rb.isKinematic = false;
            rb.rotation = 0f; // opsiyonel: dönme sıfırlanır

            rb.AddForce(dropDirection.normalized * dropForce, ForceMode2D.Impulse);
            rb.linearDamping = dropLinearDrag;
            rb.angularDamping = dropAngularDrag;

            StopAllCoroutines(); // Yeni coroutine başlamadan önce eskileri iptal et
            StartCoroutine(ResetDrag(rb, dragResetDelay));
        }

        Collider2D col = GetComponent<Collider2D>();
        if (col)
            col.enabled = true;

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
