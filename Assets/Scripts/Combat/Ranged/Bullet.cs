using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] private float speed = 20f;
    [SerializeField] private float lifeTime = 3f;

    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void Fire(Vector2 direction)
    {
        CancelInvoke(); // Önceki çağrıları temizle

        gameObject.SetActive(true);
        rb.linearVelocity = direction.normalized * speed;

        // Sadece zamanlayıcı başlat — çarpmadıysa bu çalışır
        Invoke(nameof(ReturnToPool), lifeTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.TryGetComponent(out IDamageable damageable))
        {
            damageable.Die();
            CancelInvoke();
            ReturnToPool();
        }
        else
        {
            CancelInvoke();
            ReturnToPool();
        }
    }


    private void ReturnToPool()
    {
        rb.linearVelocity = Vector2.zero;
        BulletPool.Instance.ReturnBullet(this);
    }
}
