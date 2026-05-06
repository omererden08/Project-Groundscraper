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
            GameEvents.RaiseSceneLoadRequested(sceneAfterCutscene);
            sceneAfterCutscene = null;
        }
    }

    public void SetGameState(GameState newState)
    {
        if (currentState == newState) return;

        currentState = newState;
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
}