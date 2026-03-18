using UnityEngine;

public abstract class EnemyBase : MonoBehaviour
{
    [Header("Pool")]
    [SerializeField] private EnemyType type;
    public EnemyType Type => type;

    private EnemyPool ownerPool;

    public void InitPool(EnemyPool pool) => ownerPool = pool;

    // Pool/Spawner bunu çaðýracak
    public void SpawnedFromPool()
    {
        gameObject.SetActive(true);
        OnSpawned();
    }

    // Enemy ölünce vs çaðýr
    public void DespawnToPool()
    {
        if (ownerPool != null) ownerPool.Despawn(this);
        else gameObject.SetActive(false);
    }

    // Pool çaðýrýr
    public void DespawnedToPool()
    {
        OnDespawned();
        gameObject.SetActive(false);
    }

    protected virtual void OnSpawned() { }
    protected virtual void OnDespawned() { }
}