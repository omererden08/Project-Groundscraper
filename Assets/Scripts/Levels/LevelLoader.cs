using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class LevelLoader : MonoBehaviour
{
    public static LevelLoader Instance { get; private set; }

    [Header("Scene References")]
    [SerializeField] private Transform levelRoot;
    [SerializeField] private Transform playerTransform;

    [Header("Player")]
    [SerializeField] private GameObject playerPrefab;

    private GameObject currentLevelInstance;
    private LevelData currentLevel;

    public GameObject CurrentLevelInstance => currentLevelInstance;
    public LevelData CurrentLevel => currentLevel;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        ResolveInitialReferences();
    }

    private void ResolveInitialReferences()
    {
        if (levelRoot == null)
        {
            GameObject root = GameObject.Find("LevelRoot");
            if (root != null)
                levelRoot = root.transform;
        }

        if (levelRoot == null)
        {
            GameObject root = new GameObject("LevelRoot");
            levelRoot = root.transform;
        }

        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                playerTransform = player.transform;
        }
    }

    public void Load(LevelData data)
    {
        if (data == null)
        {
            Debug.LogError("LevelLoader.Load: LevelData null!");
            return;
        }

        if (data.levelPrefab == null)
        {
            Debug.LogError($"LevelLoader.Load: {data.name} içinde levelPrefab null!");
            return;
        }

        StopAllCoroutines();
        StartCoroutine(LoadRoutine(data));
    }

    public void Restart()
    {
        if (currentLevel == null)
        {
            Debug.LogWarning("LevelLoader.Restart: CurrentLevel null.");
            return;
        }

        Load(currentLevel);
    }

    private IEnumerator LoadRoutine(LevelData data)
    {
        CleanupAllWeapons();

        if (currentLevelInstance != null)
        {
            Destroy(currentLevelInstance);
            currentLevelInstance = null;
        }

        yield return null;

        currentLevel = data;
        currentLevelInstance = Instantiate(data.levelPrefab, levelRoot);

        LevelSpawnPoint spawnPoint = currentLevelInstance.GetComponentInChildren<LevelSpawnPoint>(true);

        EnsurePlayerExists(spawnPoint);
        PlacePlayerAtSpawn(spawnPoint);
        ResetPlayerState();
        AssignCameraTarget();

        Time.timeScale = 1f;

        GameEvents.RaiseLevelLoaded(data.levelId);
    }

    private void EnsurePlayerExists(LevelSpawnPoint spawnPoint)
    {
        if (playerTransform != null)
            return;

        GameObject existingPlayer = GameObject.FindGameObjectWithTag("Player");
        if (existingPlayer != null)
        {
            playerTransform = existingPlayer.transform;
            return;
        }

        if (playerPrefab == null)
        {
            Debug.LogError("LevelLoader: Player yok ve playerPrefab atanmadı!");
            return;
        }

        Vector3 spawnPos = spawnPoint != null ? spawnPoint.transform.position : Vector3.zero;
        GameObject playerObj = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
        playerTransform = playerObj.transform;
    }

    private void CleanupAllWeapons()
    {
#if UNITY_2022_1_OR_NEWER
        var weapons = FindObjectsByType<RangedWeapon>(FindObjectsSortMode.None);
#else
        var weapons = FindObjectsOfType<RangedWeapon>(true);
#endif

        for (int i = 0; i < weapons.Length; i++)
        {
            if (weapons[i] != null)
                Destroy(weapons[i].gameObject);
        }
    }

    private void PlacePlayerAtSpawn(LevelSpawnPoint spawnPoint)
    {
        if (playerTransform == null || spawnPoint == null)
            return;

        playerTransform.position = spawnPoint.transform.position;
    }

    private void ResetPlayerState()
    {
        if (playerTransform == null)
            return;

        PlayerController playerController = playerTransform.GetComponent<PlayerController>();
        if (playerController != null)
        {
            playerController.enabled = true;
            playerController.ResetForRestart();
        }

        Collider2D[] colliders = playerTransform.GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
                colliders[i].enabled = true;
        }

        Rigidbody2D rb = playerTransform.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.simulated = true;
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

#if ENABLE_INPUT_SYSTEM
        PlayerInput playerInput = playerTransform.GetComponent<PlayerInput>();
        if (playerInput != null)
            playerInput.ActivateInput();
#endif
    }

    private void AssignCameraTarget()
    {
        if (playerTransform == null)
            return;

#if UNITY_2022_1_OR_NEWER
        CameraController cam = FindFirstObjectByType<CameraController>();
#else
        CameraController cam = FindObjectOfType<CameraController>();
#endif
        if (cam != null)
            cam.SetTarget(playerTransform);
    }
}