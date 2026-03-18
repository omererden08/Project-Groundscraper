using UnityEngine;

public class MainMenuButtons : MonoBehaviour
{
    [SerializeField] private string firstLevelId = "Level 1";

    private void OnEnable()
    {
        GameManager.Instance?.SetGameState(GameState.MainMenu);
    }

    public void OnClickStart()
    {
        LevelTransitionController.Instance?.LoadLevelById(firstLevelId);
    }

    public void OnClickQuit()
    {
        GameEvents.RaiseQuitRequested();
    }
}