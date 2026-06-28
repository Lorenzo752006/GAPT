using UnityEngine;

// Utility script to ignore collision between one enemy collider and the player collider.
public class IgnorePlayerCollision : MonoBehaviour
{
    [SerializeField] private Collider2D enemyCollider;
    [SerializeField] private Collider2D playerCollider;

    void Awake()
    {
        if (!enemyCollider) enemyCollider = GetComponent<Collider2D>();
        if (!playerCollider) playerCollider = GameObject.FindWithTag("Player")?.GetComponent<Collider2D>();

        if (enemyCollider && playerCollider)
            Physics2D.IgnoreCollision(enemyCollider, playerCollider, true);
    }
}
