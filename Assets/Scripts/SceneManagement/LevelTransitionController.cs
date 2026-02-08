using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelTransitionController : MonoBehaviour
{
    public static LevelTransitionController Instance { get; private set; }

    [Header("Scene Names")]
    [SerializeField] private string mainMenuScene = "MainMenu";

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

    public void LoadLevel(string sceneName)
    {
        GameManager.Instance?.SetGameState(GameState.Playing);
        GameEvents.RaiseSceneLoadRequested(sceneName);
    }

    public void LoadCutscene(string cutsceneScene)
    {
        GameManager.Instance?.SetGameState(GameState.Cutscene);
        GameEvents.RaiseSceneLoadRequested(cutsceneScene);
    }

    public void ReturnToMainMenu()
    {
        GameManager.Instance?.SetGameState(GameState.MainMenu);
        GameEvents.RaiseSceneLoadRequested(mainMenuScene);
    }

    public void RestartLevel()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        LoadLevel(currentScene);
    }
}
