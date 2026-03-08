using UnityEngine;

public class EnemyFuzzyController : MonoBehaviour
{
    [Header("Refs")]
    public Transform player;
    public EnemyLocomotion locomotion;
    public EnemyHealth enemyHealth; // NEW: read actual enemy HP

    [Header("Distance thresholds")]
    public float closeDist = 2f;
    public float farDist = 8f;

    [Header("Tuning")]
    public float minSpeedMultiplier = 0.6f;
    public float maxSpeedMultiplier = 1.2f;

    [Header("Overrides")]
    public float hardFleeHP = 30f;

    [Header("Debug")]
    public bool debugLog = false;

    void Reset()
    {
        locomotion = GetComponent<EnemyLocomotion>();
        enemyHealth = GetComponent<EnemyHealth>();
    }

    void Awake()
    {
        if (!locomotion) locomotion = GetComponent<EnemyLocomotion>();
        if (!enemyHealth) enemyHealth = GetComponent<EnemyHealth>();
    }

    void Update()
    {
        if (!player || !locomotion) return;

        locomotion.SetTarget(player);

        float d = Vector2.Distance(transform.position, player.position);

        // Read actual enemy HP (fallback to 100 if missing)
        float hp = enemyHealth ? enemyHealth.currentHealth : 100f;

        // STEP 1: FUZZIFY HEALTH
        float hLow = GradeDown(hp, 25f, 55f);
        float hMed = Triangle(hp, 30f, 55f, 80f);
        float hHigh = GradeUp(hp, 60f, 90f);

        // STEP 2: FUZZIFY DISTANCE
        float midDist = (closeDist + farDist) * 0.5f;
        float dClose = GradeDown(d, closeDist, midDist);
        float dMed = Triangle(d, closeDist, midDist, farDist);
        float dFar = GradeUp(d, midDist, farDist);

        // STEP 3: RULES (min = AND, max = OR)
        float aggrHigh =
            Mathf.Max(
                Mathf.Min(hHigh, dClose),
                Mathf.Min(hHigh, dMed),
                Mathf.Min(hMed, dClose)
            );

        float aggrMed =
            Mathf.Max(
                Mathf.Min(hMed, dMed),
                Mathf.Min(hHigh, dFar),
                Mathf.Min(hLow, dFar)
            );

        float aggrLow =
            Mathf.Max(
                Mathf.Min(hLow, dClose),
                Mathf.Min(hLow, dMed),
                Mathf.Min(hMed, dFar)
            );

        // STEP 4: DEFUZZIFY -> single aggression [0..1]
        float denom = aggrLow + aggrMed + aggrHigh;
        float aggression = denom <= 0.0001f
            ? 0.5f
            : (aggrLow * 0.2f + aggrMed * 0.5f + aggrHigh * 0.9f) / denom;

        // Optional: prove step 1+2 with logs
        if (debugLog)
        {
            Debug.Log(
                $"HP={hp:F1} (L={hLow:F2} M={hMed:F2} H={hHigh:F2}) | " +
                $"D={d:F2} (C={dClose:F2} M={dMed:F2} F={dFar:F2}) | " +
                $"Agg={aggression:F2}"
            );
        }

        // Behaviour mapping
        bool shouldFlee = hp < hardFleeHP || aggression < 0.35f;
        locomotion.SetFlee(shouldFlee);

        float speedMul = Mathf.Lerp(minSpeedMultiplier, maxSpeedMultiplier, aggression);
        locomotion.SetSpeedMultiplier(speedMul);

        locomotion.SetStopDistance(Mathf.Lerp(3.0f, 1.2f, aggression));
    }

    // Membership functions
    static float GradeUp(float x, float a, float b)
    {
        if (x <= a) return 0f;
        if (x >= b) return 1f;
        return (x - a) / (b - a);
    }

    static float GradeDown(float x, float a, float b)
    {
        if (x <= a) return 1f;
        if (x >= b) return 0f;
        return (b - x) / (b - a);
    }

    static float Triangle(float x, float a, float b, float c)
    {
        if (x <= a || x >= c) return 0f;
        if (Mathf.Abs(x - b) < 0.0001f) return 1f;
        if (x < b) return (x - a) / (b - a);
        return (c - x) / (c - b);
    }
}