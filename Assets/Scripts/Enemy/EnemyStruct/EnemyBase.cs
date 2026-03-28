using UnityEngine;

public abstract class EnemyBase : MonoBehaviour
{
    [Header("Pool")]
    [SerializeField] private EnemyType type;
    public EnemyType Type => type;

    private EnemyPool ownerPool;
    private bool isRegisteredToStage;

    public void InitPool(EnemyPool pool)
    {
        ownerPool = pool;
    }

    public void SpawnedFromPool()
    {
        isRegisteredToStage = false;

        gameObject.SetActive(true);
        OnSpawned();
    }

    public void RegisterToStage()
    {
        if (isRegisteredToStage)
            return;

        if (StageClearManager.Instance != null)
        {
            StageClearManager.Instance.RegisterEnemy(this);
            isRegisteredToStage = true;
        }
    }

    public void DespawnToPool()
    {
        UnregisterFromStageIfNeeded();

        if (ownerPool != null)
            ownerPool.Despawn(this);
        else
            gameObject.SetActive(false);
    }

    public void DespawnedToPool()
    {
        OnDespawned();
        isRegisteredToStage = false;
        gameObject.SetActive(false);
    }

    private void UnregisterFromStageIfNeeded()
    {
        if (!isRegisteredToStage)
            return;

        if (StageClearManager.Instance != null)
            StageClearManager.Instance.UnregisterEnemy(this);

        isRegisteredToStage = false;
    }

    protected virtual void OnSpawned() { }
    protected virtual void OnDespawned() { }
}