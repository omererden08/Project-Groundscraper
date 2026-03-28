using System.Collections.Generic;
using UnityEngine;

public class EnemyPool : MonoBehaviour
{
    [System.Serializable]
    public class PoolEntry
    {
        public EnemyType type;
        public EnemyController prefab;
        public int initialSize = 5;
    }

    [Header("Pool Config")]
    [SerializeField] private PoolEntry[] entries;

    private readonly Dictionary<EnemyType, Queue<EnemyController>> pools = new();
    private readonly List<EnemyController> aliveEnemies = new();

    private void Awake()
    {
        for (int i = 0; i < entries.Length; i++)
        {
            PoolEntry entry = entries[i];

            if (entry == null || entry.prefab == null)
                continue;

            if (!pools.ContainsKey(entry.type))
                pools.Add(entry.type, new Queue<EnemyController>());

            for (int j = 0; j < entry.initialSize; j++)
            {
                EnemyController enemy = Create(entry.prefab);
                pools[entry.type].Enqueue(enemy);
            }
        }
    }

    private EnemyController Create(EnemyController prefab)
    {
        EnemyController enemy = Instantiate(prefab, transform);
        enemy.InitPool(this);
        enemy.gameObject.SetActive(false);
        return enemy;
    }

    public EnemyBase Spawn(
        EnemyType type,
        Vector3 position,
        Quaternion rotation,
        Transform parent,
        Transform player,
        Transform patrolRootOverride)
    {
        if (!pools.TryGetValue(type, out Queue<EnemyController> queue))
        {
            Debug.LogError($"EnemyPool: {type} için pool bulunamadı.");
            return null;
        }

        EnemyController enemy;

        if (queue.Count > 0)
        {
            enemy = queue.Dequeue();
        }
        else
        {
            EnemyController prefab = GetPrefab(type);

            if (prefab == null)
            {
                Debug.LogError($"EnemyPool: {type} için prefab bulunamadı.");
                return null;
            }

            enemy = Create(prefab);
        }

        enemy.transform.SetParent(parent);
        enemy.transform.SetPositionAndRotation(position, rotation);

        enemy.InitializeForSpawn(player, patrolRootOverride);
        enemy.SpawnedFromPool();

        aliveEnemies.Add(enemy);
        return enemy;
    }

    public void Despawn(EnemyBase enemyBase)
    {
        if (enemyBase == null)
            return;

        EnemyController enemy = enemyBase as EnemyController;
        if (enemy == null)
            return;

        aliveEnemies.Remove(enemy);

        enemy.DespawnedToPool();

        if (!pools.ContainsKey(enemy.Type))
            pools.Add(enemy.Type, new Queue<EnemyController>());

        pools[enemy.Type].Enqueue(enemy);
    }

    public void DespawnAllAlive()
    {
        for (int i = aliveEnemies.Count - 1; i >= 0; i--)
        {
            EnemyController enemy = aliveEnemies[i];
            if (enemy != null)
                enemy.DespawnToPool();
        }

        aliveEnemies.Clear();
    }

    private EnemyController GetPrefab(EnemyType type)
    {
        for (int i = 0; i < entries.Length; i++)
        {
            if (entries[i].type == type)
                return entries[i].prefab;
        }

        return null;
    }
}