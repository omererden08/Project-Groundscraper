using System.Collections;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemyPool pool;
    [SerializeField] private Transform enemiesRoot;
    [SerializeField] private Transform player;

    private Coroutine spawnRoutine;

    private void Awake()
    {
        if (enemiesRoot == null)
        {
            GameObject go = new GameObject("EnemiesRoot");
            enemiesRoot = go.transform;
        }

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
                player = p.transform;
        }

        if (pool == null)
            pool = FindFirstObjectByType<EnemyPool>();
    }

    private void OnEnable()
    {
        GameEvents.OnLevelLoaded += HandleLevelLoaded;
    }

    private void OnDisable()
    {
        GameEvents.OnLevelLoaded -= HandleLevelLoaded;
    }

    private void HandleLevelLoaded(string levelId)
    {
        if (pool == null)
        {
            Debug.LogError("EnemySpawner: EnemyPool referansý yok.");
            return;
        }

        if (StageClearManager.Instance != null)
            StageClearManager.Instance.ResetStage();

        pool.DespawnAllAlive();

        if (spawnRoutine != null)
            StopCoroutine(spawnRoutine);

        spawnRoutine = StartCoroutine(SpawnFromCurrentLevelRoutine());
    }

    private IEnumerator SpawnFromCurrentLevelRoutine()
    {
        yield return null;

        if (LevelLoader.Instance == null || LevelLoader.Instance.CurrentLevelInstance == null)
        {
            Debug.LogError("EnemySpawner: LevelLoader veya CurrentLevelInstance null.");
            yield break;
        }

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
                player = p.transform;
        }

        if (player == null)
        {
            Debug.LogError("EnemySpawner: Player bulunamadý. Player tag kontrol et.");
            yield break;
        }

        GameObject levelGo = LevelLoader.Instance.CurrentLevelInstance;
        EnemySpawnPoint[] spawns = levelGo.GetComponentsInChildren<EnemySpawnPoint>(true);

        for (int i = 0; i < spawns.Length; i++)
        {
            EnemySpawnPoint sp = spawns[i];

            if (sp == null || !sp.spawnOnLevelStart)
                continue;

            if (sp.delay > 0f)
                yield return new WaitForSeconds(sp.delay);

            SpawnInternal(sp);
        }
    }

    public void SpawnSingle(EnemySpawnPoint sp)
    {
        if (sp == null || pool == null)
            return;

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
                player = p.transform;
        }

        if (player == null)
            return;

        SpawnInternal(sp);
    }

    private void SpawnInternal(EnemySpawnPoint sp)
    {
        EnemyBase enemy = pool.Spawn(
            sp.type,
            sp.transform.position,
            sp.transform.rotation,
            enemiesRoot,
            player,
            sp.patrolRootOverride
        );

        if (enemy != null)
            enemy.RegisterToStage();
    }
}