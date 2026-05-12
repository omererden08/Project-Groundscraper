using UnityEngine;

public class BacktoMenu : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Input")]
    [SerializeField] private bool useAttackInput = true;
    [SerializeField] private bool useInteractInput = false;

    private bool triggered;
    private bool inputManagerWarningShown;

    private void Update()
    {
        if (triggered)
            return;

        if (WasClickPressed())
        {
            triggered = true;
            GoToMainMenu();
        }
    }

    private bool WasClickPressed()
    {
        if (InputManager.Instance == null)
        {
            if (!inputManagerWarningShown)
            {
                inputManagerWarningShown = true;
                Debug.LogError("BacktoMenu: InputManager.Instance bulunamadı!");
            }

            return false;
        }

        return InputManager.Instance.TryConsumeCutsceneAdvanceInput(
            useAttackInput,
            useInteractInput
        );
    }

    private void GoToMainMenu()
    {
        GameManager.Instance?.SetGameState(GameState.MainMenu);
        GameEvents.RaiseSceneLoadRequested(mainMenuSceneName);
    }
}