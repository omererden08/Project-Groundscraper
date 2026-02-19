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

    public Vector2 AimDirection
    {
        get
        {
            if (player == null)
                return transform.up;

            Vector2 dir = (Vector2)player.position - (Vector2)transform.position;
            return dir.sqrMagnitude > 0.0001f ? dir.normalized : transform.up;
        }
    }

    public float MeleeRange => data != null ? data.attackRange : 1f;
    public float MeleeRadius => 0.8f;

    // States
    public IdleState IdleState { get; private set; }
    public PatrolState PatrolState { get; private set; }
    public ChaseState ChaseState { get; private set; }
    public AttackState AttackState { get; private set; }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        pathfinder = GetComponent<EnemyPathfinder>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (data != null)
            currentMoveSpeed = data.moveSpeed;

        // Ranged ise, firePoint assign edilmemişse child içinden bulmayı dene
        if (firePoint == null && data != null && data.isRanged)
            firePoint = transform.Find("FirePoint");

        // Patrol noktalarını world position olarak topla
        List<Vector3> points = new List<Vector3>();
        if (patrolPointsRoot != null)
        {
            foreach (Transform child in patrolPointsRoot)
                points.Add(child.position);
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
        // Patrol noktası varsa Patrol, yoksa Idle ile başla
        stateMachine.Initialize(patrolPoints.Length > 0 ? (EnemyState)PatrolState : IdleState);
        PathIndex = 0;
    }

    private void Update()
    {
        if (isDead) return;

        stateMachine.Update();
        currentStateName = stateMachine.CurrentState?.GetType().Name;
    }

    #region Movement / Rotation

    public void SetSpeedMultiplier(float multiplier)
    {
        if (data == null) return;
        currentMoveSpeed = data.moveSpeed * multiplier;
    }

    public void ResetSpeed()
    {
        if (data == null) return;
        currentMoveSpeed = data.moveSpeed;
    }

    public void MoveTowards(Vector2 target)
    {
        Vector2 dir = target - (Vector2)transform.position;

        if (dir.sqrMagnitude < 0.0001f)
        {
            StopMoving();
            return;
        }

        dir.Normalize();
        rb.velocity = dir * currentMoveSpeed;
    }

    public void StopMoving() => rb.velocity = Vector2.zero;

    public void RotateTowards(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.0001f) return;

        Quaternion targetRotation = Quaternion.LookRotation(
            Vector3.forward,
            direction.normalized
        );

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            (data != null ? data.rotationSpeed : 360f) * Time.deltaTime
        );
    }

    #endregion

    #region Vision / Combat Checks

    public bool IsPlayerVisible()
    {
        if (player == null || data == null) return false;

        Vector2 origin = transform.position;
        Vector2 toPlayer = (Vector2)player.position - origin;
        float sqrDist = toPlayer.sqrMagnitude;
        float rangeSqr = data.visionRange * data.visionRange;

        if (sqrDist > rangeSqr) return false;

        Vector2 dir = toPlayer.normalized;
        float angle = Vector2.Angle(transform.up, dir);
        if (angle > data.visionAngle * 0.5f) return false;

        int mask = LayerMask.GetMask("Player", "Obstacle");
        RaycastHit2D hit = Physics2D.Raycast(origin, dir, Mathf.Sqrt(sqrDist), mask);

        return hit.collider != null && hit.collider.CompareTag("Player");
    }

    public bool IsInAttackRange()
    {
        if (player == null || data == null) return false;

        Vector2 diff = (Vector2)transform.position - (Vector2)player.position;
        return diff.sqrMagnitude <= data.attackRange * data.attackRange;
    }

    public void PerformAttack()
    {
        if (data == null) return;

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

    #endregion

    #region Damage / Death

    // IDamageable için muhtemel implementasyon (gerçek arabirime göre düzenle)
    public void Die()
    {
        if (isDead) return;
        isDead = true;

        gameObject.SetActive(false);

        StopMoving();
        //var sr = GetComponent<SpriteRenderer>();
        //if (sr != null) sr.color = Color.red;

        // İstersen collider kapat, state machine durdur vs.
        // GetComponent<Collider2D>()?.enabled = false;
    }

    #endregion

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (data == null) return;

        // Vision range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, data.visionRange);

        // FOV çizgileri
        Vector3 left = Quaternion.Euler(0, 0, -data.visionAngle / 2f) * transform.up;
        Vector3 right = Quaternion.Euler(0, 0, data.visionAngle / 2f) * transform.up;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + left * data.visionRange);
        Gizmos.DrawLine(transform.position, transform.position + right * data.visionRange);

        // Patrol noktaları
        Gizmos.color = Color.green;

        // Editor'de Awake çalışmadan da görebilmek için root'tan oku
        if (patrolPointsRoot != null)
        {
            foreach (Transform child in patrolPointsRoot)
            {
                Gizmos.DrawSphere(child.position, 0.15f);
            }
        }
        else if (patrolPoints != null)
        {
            foreach (var point in patrolPoints)
            {
                Gizmos.DrawSphere(point, 0.15f);
            }
        }
    }
#endif
}
