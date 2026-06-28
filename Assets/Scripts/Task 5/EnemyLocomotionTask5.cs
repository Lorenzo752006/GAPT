using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyLocomotionTask5 : MonoBehaviour
{
    private Rigidbody2D rb;
    private Vector2[] rayDirections;
    private float[] lastFrameInterests;

    [SerializeField] private Transform target;

    [Header("Movement")]
    [SerializeField] private float acceleration = 12f;
    [SerializeField] private float maxSpeed = 5f;
    [SerializeField] private float stopDistance = 2f;
    [SerializeField] private float brakeStrength = 5f;

    [Header("Physics Feel")]
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float gripStrength = 15f;
    [SerializeField] private float arrivalSharpness = 1.5f;

    [Header("AI Logic")]
    [SerializeField] private bool flee = false;

    [Header("Context Steering")]
    [SerializeField] private int rayCount = 12;
    [SerializeField] private float detectionRange = 4.5f;
    [SerializeField] private float wallDangerWeight = 2.0f;
    [SerializeField] private float agentRadius = 0.4f;
    [SerializeField] private LayerMask wallLayer;

    private void Awake()
    {
        BuildRayDirections();
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    private void FixedUpdate()
    {
        if (target == null)
            return;

        Vector2 desiredVelocity = GetContextVelocity(target.position);
        Vector2 steering = Vector2.ClampMagnitude(desiredVelocity - rb.linearVelocity, 1.5f);
        rb.AddForce(steering * acceleration, ForceMode2D.Force);

        Vector2 right = transform.right;
        float drift = Vector2.Dot(rb.linearVelocity, right);
        rb.AddForce(-right * drift * gripStrength, ForceMode2D.Force);

        if (rb.linearVelocity.magnitude > maxSpeed)
        {
            Vector2 cappedVelocity = rb.linearVelocity.normalized * maxSpeed;
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, cappedVelocity, Time.fixedDeltaTime * brakeStrength);
        }

        if (rb.linearVelocity.magnitude > 0.1f)
        {
            float angle = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg - 90f;
            Quaternion moveRotation = Quaternion.Euler(0f, 0f, angle);
            transform.rotation = Quaternion.Slerp(transform.rotation, moveRotation, rotationSpeed * Time.fixedDeltaTime);
        }
    }

    private void BuildRayDirections()
    {
        rayCount = Mathf.Max(1, rayCount);
        rayDirections = new Vector2[rayCount];

        for (int i = 0; i < rayCount; i++)
        {
            float angle = i * (Mathf.PI * 2f) / rayCount;
            rayDirections[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }
    }

    private Vector2 GetContextVelocity(Vector2 targetPosition)
    {
        if (rayDirections == null || rayDirections.Length != rayCount)
            BuildRayDirections();

        float[] interestMap = new float[rayCount];
        float[] dangerMap = new float[rayCount];
        Vector2 castOrigin = GetSensorCastOrigin();
        Vector2 targetDirection = (targetPosition - (Vector2)transform.position).normalized;

        if (flee)
            targetDirection *= -1f;

        for (int i = 0; i < rayCount; i++)
        {
            Vector2 direction = rayDirections[i];
            interestMap[i] = Mathf.Max(0f, Vector2.Dot(direction, targetDirection));

            Vector2 rayStart = castOrigin + direction * 0.1f;
            RaycastHit2D hit = Physics2D.CircleCast(rayStart, agentRadius, direction, detectionRange, wallLayer);
            if (hit.collider != null)
            {
                float actualDistance = hit.distance + 0.1f;
                float danger = 1f - Mathf.Clamp01(actualDistance / detectionRange);
                dangerMap[i] = danger * wallDangerWeight;
            }
        }

        int bestSlot = 0;
        float maxScore = -1f;
        EnsureInterestHistory();

        for (int i = 0; i < rayCount; i++)
        {
            float score = interestMap[i] - dangerMap[i];
            score = Mathf.Lerp(lastFrameInterests[i], score, 0.15f);
            lastFrameInterests[i] = score;

            if (score > maxScore)
            {
                maxScore = score;
                bestSlot = i;
            }
        }

        Vector2 finalHeading = rayDirections[bestSlot];
        int previousSlot = (bestSlot - 1 + rayCount) % rayCount;
        int nextSlot = (bestSlot + 1) % rayCount;

        if (dangerMap[previousSlot] < 0.5f)
            finalHeading += rayDirections[previousSlot] * Mathf.Max(0f, interestMap[previousSlot] - dangerMap[previousSlot]);

        if (dangerMap[nextSlot] < 0.5f)
            finalHeading += rayDirections[nextSlot] * Mathf.Max(0f, interestMap[nextSlot] - dangerMap[nextSlot]);

        finalHeading.Normalize();

        float distance = Vector2.Distance(transform.position, targetPosition);
        float arrival = flee ? 1f : Mathf.Pow(Mathf.Clamp01(distance / stopDistance), arrivalSharpness);
        float braking = Mathf.Clamp01(1f - dangerMap[bestSlot]);

        return finalHeading * maxSpeed * arrival * braking;
    }

    private void EnsureInterestHistory()
    {
        if (lastFrameInterests == null || lastFrameInterests.Length != rayCount)
            lastFrameInterests = new float[rayCount];
    }

    private Vector2 GetSensorCastOrigin()
    {
        Vector2 velocity = rb != null ? rb.linearVelocity : Vector2.zero;
        float lookAheadOffset = velocity.magnitude * Time.fixedDeltaTime;
        return (Vector2)transform.position + velocity.normalized * lookAheadOffset;
    }

    private void OnDrawGizmosSelected()
    {
        if (rb == null || rayDirections == null)
            return;

        Vector2 castOrigin = GetSensorCastOrigin();

        for (int i = 0; i < rayDirections.Length; i++)
        {
            Vector2 direction = rayDirections[i];
            Vector2 rayStart = castOrigin + direction * 0.1f;
            RaycastHit2D hit = Physics2D.CircleCast(rayStart, agentRadius, direction, detectionRange, wallLayer);

            if (hit.collider != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(rayStart + direction * hit.distance, agentRadius);
            }
            else
            {
                Gizmos.color = Color.green;
                Gizmos.DrawRay(rayStart, direction * detectionRange);
            }
        }
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    public Transform GetTarget()
    {
        return target;
    }

    public void SetFlee(bool value)
    {
        flee = value;
    }

    public bool IsFleeing()
    {
        return flee;
    }

    public float GetMaxSpeed()
    {
        return maxSpeed;
    }

    public LayerMask GetWallLayer()
    {
        return wallLayer;
    }
}
