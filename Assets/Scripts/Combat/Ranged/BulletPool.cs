using System.Collections.Generic;
using UnityEngine;

public class BulletPool : MonoBehaviour
{
    public static BulletPool Instance;

    private readonly Dictionary<GameObject, Queue<Bullet>> pools = new();

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public Bullet GetBullet(GameObject bulletPrefab)
    {
        if (bulletPrefab == null)
        {
            Debug.LogError("BulletPool: bulletPrefab is null.");
            return null;
        }

        if (!pools.ContainsKey(bulletPrefab))
            pools[bulletPrefab] = new Queue<Bullet>();

        Queue<Bullet> pool = pools[bulletPrefab];

        while (pool.Count > 0)
        {
            Bullet bullet = pool.Dequeue();

            if (bullet != null)
            {
                bullet.gameObject.SetActive(true);
                return bullet;
            }
        }

        GameObject bulletObject = Instantiate(bulletPrefab);
        Bullet newBullet = bulletObject.GetComponent<Bullet>();

        if (newBullet == null)
        {
            Debug.LogError($"{bulletPrefab.name} prefabinde Bullet component yok.");
            Destroy(bulletObject);
            return null;
        }

        newBullet.SetPool(this, bulletPrefab);
        return newBullet;
    }

    public void ReturnBullet(Bullet bullet, GameObject bulletPrefab)
    {
        if (bullet == null || bulletPrefab == null)
            return;

        if (!pools.ContainsKey(bulletPrefab))
            pools[bulletPrefab] = new Queue<Bullet>();

        bullet.gameObject.SetActive(false);
        pools[bulletPrefab].Enqueue(bullet);
    }
}