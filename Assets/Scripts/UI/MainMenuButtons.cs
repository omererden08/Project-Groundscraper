using UnityEngine;

public class MainMenuButtons : MonoBehaviour
{

    private void OnEnable()
    {
        GameManager.Instance?.SetGameState(GameState.MainMenu);
    }

    public void OnClickStart()
    {
        LevelTransitionController.Instance.StartGame();
    }

    public void OnClickQuit()
    {
        GameEvents.RaiseQuitRequested();
    }
}