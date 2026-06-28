using UnityEngine;

// Simple baseline locomotion: always accelerate directly toward the player.
public class EnemyLocomotionSimpleTask6 : MonoBehaviour
{
    public Transform player;
    public float speed = 5f;
    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void FixedUpdate()
    {
        if (player == null || rb == null)
            return;

        Vector2 direction = (player.position - transform.position).normalized;
        rb.AddForce(direction * speed, ForceMode2D.Force);

        // Rotate the sprite/body to face its current movement direction.
        if (rb.linearVelocity.magnitude > 0.1f)
        {
            float angle = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg - 90f;
            rb.rotation = angle;
        }
    }
}
