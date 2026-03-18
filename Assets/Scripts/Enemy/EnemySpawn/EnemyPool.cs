using System.Collections.Generic;
using UnityEngine;

public class EnemyPool : MonoBehaviour
{
    [System.Serializable]
    public class PoolEntry
    {
        public EnemyType type;
        public EnemyBase prefab;
        [Min(0)] public int prewarm = 8;
    }

    [SerializeField] private PoolEntry[] entries;

    private readonly Dictionary<EnemyType, Queue<EnemyBase>> pool = new();
    private readonly Dictionary<EnemyType, EnemyBase> prefabMap = new();
    private readonly HashSet<EnemyBase> alive = new();

    private void Awake()
    {
        foreach (var e in entries)
        {
            if (e.prefab == null) continue;

            prefabMap[e.type] = e.prefab;
            pool[e.type] = new Queue<EnemyBase>(e.prewarm);

            for (int i = 0; i < e.prewarm; i++)
            {
                var inst = Instantiate(e.prefab, transform);
                inst.InitPool(this);
                inst.DespawnedToPool(); // OnDespawned + disable
                pool[e.type].Enqueue(inst);
            }
        }
    }

    public EnemyBase Spawn(EnemyType type, Vector3 pos, Quaternion rot, Transform parent, Transform player, Transform patrolRootOverride = null)
    {
        if (!prefabMap.TryGetValue(type, out var prefab))
        {
            Debug.LogError($"EnemyPool: Prefab yok -> {type}");
            return null;
        }

        EnemyBase enemy = (pool.TryGetValue(type, out var q) && q.Count > 0)
            ? q.Dequeue()
            : Instantiate(prefab, transform);

        enemy.InitPool(this);

        enemy.transform.SetParent(parent, true);
        enemy.transform.SetPositionAndRotation(pos, rot);

        // inject
        var controller = enemy.GetComponent<EnemyController>();
        if (controller != null)
            controller.InitializeForSpawn(player, patrolRootOverride);

        enemy.SpawnedFromPool(); // enable + OnSpawned

        alive.Add(enemy);
        return enemy;
    }

    public void Despawn(EnemyBase enemy)
    {
        if (enemy == null) return;

        alive.Remove(enemy);

        var type = enemy.Type;
        enemy.transform.SetParent(transform, true);
        enemy.DespawnedToPool(); // OnDespawned + disable

        if (!pool.TryGetValue(type, out var q))
            pool[type] = q = new Queue<EnemyBase>();

        q.Enqueue(enemy);
    }

    public void DespawnAllAlive()
    {
        var temp = new List<EnemyBase>(alive);
        for (int i = 0; i < temp.Count; i++)
            Despawn(temp[i]);

        alive.Clear();
    }
}