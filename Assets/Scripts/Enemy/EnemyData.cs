using UnityEngine;

[CreateAssetMenu(menuName = "Enemy/EnemyData")]
public class EnemyData : ScriptableObject
{
    public float moveSpeed = 2f;
    public float rotationSpeed = 720f;
    public float attackRange = 1.5f;
    public float attackDelay = 0.5f;
    public bool isRanged = false;

    [Header("Ranged Settings")]
    public GameObject projectilePrefab;
    public float projectileSpeed = 10f;

    [Header("Vision Settings")]
    public float visionAngle = 90f;
    public float visionRange = 8f;
}
