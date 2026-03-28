using System;
using UnityEngine;
using UnityEngine.Events;

public class StageClearManager : MonoBehaviour
{
    public static StageClearManager Instance { get; private set; }

    public event Action OnStageCleared;

    [Header("Debug")]
    [SerializeField] private int aliveEnemyCount;

    [Header("Events")]
    [SerializeField] private UnityEvent onStageCleared;

    private bool stageClearTriggered;

    public int AliveEnemyCount => aliveEnemyCount;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void ResetStage()
    {
        aliveEnemyCount = 0;
        stageClearTriggered = false;
    }

    public void RegisterEnemy(EnemyBase enemy)
    {
        if (enemy == null)
            return;

        aliveEnemyCount++;
        Debug.Log($"[Stage] Register: {enemy.name} | Alive Count: {aliveEnemyCount}");
    }

    public void UnregisterEnemy(EnemyBase enemy)
    {
        if (enemy == null)
            return;

        aliveEnemyCount--;

        if (aliveEnemyCount < 0)
            aliveEnemyCount = 0;

        Debug.Log($"[Stage] Unregister: {enemy.name} | Alive Count: {aliveEnemyCount}");

        CheckStageClear();
    }

    private void CheckStageClear()
    {
        if (stageClearTriggered)
            return;

        if (aliveEnemyCount == 0)
        {
            stageClearTriggered = true;

            Debug.Log("Stage Clear");

            OnStageCleared?.Invoke();
            onStageCleared?.Invoke();
        }
    }
}