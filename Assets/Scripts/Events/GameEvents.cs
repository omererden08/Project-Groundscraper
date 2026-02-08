using System;

public enum GameState
{
    MainMenu,
    Playing,
    Paused,
    GameOver,
    Cutscene
}

public static class GameEvents
{
    public static event Action<GameState> OnGameStateChanged;
    public static event Action OnGamePaused;
    public static event Action OnGameResumed;
    public static event Action OnGameOver;
    public static event Action OnQuitRequested;

    public static event Action<string> OnSceneLoadRequested;
    public static event Action<string> OnSceneLoadStarted;
    public static event Action<string> OnSceneLoadCompleted;

    public static void RaiseGameStateChanged(GameState state) => OnGameStateChanged?.Invoke(state);
    public static void RaiseGamePaused() => OnGamePaused?.Invoke();
    public static void RaiseGameResumed() => OnGameResumed?.Invoke();
    public static void RaiseGameOver() => OnGameOver?.Invoke();
    public static void RaiseQuitRequested() => OnQuitRequested?.Invoke();

    public static void RaiseSceneLoadRequested(string sceneName) => OnSceneLoadRequested?.Invoke(sceneName);
    public static void RaiseSceneLoadStarted(string sceneName) => OnSceneLoadStarted?.Invoke(sceneName);
    public static void RaiseSceneLoadCompleted(string sceneName) => OnSceneLoadCompleted?.Invoke(sceneName);
}
