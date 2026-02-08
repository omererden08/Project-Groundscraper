using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(EnemyPathfinder))]
public class EnemyController : MonoBehaviour, IDamageable, IMeleeAttacker
{
    [Header("Data")]
    [SerializeField] private EnemyData data;
    [SerializeField] private Transform firePoint;

    [Header("Patrol Points")]
    [Tooltip("Sahnedeki bağımsız Checkpoints GameObject'i (child değil!)")]
    [SerializeField] private Transform patrolPointsRoot;

    [Header("Debug")]
    [SerializeField] private string currentStateName;

    // Runtime
    private Vector3[] patrolPoints;
    private Rigidbody2D rb;
    private Transform player;
    private EnemyPathfinder pathfinder;
    private EnemyStateMachine stateMachine;
    private bool isDead;
    private float currentMoveSpeed;

    // Public Accessors
    public EnemyData Data => data;
    public Transform Player => player;
    public Transform FirePoint => firePoint;
    public float MoveSpeed => currentMoveSpeed;
    public Vector3[] PatrolPoints => patrolPoints;
    public int PathIndex { get; set; }

    public Transform Transform => transform;
    public EnemyPathfinder Pathfinder => pathfinder;
    public Vector2 AimDirection => (player.position - transform.position).normalized;

    public float MeleeRange => data.attackRange;
    public float MeleeRadius => 0.8f;

    public IdleState IdleState { get; private set; }
    public PatrolState PatrolState { get; private set; }
    public ChaseState ChaseState { get; private set; }
    public AttackState AttackState { get; private set; }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        pathfinder = GetComponent<EnemyPathfinder>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        currentMoveSpeed = data.moveSpeed; 

        if (firePoint == null && data.isRanged)
            firePoint = transform.Find("FirePoint");

        List<Vector3> points = new List<Vector3>();
        if (patrolPointsRoot != null)
        {
            foreach (Transform child in patrolPointsRoot)
                points.Add(child.position); // world position!
        }
        patrolPoints = points.ToArray();

        // FSM
        stateMachine = new EnemyStateMachine();
        IdleState = new IdleState(this, stateMachine);
        PatrolState = new PatrolState(this, stateMachine);
        ChaseState = new ChaseState(this, stateMachine);
        AttackState = new AttackState(this, stateMachine);
    }

    private void Start()
    {
        stateMachine.Initialize(patrolPoints.Length > 0 ? PatrolState : IdleState);
        PathIndex = 0;

    }

    private void Update()
    {
        if (isDead) return;

        stateMachine.Update();
        currentStateName = stateMachine.CurrentState?.GetType().Name;
    }
    public void SetSpeedMultiplier(float multiplier)
    {
        currentMoveSpeed = data.moveSpeed * multiplier;
    }

    public void ResetSpeed()
    {
        currentMoveSpeed = data.moveSpeed;
    }

    public void MoveTowards(Vector2 target)
    {
        Vector2 dir = (target - (Vector2)transform.position);
        if (dir.sqrMagnitude < 0.01f)
        {
            StopMoving();
            return;
        }

        dir.Normalize();
        rb.linearVelocity = dir * currentMoveSpeed;
    }



    public void StopMoving() => rb.linearVelocity = Vector2.zero;

    public void RotateTowards(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.01f) return;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Quaternion targetRotation = Quaternion.Euler(0, 0, angle);

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            data.rotationSpeed * Time.deltaTime
        );
    }



    public bool IsPlayerVisible()
    {
        if (player == null) return false;

        Vector2 dir = player.position - transform.position;

        if (dir.magnitude > data.visionRange) return false;

        float angle = Vector2.Angle(transform.right, dir.normalized);
        if (angle > data.visionAngle / 2f) return false;

        // 👇 Raycast ile engel kontrolü
        int mask = LayerMask.GetMask("Player", "Obstacle");  // Obstacle ve Player layer’larını içeriyor
        RaycastHit2D hit = Physics2D.Raycast(transform.position, dir.normalized, dir.magnitude, mask);

        if (hit.collider == null)
            return false;

        return hit.collider.CompareTag("Player");
    }


    public bool IsInAttackRange()
    {
        return Vector2.Distance(transform.position, player.position) <= data.attackRange;
    }

    public void PerformAttack()
    {
        if (data.isRanged && firePoint != null)
        {
            var bullet = BulletPool.Instance.GetBullet();
            bullet.transform.position = firePoint.position;
            bullet.transform.rotation = Quaternion.LookRotation(Vector3.forward, AimDirection);
            bullet.Fire(AimDirection);
        }
        else
        {
            MeleeAttackHandler.DoAttack(this);
        }
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;
        StopMoving();
        GetComponent<SpriteRenderer>().color = Color.red;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (data == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, data.visionRange);

        Vector3 left = Quaternion.Euler(0, 0, -data.visionAngle / 2f) * transform.right;
        Vector3 right = Quaternion.Euler(0, 0, data.visionAngle / 2f) * transform.right;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + left * data.visionRange);
        Gizmos.DrawLine(transform.position, transform.position + right * data.visionRange);



        Gizmos.color = Color.green;
        if (patrolPoints != null)
        {
            foreach (var point in patrolPoints)
                Gizmos.DrawSphere(point, 0.15f);
        }
    }
#endif
}
