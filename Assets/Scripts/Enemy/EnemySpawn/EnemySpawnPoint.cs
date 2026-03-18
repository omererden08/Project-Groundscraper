using UnityEngine;

public class EnemySpawnPoint : MonoBehaviour
{
    public EnemyType type = EnemyType.Melee;

    [Header("Spawn Timing")]
    public bool spawnOnLevelStart = true;
    [Min(0f)] public float delay = 0f;

    [Header("Optional")]
    [Tooltip("Bu noktaya özel patrol noktalarý (level bazlý). Boþsa enemy prefab içindeki Checkpoints kullanýlýr.")]
    public Transform patrolRootOverride;

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.2f);
        Gizmos.DrawLine(transform.position, transform.position + transform.up * 0.5f);
    }
#endif
}