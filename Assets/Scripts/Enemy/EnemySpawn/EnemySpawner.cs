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
            var go = new GameObject("EnemiesRoot");
            enemiesRoot = go.transform;
        }

        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        if (pool == null)
        {
            pool = FindFirstObjectByType<EnemyPool>();

        }
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
            Debug.LogError("EnemySpawner: EnemyPool referansý yok!");
            return;
        }

        // 1) Eski düþmanlarý temizle
        pool.DespawnAllAlive();

        // 2) Yeni level spawnlarýný baþlat
        if (spawnRoutine != null) StopCoroutine(spawnRoutine);
        spawnRoutine = StartCoroutine(SpawnFromCurrentLevelRoutine());
    }

    private IEnumerator SpawnFromCurrentLevelRoutine()
    {
        yield return null; // instantiate sonrasý 1 frame güvenli

        if (LevelLoader.Instance == null || LevelLoader.Instance.CurrentLevelInstance == null)
        {
            Debug.LogError("EnemySpawner: LevelLoader veya CurrentLevelInstance null!");
            yield break;
        }

        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        if (player == null)
        {
            Debug.LogError("EnemySpawner: Player bulunamadý! Player tag var mý?");
            yield break;
        }

        var levelGo = LevelLoader.Instance.CurrentLevelInstance;

        // SADECE bu level instance içinden spawn point topla (FindObjectsOfType YOK)
        var spawns = levelGo.GetComponentsInChildren<EnemySpawnPoint>(true);

        for (int i = 0; i < spawns.Length; i++)
        {
            var sp = spawns[i];
            if (sp == null || !sp.spawnOnLevelStart) continue;

            if (sp.delay > 0f)
                yield return new WaitForSeconds(sp.delay);

            pool.Spawn(
                sp.type,
                sp.transform.position,
                sp.transform.rotation,
                enemiesRoot,
                player,
                sp.patrolRootOverride
            );
        }
    }

    // Alarm/trigger gibi durumlarda tek spawn
    public void SpawnSingle(EnemySpawnPoint sp)
    {
        if (sp == null || pool == null) return;

        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
        if (player == null) return;

        pool.Spawn(sp.type, sp.transform.position, sp.transform.rotation, enemiesRoot, player, sp.patrolRootOverride);
    }
}