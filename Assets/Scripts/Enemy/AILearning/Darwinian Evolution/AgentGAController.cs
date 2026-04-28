using UnityEngine;

[RequireComponent(typeof(EnemyHealth))]
public class AgentGAController : MonoBehaviour
{
    // Toggle between Basic and Complex
    public bool useComplexAI = true;
    public DNA dna;

    // Genetic Traits
    private float topSpeed;      // Gene 0
    private float turnSpeed;     // Gene 1
    private float acceleration;  // Gene 2
    private float focusLevel;    // Gene 3 (Irrationality)

    private float fitness;
    private Transform player;
    private SpriteRenderer sr;
    private EnemyHealth health;
    private Vector2 currentVelocity;

    // Irrationality State
    private Vector2 randomDirection;
    private float irrationalTimer;
    private bool isCurrentlyIrrational;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        health = GetComponent<EnemyHealth>();

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;

        // Initialize DNA with 4 genes
        if (dna == null || dna.genes == null || dna.genes.Length < 4)
            dna = new DNA(4);

        MapGenes();
        randomDirection = Random.insideUnitCircle.normalized;
    }

    void MapGenes()
    {
        topSpeed = Mathf.Lerp(1f, 4.75f, (dna.genes[0] + 1f) / 2f);
        turnSpeed = Mathf.Lerp(30f, 300f, (dna.genes[1] + 1f) / 2f);
        acceleration = Mathf.Lerp(0.5f, 5f, (dna.genes[2] + 1f) / 2f);
        focusLevel = Mathf.Lerp(0.5f, 1f, (dna.genes[3] + 1f) / 2f);
    }

    void Update()
    {
        if (player == null) return;

        if (useComplexAI)
            ComplexMovement();
        else
            BasicMovement();

        CalculateFitness();
        VisualizeFitness();
    }

    // --- MOVEMENT METHODS ---


    void ComplexMovement()
    {
        bool isLowHealth = (health.CurrentHealth < 30f);
        Vector2 targetDir;

        // Tick down the timer
        irrationalTimer -= Time.deltaTime;

        // Only make a new decision when the timer runs out
        if (irrationalTimer <= 0)
        {
            irrationalTimer = Random.Range(1f, 3f); // Decision lasts 1-3 seconds

            isCurrentlyIrrational = (Random.value > focusLevel);

            if (isCurrentlyIrrational)
                randomDirection = Random.insideUnitCircle.normalized;
        }

        // --- EXECUTE DECISION ---
        if (isCurrentlyIrrational && !isLowHealth)
        {
            targetDir = randomDirection;
        }
        else
        {
            targetDir = ((Vector2)player.position - (Vector2)transform.position).normalized;
            if (isLowHealth) targetDir *= -1;
        }

        float angle = Mathf.Atan2(targetDir.y, targetDir.x) * Mathf.Rad2Deg - 90f;
        Quaternion targetRotation = Quaternion.AngleAxis(angle, Vector3.forward);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime * 0.1f);

        Vector2 desiredVelocity = transform.up * topSpeed;
        currentVelocity = Vector2.Lerp(currentVelocity, desiredVelocity, acceleration * Time.deltaTime);
        transform.position += (Vector3)currentVelocity * Time.deltaTime;
    }

    void BasicMovement()
    {
        transform.Translate(Vector3.up * 2f * Time.deltaTime);
        transform.Rotate(Vector3.forward * Random.Range(-150f, 150f) * Time.deltaTime);
    }

    // --- FITNESS & EVALUATION ---

    public void CalculateFitness()
    {
        float dist = Vector2.Distance(transform.position, player.position);
        bool isLowHealth = (health.CurrentHealth < 30f);

        if (isLowHealth)
            fitness = Mathf.Clamp01(dist / 10f); // Reward fleeing when hurt
        else
            fitness = (1f / (dist + 0.5f)) * 1.5f; // Reward chasing when healthy
    }

    void VisualizeFitness()
    {
        if (sr == null) return;
        // Red = Bad Fitness, Green = Good Fitness
        sr.color = Color.Lerp(Color.red, Color.green, fitness);
    }

    public float GetFitness()
    {
        return fitness;
    }
}