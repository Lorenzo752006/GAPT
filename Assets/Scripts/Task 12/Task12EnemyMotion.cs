using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Task12EnemyLocomotion : MonoBehaviour
{
    private Rigidbody2D rb;

    [Header("Movement")]
    [SerializeField] private float acceleration = 12f; 
    [SerializeField] private float maxSpeed = 5f;
    [SerializeField] private float stopDistance = 0.5f; // Small buffer for grid snapping
    [SerializeField] private float brakingDrag = 5f;

    [Header("Physics Feel")]
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float gripStrength = 15f; 

    private Vector2 currentTarget;
    private bool hasTarget = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.linearDamping = 1f; // Provides natural air resistance
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    // This is the ONLY way other scripts should talk to this locomotion
    public void SetSteeringTarget(Vector2 targetPos)
    {
        currentTarget = targetPos;
        hasTarget = true;
    }

    public void Stop()
    {
        hasTarget = false;
        rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, Time.fixedDeltaTime * brakingDrag);
    }

    void FixedUpdate()
    {
        if (!hasTarget) return;

        MoveTowardsTarget();
        ApplyTraction();
        ApplyRotation();
    }

    private void MoveTowardsTarget()
    {
        Vector2 offset = currentTarget - (Vector2)transform.position;
        float distance = offset.magnitude;

        if (distance < stopDistance)
        {
            Stop();
            return;
        }

        // Calculate Desired Velocity
        Vector2 desiredDirection = offset.normalized;
        Vector2 desiredVelocity = desiredDirection * maxSpeed;

        // Steering Force: Force = Desired - Current
        Vector2 steeringForce = (desiredVelocity - rb.linearVelocity) * acceleration;
        
        rb.AddForce(steeringForce);

        // Speed Cap
        if (rb.linearVelocity.magnitude > maxSpeed)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
        }
    }

    private void ApplyTraction()
    {
        Vector2 right = transform.right;
        float drift = Vector2.Dot(rb.linearVelocity, right);
        rb.AddForce(-right * drift * gripStrength, ForceMode2D.Force);
    }

    private void ApplyRotation()
    {
        if (rb.linearVelocity.magnitude > 0.1f)
        {
            float angle = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg - 90f;
            Quaternion moveRotation = Quaternion.Euler(0, 0, angle);
            transform.rotation = Quaternion.Slerp(transform.rotation, moveRotation, rotationSpeed * Time.fixedDeltaTime);
        }
    }
}