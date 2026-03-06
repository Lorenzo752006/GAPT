using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
// Context-steering locomotion used in Task 6.
// It samples multiple directions around the enemy, scores them based on
// interest (toward the target) and danger (walls), then steers physically.
public class EnemyLocomotionTask6 : MonoBehaviour
{
    private Rigidbody2D rb;
    private Vector2[] rayDirections;
    private float[] lastFrameScores;

    [SerializeField] private Transform target;

    [Header("Movement")]
    [SerializeField] private float acceleration = 12f;
    [SerializeField] private float maxSpeed = 5f;
    [SerializeField] private float stopDistance = 2f;
    [SerializeField] private float brakeStrength = 5f;
    [SerializeField] private float maxSteeringForce = 4f;

    [Header("Physics Feel")]
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float gripStrength = 15f;
    [SerializeField] private float arrivalSharpness = 1.5f;

    [Header("AI Logic")]
    [SerializeField] private bool flee = false;

    [Header("Context Steering")]
    [SerializeField] private int rayCount = 24;
    [SerializeField] private float detectionRange = 4.5f;
    [SerializeField] private float wallDangerWeight = 2.0f;
    [SerializeField] private float agentRadius = 0.4f;
    [SerializeField] private float scoreSmoothing = 0.5f;
    [SerializeField] private LayerMask wallLayer;

    private float baseMaxSpeed;
    private float baseStopDistance;

    void Awake()
    {
        BuildRayDirections();
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        baseMaxSpeed = maxSpeed;
        baseStopDistance = stopDistance;
    }

    void FixedUpdate()
    {
        if (target == null)
        {
            // If no target is assigned, gradually brake to a stop.
            if (rb.linearVelocity.magnitude > 0.01f)
            {
                rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, Time.fixedDeltaTime * brakeStrength);
            }
            else
            {
                rb.linearVelocity = Vector2.zero;
            }
            return;
        }

        Vector2 desiredVelocity = GetContextVelocity(target.position);

        // Steering = desired velocity minus current velocity.
        Vector2 steering = desiredVelocity - rb.linearVelocity;
        steering = Vector2.ClampMagnitude(steering, maxSteeringForce);
        rb.AddForce(steering * acceleration, ForceMode2D.Force);

        // Remove sideways drift so movement feels tighter.
        Vector2 right = transform.right;
        float drift = Vector2.Dot(rb.linearVelocity, right);
        rb.AddForce(-right * drift * gripStrength, ForceMode2D.Force);

        if (rb.linearVelocity.magnitude > maxSpeed)
        {
            rb.linearVelocity = Vector2.Lerp(
                rb.linearVelocity,
                rb.linearVelocity.normalized * maxSpeed,
                Time.fixedDeltaTime * brakeStrength
            );
        }

        if (rb.linearVelocity.magnitude > 0.1f)
        {
            float angle = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg - 90f;
            Quaternion moveRotation = Quaternion.Euler(0f, 0f, angle);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                moveRotation,
                rotationSpeed * Time.fixedDeltaTime
            );
        }
    }

    private void BuildRayDirections()
    {
        if (rayCount < 3) rayCount = 3;

        rayDirections = new Vector2[rayCount];
        lastFrameScores = new float[rayCount];

        for (int i = 0; i < rayCount; i++)
        {
            float angle = i * (Mathf.PI * 2f) / rayCount;
            rayDirections[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            lastFrameScores[i] = 0f;
        }
    }

    private Vector2 GetContextVelocity(Vector2 targetPos)
    {
        if (rayDirections == null || rayDirections.Length != rayCount)
            BuildRayDirections();

        float[] dangerMap = new float[rayCount];

        Vector2 toTarget = targetPos - (Vector2)transform.position;
        float distance = toTarget.magnitude;

        if (distance < 0.0001f)
            return Vector2.zero;

        Vector2 targetDir = toTarget.normalized;
        if (flee) targetDir *= -1f;

        Vector2 velocityDir = rb.linearVelocity.sqrMagnitude > 0.0001f
            ? rb.linearVelocity.normalized
            : Vector2.zero;

        Vector2 finalHeading = Vector2.zero;
        float totalWeight = 0f;
        float bestDanger = 0f;

        for (int i = 0; i < rayCount; i++)
        {
            Vector2 dir = rayDirections[i];

            float interest = Mathf.Max(0f, Vector2.Dot(dir, targetDir));

            // CircleCast checks whether moving in this direction would hit a wall.
            Vector2 castOrigin = (Vector2)transform.position + dir * 0.1f;
            RaycastHit2D hit = Physics2D.CircleCast(castOrigin, agentRadius, dir, detectionRange, wallLayer);

            float danger = 0f;
            if (hit.collider != null)
            {
                float actualDist = hit.distance + 0.1f;
                danger = 1f - Mathf.Clamp01(actualDist / detectionRange);
                danger *= wallDangerWeight;
            }

            dangerMap[i] = danger;

            // Small bias toward continuing the current movement direction.
            float velocityBias = velocityDir.sqrMagnitude > 0.0001f
                ? Mathf.Max(0f, Vector2.Dot(dir, velocityDir)) * 0.15f
                : 0f;

            float rawScore = Mathf.Max(0f, interest + velocityBias - danger);
            float smoothedScore = Mathf.Lerp(lastFrameScores[i], rawScore, scoreSmoothing);

            lastFrameScores[i] = smoothedScore;

            if (smoothedScore > 0.001f)
            {
                finalHeading += dir * smoothedScore;
                totalWeight += smoothedScore;
            }
        }

        if (totalWeight > 0.0001f)
            finalHeading /= totalWeight;
        else
            finalHeading = targetDir;

        finalHeading = finalHeading.sqrMagnitude > 0.0001f
            ? finalHeading.normalized
            : targetDir;

        for (int i = 0; i < rayCount; i++)
        {
            float alignment = Vector2.Dot(rayDirections[i], finalHeading);
            if (alignment > 0.9f)
                bestDanger = Mathf.Max(bestDanger, dangerMap[i]);
        }

        // Arrival reduces speed near the target unless we are fleeing.
        float arrival = flee ? 1f : Mathf.Pow(Mathf.Clamp01(distance / stopDistance), arrivalSharpness);
        float braking = Mathf.Clamp01(1f - bestDanger);

        return finalHeading * maxSpeed * arrival * braking;
    }

    public void SetTarget(Transform t) => target = t;

    public void ClearTarget() => target = null;

    public void SetFlee(bool value) => flee = value;

    public void SetSpeedMultiplier(float multiplier)
    {
        maxSpeed = baseMaxSpeed * Mathf.Clamp(multiplier, 0.1f, 3f);
    }

    public void SetStopDistance(float distance)
    {
        stopDistance = Mathf.Max(0.05f, distance);
    }

    public void StopMovement()
    {
        if (rb == null) return;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
    }
}
