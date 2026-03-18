using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class NextLevelTrigger : MonoBehaviour
{
    private void Reset()
    {
        var col = GetComponent<BoxCollider2D>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        LevelTransitionController.Instance?.LoadNextLevel();
    }
}