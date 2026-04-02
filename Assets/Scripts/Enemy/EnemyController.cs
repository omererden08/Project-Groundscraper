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

    [Header("Animation")]
    [SerializeField] private string bodyObjectName = "EnemyBody";
    [SerializeField] private string legsObjectName = "EnemyLegs";
    [SerializeField] private string isMovingParam = "IsMoving";
    [SerializeField] private string shootTriggerName = "Shoot";
    [SerializeField] private float moveAnimThreshold = 0.01f;

    [Header("Debug")]
    [SerializeField] private string currentStateName;

    private Vector3 rememberedPlayerPosition;
    private bool hasRememberedPlayerPosition;
    private Vector3[] patrolPoints = System.Array.Empty<Vector3>();
    private Rigidbody2D rb;
    private Transform player;
    private EnemyPathfinder pathfinder;
    private EnemyStateMachine stateMachine;
    private bool isDead;
    private float currentMoveSpeed;

    private Transform bodyTransform;
    private Animator bodyAnimator;
    private Transform legsTransform;
    private Animator legsAnimator;

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
    public Vector3 RememberedPlayerPosition => rememberedPlayerPosition;
    public bool HasRememberedPlayerPosition => hasRememberedPlayerPosition;

    public void RememberPlayerPosition(Vector3 pos)
    {
        rememberedPlayerPosition = pos;
        hasRememberedPlayerPosition = true;
    }

    public void ClearRememberedPlayerPosition()
    {
        hasRememberedPlayerPosition = false;
    }

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

        if (firePoint == null && data != null && data.isRanged)
            firePoint = transform.Find("FirePoint");

        if (patrolPointsRoot == null)
        {
            var t = transform.Find("Checkpoints");
            if (t != null) patrolPointsRoot = t;
        }

        CacheAnimationRefs();

        stateMachine = new EnemyStateMachine();
        IdleState = new IdleState(this, stateMachine);
        PatrolState = new PatrolState(this, stateMachine);
        ChaseState = new ChaseState(this, stateMachine);
        AttackState = new AttackState(this, stateMachine);
    }

    public void InitializeForSpawn(Transform playerTransform, Transform overridePatrolRoot = null)
    {
        player = playerTransform;

        if (overridePatrolRoot != null)
            patrolPointsRoot = overridePatrolRoot;

        patrolPoints = BuildPatrolPoints(patrolPointsRoot);
    }

    private Vector3[] BuildPatrolPoints(Transform root)
    {
        if (root == null) return System.Array.Empty<Vector3>();

        var list = new List<Transform>();
        var all = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            var t = all[i];
            if (t == root) continue;
            list.Add(t);
        }

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
        ClearRememberedPlayerPosition();

        rb.linearVelocity = Vector2.zero;
        pathfinder.StopTracking();

        if (data != null)
            currentMoveSpeed = data.moveSpeed;

        SetMoveAnimation(false);

        var startState = (patrolPoints != null && patrolPoints.Length > 0)
            ? (EnemyState)PatrolState
            : IdleState;

        stateMachine.Initialize(startState);
    }

    protected override void OnDespawned()
    {
        rb.linearVelocity = Vector2.zero;
        pathfinder.StopTracking();
        SetMoveAnimation(false);
    }

    private void Update()
    {
        if (isDead) return;

        stateMachine.Update();
        UpdateMoveAnimation();

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

    public void StopMoving()
    {
        rb.linearVelocity = Vector2.zero;
        SetMoveAnimation(false);
    }

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

        TriggerShootAnimation();

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
        SetMoveAnimation(false);
        DespawnToPool();
    }

    #endregion

    #region Animation

    private void CacheAnimationRefs()
    {
        bodyTransform = FindChildByName(transform, bodyObjectName);
        if (bodyTransform != null)
            bodyAnimator = bodyTransform.GetComponent<Animator>();

        legsTransform = FindChildByName(transform, legsObjectName);
        if (legsTransform != null)
            legsAnimator = legsTransform.GetComponent<Animator>();
    }

    private void UpdateMoveAnimation()
    {
        bool isMoving = rb != null && rb.linearVelocity.sqrMagnitude > moveAnimThreshold * moveAnimThreshold;
        SetMoveAnimation(isMoving);
    }

    private void SetMoveAnimation(bool isMoving)
    {
        if (bodyAnimator != null && HasBoolParameter(bodyAnimator, isMovingParam))
            bodyAnimator.SetBool(isMovingParam, isMoving);

        if (legsAnimator != null && HasBoolParameter(legsAnimator, isMovingParam))
            legsAnimator.SetBool(isMovingParam, isMoving);
    }

    private void TriggerShootAnimation()
    {
        if (bodyAnimator == null || !HasTriggerParameter(bodyAnimator, shootTriggerName))
            return;

        bodyAnimator.ResetTrigger(shootTriggerName);
        bodyAnimator.SetTrigger(shootTriggerName);
    }

    private static bool HasBoolParameter(Animator animator, string paramName)
    {
        foreach (var p in animator.parameters)
        {
            if (p.name == paramName && p.type == AnimatorControllerParameterType.Bool)
                return true;
        }

        return false;
    }

    private static bool HasTriggerParameter(Animator animator, string paramName)
    {
        foreach (var p in animator.parameters)
        {
            if (p.name == paramName && p.type == AnimatorControllerParameterType.Trigger)
                return true;
        }

        return false;
    }

    private static Transform FindChildByName(Transform root, string childName)
    {
        if (root == null) return null;

        Transform direct = root.Find(childName);
        if (direct != null) return direct;

        var all = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i].name == childName)
                return all[i];
        }

        return null;
    }

    #endregion
}