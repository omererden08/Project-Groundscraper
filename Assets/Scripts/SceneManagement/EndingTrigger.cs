using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(BoxCollider2D))]
public class EndingTrigger : MonoBehaviour
{
    [Header("Ending Scene")]
    [SerializeField] private string endingSceneName = "Ending";

    [Header("Settings")]
    [SerializeField] private bool activateOnlyAfterStageClear = true;
    [SerializeField] private bool destroyPlayerWeaponOnEnter = true;
    [SerializeField] private bool setGameStateToCutscene = true;

    private BoxCollider2D triggerCollider;
    private bool triggered;

    private void Awake()
    {
        triggerCollider = GetComponent<BoxCollider2D>();
        triggerCollider.isTrigger = true;

        if (activateOnlyAfterStageClear)
            triggerCollider.enabled = false;
        else
            triggerCollider.enabled = true;
    }

    private void OnEnable()
    {
        if (StageClearManager.Instance != null)
            StageClearManager.Instance.OnStageCleared += HandleStageCleared;
    }

    private void OnDisable()
    {
        if (StageClearManager.Instance != null)
            StageClearManager.Instance.OnStageCleared -= HandleStageCleared;
    }

    private void HandleStageCleared()
    {
        if (triggerCollider != null)
            triggerCollider.enabled = true;

        Debug.Log("Ending trigger activated.");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered)
            return;

        if (!other.CompareTag("Player"))
            return;

        if (activateOnlyAfterStageClear &&
            StageClearManager.Instance != null &&
            StageClearManager.Instance.AliveEnemyCount > 0)
        {
            return;
        }

        triggered = true;

        if (triggerCollider != null)
            triggerCollider.enabled = false;

        if (destroyPlayerWeaponOnEnter)
            DestroyPlayerHeldWeapons(other.transform);

        LoadEndingScene();
    }

    private void DestroyPlayerHeldWeapons(Transform playerTransform)
    {
        Transform weaponHoldPoint = playerTransform.Find("WeaponHoldPoint");

        if (weaponHoldPoint == null)
            return;

        for (int i = weaponHoldPoint.childCount - 1; i >= 0; i--)
        {
            Destroy(weaponHoldPoint.GetChild(i).gameObject);
        }
    }

    private void LoadEndingScene()
    {
        if (string.IsNullOrEmpty(endingSceneName))
        {
            Debug.LogError("EndingTrigger: Ending Scene Name boş!");
            return;
        }

        if (setGameStateToCutscene)
            GameManager.Instance?.SetGameState(GameState.Cutscene);

        GameEvents.RaiseSceneLoadRequested(endingSceneName);
    }
}