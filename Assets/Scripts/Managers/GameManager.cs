using UnityEngine;
using UnityEngine.SceneManagement;
using System;

public enum GameState { MainMenu, Playing, Paused, GameOver }

public static class GameEvents
{
    public static event Action<GameState> OnGameStateChanged;
    public static event Action OnGamePaused;
    public static event Action OnGameResumed;
    public static event Action OnGameOver;
    public static event Action OnLevelRestarted;

    public static void RaiseGameStateChanged(GameState newState) => OnGameStateChanged?.Invoke(newState);
    public static void RaiseGamePaused() => OnGamePaused?.Invoke();
    public static void RaiseGameResumed() => OnGameResumed?.Invoke();
    public static void RaiseGameOver() => OnGameOver?.Invoke();
    public static void RaiseLevelRestarted() => OnLevelRestarted?.Invoke();
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Debug")]
    [SerializeField] private GameState currentState;

    public GameState CurrentState => currentState;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        // Player öldüğünde level restartla
        PlayerEvents.OnPlayerDied += RestartLevel;
    }

    private void OnDisable()
    {
        PlayerEvents.OnPlayerDied -= RestartLevel;
    }

    private void Update()
    {
        if (InputManager.Instance != null && InputManager.Instance.PausePressed)
        {
            if (CurrentState == GameState.Playing)
                SetGameState(GameState.Paused);
            else if (CurrentState == GameState.Paused)
                SetGameState(GameState.Playing);

            InputManager.Instance.ConsumePauseInput();
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

            case GameState.MainMenu:
                Time.timeScale = 1f;
                break;
        }

        Debug.Log($"🟡 Game State changed to: {newState}");
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        GameEvents.RaiseLevelRestarted();

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        SetGameState(GameState.Playing);
    }
}
