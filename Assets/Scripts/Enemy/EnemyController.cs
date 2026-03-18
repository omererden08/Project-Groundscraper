using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(EnemyPathfinder))]
public class EnemyController : EnemyBase, IDamageable, IMeleeAttacker
{
    [Header("Data")]
    [SerializeField] private EnemyData data;
    [SerializeField] private Transform firePoint;

    [Header("Patrol Points")]
    [Tooltip("Boşsa prefab içindeki 'Checkpoints' child'ını arar. Level bazlı patrol için spawn anında override verebilirsin.")]
    [SerializeField] private Transform patrolPointsRoot;

    [Header("Debug")]
    [SerializeField] private string currentStateName;

    // Runtime
    private Vector3[] patrolPoints = System.Array.Empty<Vector3>();
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
            if (player == null) return transform.up;
            Vector2 dir = (Vector2)player.position - (Vector2)transform.position;
            return dir.sqrMagnitude > 0.0001f ? dir.normalized : (Vector2)transform.up;
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

        if (data != null)
            currentMoveSpeed = data.moveSpeed;

        // Ranged ise, firePoint assign edilmemişse child içinden bul
        if (firePoint == null && data != null && data.isRanged)
            firePoint = transform.Find("FirePoint");

        // Patrol root boşsa prefab içindeki "Checkpoints" child'ını dene
        if (patrolPointsRoot == null)
        {
            var t = transform.Find("Checkpoints");
            if (t != null) patrolPointsRoot = t;
        }

        // FSM kur (başlatmayı spawn anında yapacağız)
        stateMachine = new EnemyStateMachine();
        IdleState = new IdleState(this, stateMachine);
        PatrolState = new PatrolState(this, stateMachine);
        ChaseState = new ChaseState(this, stateMachine);
        AttackState = new AttackState(this, stateMachine);
    }

    /// <summary>
    /// Pool/Spawner spawn anında çağırır.
    /// Player ve (istersen) level bazlı patrol root burada verilir.
    /// </summary>
    public void InitializeForSpawn(Transform playerTransform, Transform overridePatrolRoot = null)
    {
        player = playerTransform;

        if (overridePatrolRoot != null)
            patrolPointsRoot = overridePatrolRoot;

        patrolPoints = BuildPatrolPoints(patrolPointsRoot);

        // Debug istersen:
        // Debug.Log($"{name} patrol count = {patrolPoints.Length} root={(patrolPointsRoot ? patrolPointsRoot.name : "null")}");
    }

    private Vector3[] BuildPatrolPoints(Transform root)
    {
        if (root == null) return System.Array.Empty<Vector3>();

        // ✅ nested dahil tüm child'ları topla, root'u hariç tut
        var list = new List<Transform>();
        var all = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            var t = all[i];
            if (t == root) continue;
            list.Add(t);
        }

        // ✅ İsim sıralaması: P0, P1, P2 ... gibi isimlerde doğru rota verir
        list.Sort((a, b) => string.CompareOrdinal(a.name, b.name));

        var points = new Vector3[list.Count];
        for (int i = 0; i < list.Count; i++)
            points[i] = list[i].position;

        return points;
    }

    protected override void OnSpawned()
    {
        isDead = false;
        PathIndex = 0;

        // fizik & path reset
        rb.linearVelocity = Vector2.zero;
        pathfinder.StopTracking(); // stop + clear path

        // speed reset
        if (data != null)
            currentMoveSpeed = data.moveSpeed;

        // FSM başlat (Start yerine burada)
        var startState = (patrolPoints != null && patrolPoints.Length > 0)
            ? (EnemyState)PatrolState
            : IdleState;

        stateMachine.Initialize(startState);
    }

    protected override void OnDespawned()
    {
        rb.linearVelocity = Vector2.zero;
        pathfinder.StopTracking();
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
        rb.linearVelocity = dir * currentMoveSpeed;
    }

    public void StopMoving() => rb.linearVelocity = Vector2.zero;

    public void RotateTowards(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.0001f) return;

        Quaternion targetRotation = Quaternion.LookRotation(Vector3.forward, direction.normalized);

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
            var bullet = BulletPool.Instance.GetBullet(data.bulletPrefab);
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

    public void Die()
    {
        if (isDead) return;
        isDead = true;

        StopMoving();
        DespawnToPool();
    }

    #endregion
}