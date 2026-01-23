using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class EnemyController : MonoBehaviour, IDamageable
{
    [Header("Debug")]
    [SerializeField] private bool isDead = false;

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer == null)
        {
            Debug.LogWarning("Enemy has no SpriteRenderer!");
        }
    }

    public void Die()
    {
        if (isDead) return;

        isDead = true;

        // 🔴 Sprite kırmızıya boyanır
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.red;
        }

        Debug.Log($"☠️ Enemy {name} died.");

    }
}
