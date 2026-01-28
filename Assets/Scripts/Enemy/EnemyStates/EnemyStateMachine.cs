using UnityEngine;
public class EnemyStateMachine
{
    public EnemyState CurrentState { get; private set; }

    public void Initialize(EnemyState startingState)
    {
        CurrentState = startingState;
        CurrentState.Enter();
    }

    public void ChangeState(EnemyState newState)
    {
        if (CurrentState == newState) return; 

        CurrentState?.Exit();
        CurrentState = newState;
        CurrentState.Enter();
    }

    public void Update()
    {
        CurrentState?.Update();
    }
}

