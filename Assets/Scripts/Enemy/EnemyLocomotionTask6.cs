using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyLocomotionTask6 : MonoBehaviour
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

    private float baseMaxSpeed;
    private float baseStopDistance;

    void Awake()
    {
        rayDirections = new Vector2[rayCount];
        for (int i = 0; i < rayCount; i++)
        {
            float angle = i * (Mathf.PI * 2) / rayCount;
            rayDirections[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }
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
        if (target == null) return;

        Vector2 desiredVelocity = GetContextVelocity(target.position);

        Vector2 steering = desiredVelocity - rb.linearVelocity;
        steering = Vector2.ClampMagnitude(steering, 1.5f);
        rb.AddForce(steering * acceleration, ForceMode2D.Force);

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
            Quaternion moveRotation = Quaternion.Euler(0, 0, angle);
            transform.rotation = Quaternion.Slerp(transform.rotation, moveRotation, rotationSpeed * Time.fixedDeltaTime);
        }
    }

    Vector2 GetContextVelocity(Vector2 targetPos)
    {
        float[] interestMap = new float[rayCount];
        float[] dangerMap = new float[rayCount];

        float lookAheadOffset = rb.linearVelocity.magnitude * Time.fixedDeltaTime;
        Vector2 castOrigin = (Vector2)transform.position + (rb.linearVelocity.sqrMagnitude > 0.0001f
            ? (rb.linearVelocity.normalized * lookAheadOffset)
            : Vector2.zero);

        Vector2 targetDir = (targetPos - (Vector2)transform.position).normalized;
        if (flee) targetDir *= -1f;

        for (int i = 0; i < rayCount; i++)
        {
            Vector2 dir = rayDirections[i];

            interestMap[i] = Mathf.Max(0, Vector2.Dot(dir, targetDir));

            Vector2 rayStart = castOrigin + (dir * 0.1f);
            RaycastHit2D hit = Physics2D.CircleCast(rayStart, agentRadius, dir, detectionRange, wallLayer);

            if (hit.collider != null)
            {
                float actualDist = hit.distance + 0.1f;
                float danger = 1f - Mathf.Clamp01(actualDist / detectionRange);
                dangerMap[i] = danger * wallDangerWeight;
            }
        }

        int bestSlot = 0;
        float maxScore = -999f;

        if (lastFrameInterests == null || lastFrameInterests.Length != rayCount)
            lastFrameInterests = new float[rayCount];

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

        int prev = (bestSlot - 1 + rayCount) % rayCount;
        int next = (bestSlot + 1) % rayCount;
        if (dangerMap[prev] < 0.5f) finalHeading += rayDirections[prev] * Mathf.Max(0, interestMap[prev] - dangerMap[prev]);
        if (dangerMap[next] < 0.5f) finalHeading += rayDirections[next] * Mathf.Max(0, interestMap[next] - dangerMap[next]);

        finalHeading = finalHeading.sqrMagnitude > 0.0001f ? finalHeading.normalized : Vector2.zero;

        float distance = Vector2.Distance(transform.position, targetPos);
        float arrival = flee ? 1f : Mathf.Pow(Mathf.Clamp01(distance / stopDistance), arrivalSharpness);
        float braking = Mathf.Clamp01(1f - dangerMap[bestSlot]);

        return finalHeading * maxSpeed * arrival * braking;
    }

    public void SetTarget(Transform t) => target = t;
    public void SetFlee(bool value) => flee = value;

    public void SetSpeedMultiplier(float multiplier)
    {
        maxSpeed = baseMaxSpeed * Mathf.Clamp(multiplier, 0.1f, 3f);
    }

    public void SetStopDistance(float distance)
    {
        stopDistance = Mathf.Max(0.05f, distance);
    }
}