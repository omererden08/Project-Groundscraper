using UnityEngine;
using System.Collections.Generic;

public class BulletPool : MonoBehaviour
{
    public static BulletPool Instance;

    [SerializeField] private Bullet bulletPrefab;
    [SerializeField] private int poolSize = 20;

    private Queue<Bullet> pool = new Queue<Bullet>();
    private Transform container;

    private void Awake()
    {
        Instance = this;
        container = new GameObject("BulletPool_Container").transform;
        container.SetParent(this.transform);

        for (int i = 0; i < poolSize; i++)
        {
            Bullet b = Instantiate(bulletPrefab, container);
            b.gameObject.SetActive(false);
            pool.Enqueue(b);
        }
    }

    public Bullet GetBullet()
    {
        if (pool.Count > 0)
            return pool.Dequeue();

        Debug.LogWarning("Bullet pool exhausted!");
        return Instantiate(bulletPrefab);
    }

    public void ReturnBullet(Bullet bullet)
    {
        bullet.gameObject.SetActive(false);
        bullet.transform.SetParent(container);
        pool.Enqueue(bullet);
    }
}
