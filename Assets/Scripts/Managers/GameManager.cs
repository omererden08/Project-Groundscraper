using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private GameState currentState = GameState.MainMenu;
    public GameState CurrentState => currentState;

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
            case GameState.MainMenu:
                Time.timeScale = 1f;
                break;
        }
    }
}