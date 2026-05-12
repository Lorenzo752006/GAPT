using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using GridManagerTask9 = Task9.GridManagerTask9;

public class EnemyMLAgent : Agent
{
    [Header("References")]
    [SerializeField] private Transform targetGoal; 
    
    private Task12EnemyLocomotion locomotion;
    private Rigidbody2D rb;
    private Task9PatternGen mapGenerator;
    
    private float lastDistance; 
    private const float MAP_SIZE = 75f; // 75x75 map 

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody2D>();
        locomotion = GetComponent<Task12EnemyLocomotion>();
        mapGenerator = FindAnyObjectByType<Task9PatternGen>();
        
        rb.gravityScale = 0f;
    }

    public override void OnEpisodeBegin()
    {
        if (mapGenerator != null)
        {
            // 1. Reset/Load the map and sync the GridManager
            mapGenerator.GenerateNewRandomMap(); 

            // 2. Find the Player and calculate a valid spawn
            Task9PlayerController player = FindAnyObjectByType<Task9PlayerController>();
            Vector2 playerSpawnPos = mapGenerator.GetRandomFloorPosition();
            
            if (player != null)
            {
                // Convert world spawn to grid coordinates for the SetPosition method
                Vector2Int playerGridPos = GridManagerTask9.Instance.WorldToGrid(playerSpawnPos);
                
                // USE SetPosition to prevent snapping to 0,0
                player.SetPosition(playerGridPos);
            }

            // 3. Randomly spawn Enemy (this Agent) on a different floor tile
            Vector2 enemySpawnPos = mapGenerator.GetRandomFloorPosition();
            
            int safetyCounter = 0;
            // Ensure distance from player so the agent has to move
            while (Vector2.Distance(playerSpawnPos, enemySpawnPos) < 5f && safetyCounter < 10)
            {
                enemySpawnPos = mapGenerator.GetRandomFloorPosition();
                safetyCounter++;
            }
            
            // Snap the agent to the new position
            transform.position = enemySpawnPos;
        }

        // 4. Physics Reset
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        // 5. Reset distance tracking for reward calculations
        lastDistance = Vector2.Distance(transform.position, targetGoal.position);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Normalized Direction to Player (2 obs)
        Vector2 relativePos = (targetGoal.position - transform.position);
        sensor.AddObservation(relativePos.normalized);

        // Normalized Distance (1 obs)
        // Max diagonal of 75x75 is ~106. Dividing by 100 keeps values roughly in -1 to 1 range.
        sensor.AddObservation(relativePos.magnitude / 100f);

        // Agent's own Normalized Velocity (2 obs)
        sensor.AddObservation(rb.linearVelocity / 5f);

        // Relative Direction Alignment (1 obs)
        float alignment = 0f;
        if (rb.linearVelocity.magnitude > 0.05f)
            alignment = Vector2.Dot(rb.linearVelocity.normalized, relativePos.normalized);
        sensor.AddObservation(alignment);

        // Normalize current position relative to the map size
        sensor.AddObservation(transform.position.x / MAP_SIZE);
        sensor.AddObservation(transform.position.y / MAP_SIZE);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        int moveAction = actions.DiscreteActions[0];
        Vector2 direction = Vector2.zero;

        // EXECUTE MOVEMENT
        switch (moveAction)
        {
            case 0: direction = Vector2.up; break;
            case 1: direction = Vector2.down; break;
            case 2: direction = Vector2.left; break;
            case 3: direction = Vector2.right; break;
            case 4: locomotion.Stop(); break; 
        }

        if (moveAction != 4)
        {
            Vector2 steeringTarget = (Vector2)transform.position + direction;
            locomotion.SetSteeringTarget(steeringTarget);
        }

        // DISTANCE CALCULATIONS
        float currentDistance = Vector2.Distance(transform.position, targetGoal.position);

        // ONLY reward if a new "Personal Best" is achieved
        if (currentDistance < lastDistance)
        {
            float improvement = lastDistance - currentDistance;
            AddReward(improvement * 0.5f);  
            lastDistance = currentDistance;
        }
        if (moveAction != 4 && rb.linearVelocity.magnitude < 0.1f)
            AddReward(-0.02f);  
        AddReward(-0.005f);    

        // Goal
        if (currentDistance < 1.2f) 
        {
            AddReward(10.0f); 
            EndEpisode();
        }

        if (StepCount >= MaxStep - 1 && MaxStep > 0)
        {
            AddReward(-1.0f);
            // episode ends automatically at MaxStep
        }

    }


    private void OnCollisionEnter2D(Collision2D collision)
    {

        if (collision.gameObject.CompareTag("Wall"))
        {
            AddReward(-0.05f); 
        }
    }


    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActions = actionsOut.DiscreteActions;
        discreteActions[0] = 4; // Default to Stop

        if (Input.GetKey(KeyCode.W)) discreteActions[0] = 0;
        else if (Input.GetKey(KeyCode.S)) discreteActions[0] = 1;
        else if (Input.GetKey(KeyCode.A)) discreteActions[0] = 2;
        else if (Input.GetKey(KeyCode.D)) discreteActions[0] = 3;
    }
}