using System.Collections.Generic;
using UnityEngine;

// Basic enemy health component.
// Also keeps a static registry of living enemies so the player's slash attack
// can check them without doing a scene-wide search every frame.
public class EnemyHealth : MonoBehaviour
{
    public static readonly HashSet<EnemyHealth> Active = new HashSet<EnemyHealth>();

    public float maxHealth = 100f;
    public float currentHealth = 100f;

    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;

    void OnEnable()
    {
        Active.Add(this);
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
    }

    void OnDisable()
    {
        Active.Remove(this);
    }

    public void TakeDamage(float amount)
    {
        currentHealth = Mathf.Max(0f, currentHealth - Mathf.Max(0f, amount));
        if (currentHealth <= 0f)
            Destroy(gameObject);
    }
}
