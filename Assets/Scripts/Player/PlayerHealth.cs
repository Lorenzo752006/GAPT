using UnityEngine;

// Minimal player health component.
public class PlayerHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    public float currentHealth = 100f;

    void Awake()
    {
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
    }

    // Normalized health value in the 0..1 range.
    public float Health01 => maxHealth <= 0.0001f ? 0f : currentHealth / maxHealth;

    public void TakeDamage(float amount)
    {
        currentHealth = Mathf.Max(0f, currentHealth - Mathf.Max(0f, amount));
    }
}
