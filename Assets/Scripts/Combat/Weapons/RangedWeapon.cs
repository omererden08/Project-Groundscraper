using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public abstract class RangedWeapon : MonoBehaviour, IWeapon
{
    [Header("Data")]
    [SerializeField] protected WeaponData data;

    [Header("References")]
    [SerializeField] protected Transform firePoint;

    protected int currentAmmo;
    protected SpriteRenderer spriteRenderer;
    protected Rigidbody2D rb;
    protected Collider2D weaponCollider;

    protected bool isEquipped;
    protected float lastFireTime;

    public virtual bool IsAutomatic => false;
    public int CurrentAmmo => currentAmmo;
    public int MaxAmmo => data != null ? data.maxAmmo : 0;
    public string WeaponID => data != null ? data.weaponID : string.Empty;
    public bool IsRanged => true;
    public Sprite BodySprite => data != null ? data.bodySprite : null;

    protected virtual void Awake()
    {
        if (data == null)
        {
            Debug.LogError($"{name}: WeaponData not assigned.");
            enabled = false;
            return;
        }

        if (firePoint == null)
            firePoint = transform.Find("FirePoint");

        if (firePoint == null)
            Debug.LogError($"{name}: FirePoint not found!");

        currentAmmo = data.maxAmmo;

        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        weaponCollider = GetComponent<Collider2D>();
    }

    private void HandleShoot(Vector2 position, Vector2 direction)
    {
        if (!isEquipped)
            return;

        Use(direction);
    }

    public void Use()
    {
        if (firePoint == null)
        {
            Debug.LogWarning($"{name}: Use() cancelled, firePoint is null.");
            return;
        }

        Use(firePoint.right);
    }

    public void Use(Vector2 direction)
    {
        if (!isEquipped)
            return;

        if (data != null && Time.time < lastFireTime + data.fireRate)
            return;

        if (currentAmmo <= 0)
        {
            ThrowAsProjectile(direction);
            return;
        }

        if (firePoint == null)
        {
            Debug.LogWarning($"{name}: FirePoint missing.");
            return;
        }

        Fire(direction.normalized);
        ConsumeAmmo(1);
        OnAmmoChanged();

        lastFireTime = Time.time;
    }

    protected abstract void Fire(Vector2 direction);

    protected virtual void ConsumeAmmo(int amount)
    {
        currentAmmo = Mathf.Max(0, currentAmmo - amount);
    }

    protected virtual void OnAmmoChanged()
    {
    }

    protected virtual void ThrowAsProjectile(Vector2 direction)
    {
        isEquipped = false;
        PlayerEvents.OnShoot -= HandleShoot;

        transform.SetParent(null);

        if (weaponCollider != null)
            weaponCollider.enabled = true;

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;

            rb.AddForce(direction.normalized * data.dropForce, ForceMode2D.Impulse);
            rb.linearDamping = data.drag;
            rb.angularDamping = data.drag;
        }

        PlayerEvents.RaiseWeaponDropped(WeaponID);

        AmmoUI ammoUI = FindObjectOfType<AmmoUI>();
        if (ammoUI != null)
            ammoUI.Clear();
    }

    public virtual void OnEquip(Transform holder)
    {
        transform.SetParent(holder);
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

        PlayerEvents.OnShoot -= HandleShoot;
        PlayerEvents.OnShoot += HandleShoot;
    }

    public virtual void OnDrop(Vector2 direction)
    {
        isEquipped = false;
        PlayerEvents.OnShoot -= HandleShoot;

        ThrowAsProjectile(direction);

        if (spriteRenderer != null)
            spriteRenderer.enabled = true;
    }

    protected Bullet SpawnBullet(Vector2 direction)
    {
        if (data == null)
        {
            Debug.LogError($"{name}: SpawnBullet failed, data is null.");
            return null;
        }

        if (data.bulletPrefab == null)
        {
            Debug.LogError($"{name}: Bullet prefab is missing in WeaponData.");
            return null;
        }

        if (firePoint == null)
        {
            Debug.LogError($"{name}: SpawnBullet failed, firePoint is null.");
            return null;
        }

        if (BulletPool.Instance == null)
        {
            Debug.LogError($"{name}: BulletPool.Instance is null.");
            return null;
        }

        var bullet = BulletPool.Instance.GetBullet(data.bulletPrefab);

        if (bullet == null)
        {
            Debug.LogError($"{name}: BulletPool returned null for prefab {data.bulletPrefab.name}.");
            return null;
        }

        bullet.transform.position = firePoint.position;
        bullet.transform.rotation = Quaternion.LookRotation(Vector3.forward, direction);

        bullet.SetOwner(transform.root.gameObject);
        bullet.Fire(direction);

        return bullet;
    }
}