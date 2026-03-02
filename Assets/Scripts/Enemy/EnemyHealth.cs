using System.Collections.Generic;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    // This is what PlayerSlashAttack expects
    public static readonly HashSet<EnemyHealth> Active = new HashSet<EnemyHealth>();

    public float maxHealth = 100f;
    public float currentHealth = 100f;

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