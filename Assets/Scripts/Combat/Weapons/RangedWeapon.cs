using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class RangedWeapon : MonoBehaviour, IWeapon
{
    [Header("Data")]
    [SerializeField] private WeaponData data;

    [Header("References")]
    [SerializeField] private Transform firePoint;

    private int currentAmmo;


    public int CurrentAmmo => currentAmmo;
    public int MaxAmmo => data.maxAmmo;

    public string WeaponID => data.weaponID;
    public bool IsRanged => true;

    private void Awake()
    {
        if (data == null)
        {
            Debug.LogError("❌ WeaponData not assigned.");
            enabled = false;
            return;
        }

        if (firePoint == null)
            firePoint = transform.Find("FirePoint");

        if (firePoint == null)
            Debug.LogError("❌ FirePoint not found!");

        currentAmmo = data.maxAmmo;
    }

    private void OnEnable()
    {
        PlayerEvents.OnShoot += HandleShoot;
    }

    private void OnDisable()
    {
        PlayerEvents.OnShoot -= HandleShoot;
    }

    private void HandleShoot(Vector2 position, Vector2 direction)
    {
        // Sadece elde takılıysa ateş et
        if (transform.parent == null)
            return;

        Use(direction);
    }

    // 🔫 TEK VE NET USE
    public void Use(Vector2 direction)
    {
        if (currentAmmo <= 0)
        {
            Debug.Log("❌ No Ammo! Throwing weapon.");
            ThrowAsProjectile(direction);
            return;
        }

        var bullet = BulletPool.Instance.GetBullet();
        bullet.transform.position = firePoint.position;
        bullet.transform.rotation = Quaternion.LookRotation(Vector3.forward, direction);

        bullet.Fire(direction);

        currentAmmo--;
        Debug.Log($"🔫 Fired | Ammo left: {currentAmmo}");
    }

    // Interface uyumu için
    public void Use()
    {
        Use(firePoint.right);
    }

    private void ThrowAsProjectile(Vector2 direction)
    {
        transform.SetParent(null);

        var col = GetComponent<Collider2D>();
        if (col) col.enabled = true;

        var rb = GetComponent<Rigidbody2D>();
        if (rb)
        {
            rb.isKinematic = false;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;

            rb.AddForce(direction.normalized * data.dropForce, ForceMode2D.Impulse);
            rb.linearDamping = data.drag;
            rb.angularDamping = data.drag;
        }
        PlayerEvents.RaiseWeaponDropped(WeaponID);
    }

    public void OnEquip(Transform holder)
    {
        transform.SetParent(holder);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        var rb = GetComponent<Rigidbody2D>();
        if (rb)
        {
            rb.linearVelocity = Vector2.zero;
            rb.isKinematic = true;
        }

        var col = GetComponent<Collider2D>();
        if (col) col.enabled = false;
    }

    public void OnDrop(Vector2 direction)
    {
        ThrowAsProjectile(direction);
    }
}
