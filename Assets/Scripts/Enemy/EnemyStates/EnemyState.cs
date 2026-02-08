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
    public IdleState(EnemyController enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine) { }

    public override void Enter() => enemy.StopMoving();

    public override void Update()
    {
        if (enemy.IsPlayerVisible())
        {
            if (enemy.Data.isRanged && enemy.IsInAttackRange())
                stateMachine.ChangeState(enemy.AttackState);
            else
                stateMachine.ChangeState(enemy.ChaseState);
        }
    }


}

public class PatrolState : EnemyState
{
    private int currentIndex = 0;
    private int direction = 1;
    private Vector3 currentTarget;

    public PatrolState(EnemyController enemy, EnemyStateMachine sm) : base(enemy, sm) { }

    public override void Enter()
    {
        enemy.StopMoving();
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

        if (!enemy.Pathfinder.HasPath)
            return;

        // 🔄 Pathte ilerle
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

        // 🎯 Hedef nokta yakınsa bir sonrakine geç
        if (Vector2.Distance(enemy.Transform.position, currentTarget) < 0.3f)
        {
            AdvanceIndex();
            SetNewTarget();
        }
    }

    private void SetNewTarget()
    {
        currentTarget = enemy.PatrolPoints[currentIndex];
        enemy.PathIndex = 0;
        enemy.Pathfinder.RequestPath(enemy.Transform.position, currentTarget);
    }

    private void AdvanceIndex()
    {
        currentIndex += direction;

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
    public ChaseState(EnemyController enemy, EnemyStateMachine sm) : base(enemy, sm) { }

    public override void Enter()
    {
        enemy.StopMoving();
        enemy.SetSpeedMultiplier(1.5f); // %50 daha hızlı örnek
        enemy.PathIndex = 0;
        enemy.Pathfinder.StartTrackingPlayer(enemy.Player);
    }


    public override void Exit()
    {
        enemy.ResetSpeed();
        enemy.Pathfinder.StopTracking();
    }


    public override void Update()
    {
        if (!enemy.IsPlayerVisible())
        {
            // En yakın checkpoint'e geri dön
            Vector3 closest = FindClosestCheckpoint();
            enemy.PathIndex = 0;
            enemy.Pathfinder.RequestPath(enemy.Transform.position, closest);
            stateMachine.ChangeState(enemy.PatrolState);
            return;
        }

        if (!enemy.Pathfinder.HasPath)
            return;

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

    public AttackState(EnemyController enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine) { }

    public override void Enter()
    {
        timer = enemy.Data.attackDelay;
        hasAttacked = false;
        enemy.StopMoving();
    }

    public override void Update()
    {
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

        if (timer <= -0.3f)
        {
            stateMachine.ChangeState(enemy.IsPlayerVisible() ? enemy.ChaseState : enemy.IdleState);
        }
    }
}



