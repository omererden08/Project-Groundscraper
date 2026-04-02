using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelTransitionController : MonoBehaviour
{
    public static LevelTransitionController Instance { get; private set; }

    [Header("Scenes")]
    [SerializeField] private string mainMenuScene = "MainMenu";
    [SerializeField] private string gameplayScene = "Gameplay";

    [Header("Levels")]
    [SerializeField] private LevelDatabase levelDatabase;
    [SerializeField] private string firstLevelId = "Level 1";

    private string pendingLevelId;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        GameEvents.OnSceneLoadCompleted += HandleSceneLoadCompleted;
        GameEvents.OnLevelLoadRequested += HandleLevelLoadRequested;
    }

    private void OnDisable()
    {
        GameEvents.OnSceneLoadCompleted -= HandleSceneLoadCompleted;
        GameEvents.OnLevelLoadRequested -= HandleLevelLoadRequested;
    }

    public void StartGame()
    {
        LoadLevelById(firstLevelId);
    }

    public void LoadLevelById(string levelId)
    {
        pendingLevelId = levelId;

        if (!IsSceneLoaded(gameplayScene))
        {
            GameManager.Instance?.SetGameState(GameState.Playing);
            GameEvents.RaiseSceneLoadRequested(gameplayScene);
            return;
        }

        LoadPendingLevelNow();
    }

    public void ReturnToMainMenu()
    {
        pendingLevelId = null;
        GameManager.Instance?.SetGameState(GameState.MainMenu);
        GameEvents.RaiseSceneLoadRequested(mainMenuScene);
    }

    public void RestartLevel()
    {
        GameManager.Instance?.SetGameState(GameState.Playing);
        LevelLoader.Instance?.Restart();
    }

    private void HandleLevelLoadRequested(string levelId)
    {
        LoadLevelById(levelId);
    }

    private void HandleSceneLoadCompleted(string sceneName)
    {
        if (sceneName == gameplayScene && !string.IsNullOrEmpty(pendingLevelId))
        {
            LoadPendingLevelNow();
        }
    }

    private void LoadPendingLevelNow()
    {
        var data = levelDatabase != null ? levelDatabase.GetById(pendingLevelId) : null;
        if (data == null)
        {
            Debug.LogError($"LevelTransitionController: LevelData bulunamadý: {pendingLevelId}");
            return;
        }

        if (LevelLoader.Instance == null)
        {
            Debug.LogError("LevelTransitionController: LevelLoader.Instance null! Gameplay sahnesinde LevelLoader var mý?");
            return;
        }

        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null)
            player.PreserveEquippedWeaponForLevelTransition();

        LevelLoader.Instance.Load(data);

        if (player != null)
            player.RestoreEquippedWeaponAfterLevelTransition();

        GameManager.Instance?.SetGameState(GameState.Playing);
        pendingLevelId = null;
    }

    public void LoadNextLevel()
    {
        if (LevelLoader.Instance == null || levelDatabase == null)
            return;

        var current = LevelLoader.Instance.CurrentLevel;
        if (current == null)
            return;

        int currentIndex = levelDatabase.GetLevelIndex(current.levelId);
        int nextIndex = currentIndex + 1;

        var next = levelDatabase.GetByIndex(nextIndex);
        if (next == null)
        {
            ReturnToMainMenu();
            return;
        }

        LoadLevelById(next.levelId);
    }

    private bool IsSceneLoaded(string sceneName)
    {
        var s = SceneManager.GetSceneByName(sceneName);
        return s.IsValid() && s.isLoaded;
    }
}