using UnityEngine;

public class SimpleEnemy : MonoBehaviour
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
        Vector2 direction = (player.position - transform.position).normalized;

        rb.AddForce(direction * speed, ForceMode2D.Force);

        if (rb.linearVelocity.magnitude > 0.1f )
        {
            float angle = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg -90f;
            rb.rotation = angle;
        }
    }
}