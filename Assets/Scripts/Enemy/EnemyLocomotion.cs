using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyLocomotion : MonoBehaviour
{
    private Rigidbody2D rb;
    private Vector2[] rayDirections;
    private float[] lastFrameInterests;

    [SerializeField] private Transform target;

    [Header("Movement")]
    [SerializeField] private float acceleration = 12f; // Increased for snappier response
    [SerializeField] private float maxSpeed = 5f;
    [SerializeField] private float stopDistance = 2f;
    [SerializeField] private float brakeStrength = 5f;

    [Header("Physics Feel")]
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float gripStrength = 15f; // Increased to stop sliding into walls
    [SerializeField] private float arrivalSharpness = 1.5f;

    [Header("AI Logic")]
    [SerializeField] private bool flee = false;

    [Header("Context Steering")]
    [SerializeField] private int rayCount = 12;
    [SerializeField] private float detectionRange = 4.5f; // Increased for better look-ahead
    [SerializeField] private float wallDangerWeight = 2.0f;
    [SerializeField] private float agentRadius = 0.4f; // Matches actual character size
    [SerializeField] private LayerMask wallLayer;

    void Awake()
    {
        // Pre-calculate ray directions for performance
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
        // Optimization: Set collision detection to continuous if moving fast
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    void FixedUpdate()
    {
        if (target == null) return;

        Vector2 desiredVelocity = GetContextVelocity(target.position);

        // 1. Steering Force
        Vector2 steering = desiredVelocity - rb.linearVelocity;
        steering = Vector2.ClampMagnitude(steering, 1.5f); // Slightly more authority
        rb.AddForce(steering * acceleration, ForceMode2D.Force);

        // 2. High-Grip Traction (Anti-Drift)
        // Eliminates sideways sliding that causes wall-bumping
        Vector2 right = transform.right;
        float drift = Vector2.Dot(rb.linearVelocity, right);
        rb.AddForce(-right * drift * gripStrength, ForceMode2D.Force);

        // 3. Speed Cap
        if (rb.linearVelocity.magnitude > maxSpeed)
        {
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, rb.linearVelocity.normalized * maxSpeed, Time.fixedDeltaTime * brakeStrength);
        }

        // 4. Visual Rotation
        if (rb.linearVelocity.magnitude > 0.1f)
        {
            float angle = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg - 90f;
            Quaternion moveRotation = Quaternion.Euler(0, 0, angle);
            transform.rotation = Quaternion.Slerp(transform.rotation, moveRotation, rotationSpeed * Time.fixedDeltaTime);
        }
    }



    Vector2 GetContextVelocity(Vector2 target)
    {
        float[] interestMap = new float[rayCount];
        float[] dangerMap = new float[rayCount];

        // 1. PREDICTIVE ORIGIN (Used for both Interest AND Danger)
        // This is the most important fix. We must calculate interest from where 
        // the AI expects to be, or the sensors won't align with the desire.
        float lookAheadOffset = rb.linearVelocity.magnitude * Time.fixedDeltaTime;
        Vector2 castOrigin = (Vector2)transform.position + (rb.linearVelocity.normalized * lookAheadOffset);

        Vector2 targetDir = ((Vector2)target - (Vector2)transform.position).normalized;
        if (flee) targetDir *= -1f;

        // --- STEP 1: POPULATE MAPS ---
        for (int i = 0; i < rayCount; i++)
        {
            Vector2 dir = rayDirections[i];

            // Interest (Calculated from the predicted future point)
            interestMap[i] = Mathf.Max(0, Vector2.Dot(dir, targetDir));

            // Danger sensing
            Vector2 rayStart = castOrigin + (dir * 0.1f);
            RaycastHit2D hit = Physics2D.CircleCast(rayStart, agentRadius, dir, detectionRange, wallLayer);

            if (hit.collider != null)
            {
                float actualDist = hit.distance + 0.1f;
                float danger = 1f - Mathf.Clamp01(actualDist / detectionRange);

                // Apply weights directly
                dangerMap[i] = danger * wallDangerWeight;
            }
        }

        // --- STEP 2: DANGER BLURRING ---
        // (Keep your existing blurring logic here...)

        // --- STEP 3: WEIGHTED PROCESSING (The "Ignoring" Fix) ---
        int bestSlot = 0;
        float maxScore = -1f;

        for (int i = 0; i < rayCount; i++)
        {
            // Instead of a hard "If danger > 0.4, interest = 0", 
            // we use a Weighted Subtraction. 
            // This makes the AI "feel" the wall and steer away BEFORE interest hits zero.
            float score = interestMap[i] - dangerMap[i];

            // Temporal Smoothing (Hysteresis) to prevent jitter
            if (lastFrameInterests == null || lastFrameInterests.Length != rayCount)
                lastFrameInterests = new float[rayCount];

            score = Mathf.Lerp(lastFrameInterests[i], score, 0.15f);
            lastFrameInterests[i] = score;

            if (score > maxScore)
            {
                maxScore = score;
                bestSlot = i;
            }
        }

        // --- STEP 4: RESULT ---
        Vector2 finalHeading = rayDirections[bestSlot];

        // Sub-slot blending (Andrew Fray logic)
        // Only blend if the neighboring slots aren't dangerous
        int prev = (bestSlot - 1 + rayCount) % rayCount;
        int next = (bestSlot + 1) % rayCount;
        if (dangerMap[prev] < 0.5f) finalHeading += rayDirections[prev] * Mathf.Max(0, interestMap[prev] - dangerMap[prev]);
        if (dangerMap[next] < 0.5f) finalHeading += rayDirections[next] * Mathf.Max(0, interestMap[next] - dangerMap[next]);
        finalHeading.Normalize();

        // Final calculations (Arrival + Braking)
        float distance = Vector2.Distance(transform.position, target);
        float arrival = flee ? 1f : Mathf.Pow(Mathf.Clamp01(distance / stopDistance), arrivalSharpness);
        float braking = Mathf.Clamp01(1f - dangerMap[bestSlot]);

        return finalHeading * maxSpeed * arrival * braking;
    }



    private void OnDrawGizmosSelected()
    {
        if (rb == null || rayDirections == null) return;

        float lookAheadOffset = rb.linearVelocity.magnitude * Time.fixedDeltaTime;
        Vector2 castOrigin = (Vector2)transform.position + (rb.linearVelocity.normalized * lookAheadOffset);

        for (int i = 0; i < rayCount; i++)
        {
            Vector2 dir = rayDirections[i];
            Vector2 rayStart = castOrigin + (dir * 0.1f);
            RaycastHit2D hit = Physics2D.CircleCast(rayStart, agentRadius, dir, detectionRange, wallLayer);

            if (hit.collider != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(rayStart + dir * hit.distance, agentRadius);
            }
            else
            {
                Gizmos.color = Color.green;
                Gizmos.DrawRay(rayStart, dir * detectionRange);
            }
        }
    }
}