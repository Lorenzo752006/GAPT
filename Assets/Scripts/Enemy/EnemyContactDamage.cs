using UnityEngine;

public class EnemyContactDamage : MonoBehaviour
{
    public float damagePerSecond = 20f;

    void OnCollisionStay2D(Collision2D col)
    {
        var hp = col.gameObject.GetComponent<PlayerHealth>();
        if (hp != null)
            hp.TakeDamage(damagePerSecond * Time.deltaTime);
    }

    void OnTriggerStay2D(Collider2D col)
    {
        var hp = col.gameObject.GetComponent<PlayerHealth>();
        if (hp != null)
            hp.TakeDamage(damagePerSecond * Time.deltaTime);
    }
}