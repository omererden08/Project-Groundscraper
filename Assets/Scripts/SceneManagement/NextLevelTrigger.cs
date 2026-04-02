using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class NextLevelTrigger : MonoBehaviour
{
    private BoxCollider2D triggerCollider;

    private void Awake()
    {
        triggerCollider = GetComponent<BoxCollider2D>();
        triggerCollider.isTrigger = true;
        triggerCollider.enabled = false;
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

        Debug.Log("Exit trigger activated.");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (StageClearManager.Instance != null && StageClearManager.Instance.AliveEnemyCount > 0)
            return;

        Transform weaponHoldPoint = other.transform.Find("WeaponHoldPoint");
        if (weaponHoldPoint != null)
        {
            for (int i = weaponHoldPoint.childCount - 1; i >= 0; i--)
            {
                Object.Destroy(weaponHoldPoint.GetChild(i).gameObject);
            }
        }

        LevelTransitionController.Instance?.LoadNextLevel();
    }
}