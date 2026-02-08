using UnityEngine;

public class MainMenuButtons : MonoBehaviour
{
    [SerializeField] private string gameplaySceneName = "Level 1";

    private void OnEnable()
    {
        GameManager.Instance?.SetGameState(GameState.MainMenu);
    }

    public void OnClickStart()
    {
        LevelTransitionController.Instance?.LoadLevel(gameplaySceneName);
    }

    public void OnClickQuit()
    {
        GameEvents.RaiseQuitRequested();
    }
}
