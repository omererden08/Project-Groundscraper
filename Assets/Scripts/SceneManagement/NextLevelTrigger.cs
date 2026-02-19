using UnityEngine;

[RequireComponent (typeof(BoxCollider2D))]
public class NextLevelTrigger : MonoBehaviour
{
    [SerializeField] private string levelName;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            LevelTransitionController.Instance.LoadLevel(levelName);
        }
    }
}
