using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private GameState currentState = GameState.MainMenu;

    public GameState CurrentState => currentState;

    private string sceneAfterCutscene;

    public string SceneAfterCutscene => sceneAfterCutscene;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        ApplyCursorState(currentState);
    }

    private void OnEnable()
    {
        GameEvents.OnCutsceneRequested += HandleCutsceneRequested;
        GameEvents.OnCutsceneFinished += HandleCutsceneFinished;
    }

    private void OnDisable()
    {
        GameEvents.OnCutsceneRequested -= HandleCutsceneRequested;
        GameEvents.OnCutsceneFinished -= HandleCutsceneFinished;
    }

    private void HandleCutsceneRequested(string cutsceneSceneName, string nextSceneName)
    {
        sceneAfterCutscene = nextSceneName;

        SetGameState(GameState.Cutscene);

        GameEvents.RaiseCutsceneStarted();
        GameEvents.RaiseSceneLoadRequested(cutsceneSceneName);
    }

    private void HandleCutsceneFinished()
    {
        SetGameState(GameState.Playing);

        if (!string.IsNullOrEmpty(sceneAfterCutscene))
        {
            string nextScene = sceneAfterCutscene;
            sceneAfterCutscene = null;

            GameEvents.RaiseSceneLoadRequested(nextScene);
        }
    }

    public void SetGameState(GameState newState)
    {
        if (currentState == newState)
            return;

        currentState = newState;

        ApplyCursorState(newState);

        GameEvents.RaiseGameStateChanged(newState);

        switch (newState)
        {
            case GameState.Paused:
                Time.timeScale = 0f;
                GameEvents.RaiseGamePaused();
                break;

            case GameState.Playing:
                Time.timeScale = 1f;
                GameEvents.RaiseGameResumed();
                break;

            case GameState.GameOver:
                Time.timeScale = 0f;
                GameEvents.RaiseGameOver();
                break;

            case GameState.Cutscene:
                Time.timeScale = 1f;
                break;

            case GameState.MainMenu:
                Time.timeScale = 1f;
                break;
        }
    }

    private void ApplyCursorState(GameState state)
    {
        switch (state)
        {
            case GameState.MainMenu:
                // MainMenu'de normal mouse cursor görünsün.
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                break;

            case GameState.Playing:
                // Gameplay'de sprite cursor kullanacağın için gerçek cursor gizli.
                // Mouse kilitli değil, böylece sprite cursor mouse pozisyonunu takip edebilir.
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.None;
                break;

            case GameState.Cutscene:
                // Cutscene sırasında cursor görünmesin ve kilitli olsun.
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
                break;

            case GameState.Paused:
                // Senin isteğine göre MainMenu dışında cursor görünmüyor.
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
                break;

            case GameState.GameOver:
                // GameOver'da da cursor görünmesin.
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
                break;
        }
    }
}