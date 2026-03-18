using UnityEngine;

public abstract class EnemyState
{
    protected EnemyController enemy;
    protected EnemyStateMachine stateMachine;

    protected EnemyState(EnemyController enemy, EnemyStateMachine stateMachine)
    {
        this.enemy = enemy;
        this.stateMachine = stateMachine;
    }

    public virtual void Enter() { }
    public virtual void Exit() { }
    public virtual void Update() { }
}

public class IdleState : EnemyState
{
    public IdleState(EnemyController enemy, EnemyStateMachine stateMachine)
        : base(enemy, stateMachine) { }

    public override void Enter()
    {
        enemy.StopMoving();
    }

    public override void Update()
    {
        if (!enemy.IsPlayerVisible())
            return;

        if (enemy.Data != null && enemy.Data.isRanged && enemy.IsInAttackRange())
            stateMachine.ChangeState(enemy.AttackState);
        else
            stateMachine.ChangeState(enemy.ChaseState);
    }
}

public class PatrolState : EnemyState
{
    private int currentIndex = 0;
    private int direction = 1;
    private Vector3 currentTarget;

    private const float NodeReachDist = 0.2f;   // node üstünde sayılma eşiği
    private const float TargetReachDist = 0.35f; // patrol point eşiği (biraz artırdım)

    public PatrolState(EnemyController enemy, EnemyStateMachine sm)
        : base(enemy, sm) { }

    public override void Enter()
    {
        enemy.StopMoving();

        if (enemy.PatrolPoints == null || enemy.PatrolPoints.Length == 0)
        {
            stateMachine.ChangeState(enemy.IdleState);
            return;
        }

        currentIndex = FindClosestPointIndex();
        SetNewTarget();
    }

    public override void Update()
    {
        if (enemy.IsPlayerVisible())
        {
            stateMachine.ChangeState(enemy.ChaseState);
            return;
        }

        if (enemy.PatrolPoints == null || enemy.PatrolPoints.Length == 0)
        {
            stateMachine.ChangeState(enemy.IdleState);
            return;
        }

        // 1) Eğer hedefe yaklaştıysak -> next target
        if (IsCloseToTarget())
        {
            GoNextTarget();
            return;
        }

        // 2) Path yoksa -> yeniden iste (return edip kilitlenme yok)
        if (!enemy.Pathfinder.HasPath)
        {
            enemy.PathIndex = 0;
            enemy.Pathfinder.RequestPath(enemy.Transform.position, currentTarget);
            return;
        }

        var path = enemy.Pathfinder.CurrentPath;

        // 3) Path bitti mi? (İŞTE KİLİT NOKTA)
        // PathIndex path.Count'a ulaştığında artık "hedefe ulaştık" kabul edip yeni hedefe geçiyoruz.
        if (enemy.PathIndex >= path.Count)
        {
            GoNextTarget();
            return;
        }

        // 4) Path node üstünde ilerleme
        Vector2 currentNode = path[enemy.PathIndex];

        // Look ahead (daha smooth dönüş)
        Vector2 lookTarget = currentNode;
        if (enemy.PathIndex + 1 < path.Count)
            lookTarget = path[enemy.PathIndex + 1];

        Vector2 dir = lookTarget - (Vector2)enemy.Transform.position;
        enemy.RotateTowards(dir);
        enemy.MoveTowards(currentNode);

        // Node'a geldiysek bir sonraki node
        if (((Vector2)enemy.Transform.position - currentNode).sqrMagnitude <= NodeReachDist * NodeReachDist)
            enemy.PathIndex++;
    }

    private bool IsCloseToTarget()
    {
        return ((Vector2)(enemy.Transform.position - currentTarget)).sqrMagnitude <= TargetReachDist * TargetReachDist;
    }

    private void GoNextTarget()
    {
        enemy.StopMoving();
        AdvanceIndex();
        SetNewTarget();
    }

    private void SetNewTarget()
    {
        if (enemy.PatrolPoints == null || enemy.PatrolPoints.Length == 0)
            return;

        currentTarget = enemy.PatrolPoints[currentIndex];
        enemy.PathIndex = 0;
        enemy.Pathfinder.RequestPath(enemy.Transform.position, currentTarget);
    }

    private void AdvanceIndex()
    {
        if (enemy.PatrolPoints.Length <= 1)
            return;

        currentIndex += direction;

        // Ping-pong (0-1-2-1-0)
        if (currentIndex >= enemy.PatrolPoints.Length)
        {
            currentIndex = enemy.PatrolPoints.Length - 2;
            direction = -1;
        }
        else if (currentIndex < 0)
        {
            currentIndex = 1;
            direction = 1;
        }
    }

    private int FindClosestPointIndex()
    {
        int closest = 0;
        float minDist = float.MaxValue;

        for (int i = 0; i < enemy.PatrolPoints.Length; i++)
        {
            float dist = Vector3.Distance(enemy.Transform.position, enemy.PatrolPoints[i]);
            if (dist < minDist)
            {
                minDist = dist;
                closest = i;
            }
        }

        return closest;
    }
}
public class ChaseState : EnemyState
{
    public ChaseState(EnemyController enemy, EnemyStateMachine sm)
        : base(enemy, sm) { }

    public override void Enter()
    {
        enemy.StopMoving();
        enemy.SetSpeedMultiplier(3f);   // Chase hızın
        enemy.PathIndex = 0;

        if (enemy.Player != null)
            enemy.Pathfinder.StartTrackingPlayer(enemy.Player);
    }

    public override void Exit()
    {
        enemy.ResetSpeed();
        enemy.Pathfinder.StopTracking();
    }

    public override void Update()
    {
        if (enemy.Player == null)
        {
            stateMachine.ChangeState(enemy.IdleState);
            return;
        }

        // Oyuncu görünmüyorsa → direkt PatrolState
        if (!enemy.IsPlayerVisible())
        {
            enemy.Pathfinder.StopTracking();

            if (enemy.PatrolPoints != null && enemy.PatrolPoints.Length > 0)
            {
                Vector3 closest = FindClosestCheckpoint();
                enemy.PathIndex = 0;
                enemy.Pathfinder.RequestPath(enemy.Transform.position, closest);
                stateMachine.ChangeState(enemy.PatrolState);
            }
            else
            {
                stateMachine.ChangeState(enemy.IdleState);
            }

            return;
        }
        else
        {
            // Oyuncu görünür ama path yoksa tracking yeniden başlat
            if (!enemy.Pathfinder.HasPath)
            {
                enemy.PathIndex = 0;
                enemy.Pathfinder.StartTrackingPlayer(enemy.Player);
            }
        }

        if (!enemy.Pathfinder.HasPath)
            return;

        // Path üzerinde ilerle
        if (enemy.PathIndex < enemy.Pathfinder.CurrentPath.Count)
        {
            Vector2 lookTarget = enemy.Pathfinder.CurrentPath[enemy.PathIndex];

            if (enemy.PathIndex + 1 < enemy.Pathfinder.CurrentPath.Count)
                lookTarget = enemy.Pathfinder.CurrentPath[enemy.PathIndex + 1];

            Vector2 dir = lookTarget - (Vector2)enemy.Transform.position;
            enemy.RotateTowards(dir);
            enemy.MoveTowards(enemy.Pathfinder.CurrentPath[enemy.PathIndex]);

            if (Vector2.Distance(enemy.Transform.position, enemy.Pathfinder.CurrentPath[enemy.PathIndex]) < 0.2f)
                enemy.PathIndex++;
        }

        if (enemy.IsInAttackRange())
            stateMachine.ChangeState(enemy.AttackState);
    }

    private Vector3 FindClosestCheckpoint()
    {
        if (enemy.PatrolPoints == null || enemy.PatrolPoints.Length == 0)
            return enemy.Transform.position;

        Vector3 closest = enemy.PatrolPoints[0];
        float minDist = Vector3.Distance(enemy.Transform.position, closest);

        foreach (var point in enemy.PatrolPoints)
        {
            float dist = Vector3.Distance(enemy.Transform.position, point);
            if (dist < minDist)
            {
                minDist = dist;
                closest = point;
            }
        }

        return closest;
    }
}


public class AttackState : EnemyState
{
    private float timer;
    private bool hasAttacked;

    public AttackState(EnemyController enemy, EnemyStateMachine stateMachine)
        : base(enemy, stateMachine) { }

    public override void Enter()
    {
        timer = enemy.Data != null ? enemy.Data.attackDelay : 0.5f;
        hasAttacked = false;
        enemy.StopMoving();
    }

    public override void Update()
    {
        if (enemy.Player == null)
        {
            stateMachine.ChangeState(enemy.IdleState);
            return;
        }

        if (!enemy.IsInAttackRange())
        {
            stateMachine.ChangeState(enemy.ChaseState);
            return;
        }

        enemy.RotateTowards(enemy.AimDirection);
        timer -= Time.deltaTime;

        if (!hasAttacked && timer <= 0f)
        {
            hasAttacked = true;
            enemy.PerformAttack();
        }

        // Attack animasyon aralığı gibi küçük bir extra süre
        if (timer <= -0.3f)
        {
            if (enemy.IsPlayerVisible())
                stateMachine.ChangeState(enemy.ChaseState);
            else
                stateMachine.ChangeState(enemy.IdleState);
        }
    }
}
