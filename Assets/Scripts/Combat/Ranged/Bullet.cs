using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Bullet : MonoBehaviour
{
    [SerializeField] private float speed = 15f;
    [SerializeField] private int damage = 1;

    private BulletPool pool;
    private GameObject sourcePrefab;
    private Rigidbody2D rb;

    private bool isActiveBullet;
    private GameObject owner;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void SetPool(BulletPool bulletPool, GameObject prefab)
    {
        pool = bulletPool;
        sourcePrefab = prefab;
    }

    public void SetOwner(GameObject newOwner)
    {
        owner = newOwner;
    }

    public void Fire(Vector2 direction)
    {
        isActiveBullet = true;

        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.linearVelocity = direction.normalized * speed;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isActiveBullet)
            return;

        if (owner != null && other.transform.root.gameObject == owner)
            return;


        IDamageable damageable = other.GetComponent<IDamageable>();

        if (damageable != null)
            damageable.Die();

        ReturnToPool();
    }

    public void ReturnToPool()
    {
        isActiveBullet = false;
        owner = null;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        if (pool != null)
            pool.ReturnBullet(this, sourcePrefab);
        else
            gameObject.SetActive(false);
    }
}