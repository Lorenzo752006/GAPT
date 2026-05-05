using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class EnemyMLAgent : Agent
{
    [SerializeField] private Transform targetGoal;
    [SerializeField] private float moveSpeed = 5f;
    
    private Rigidbody2D rb; // Or Rigidbody if 3D
    private Vector2 startingPosition;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody2D>();
        startingPosition = transform.localPosition;
    }

    // STEP 1: Reset the environment at the start of each episode
    public override void OnEpisodeBegin()
    {
        // Reset enemy position
        transform.localPosition = startingPosition;
        
        // Optional: Randomize the goal's position here to force the AI to generalize
        // targetGoal.localPosition = new Vector3(Random.Range(-5f, 5f), Random.Range(-5f, 5f), 0);
    }

    // STEP 2: The Agent's "Eyes" (Sensory Data)
    public override void CollectObservations(VectorSensor sensor)
    {
        // We defined a Space Size of 4 in the Behavior Parameters.
        // We must add exactly 4 floats here.
        sensor.AddObservation(transform.localPosition.x);
        sensor.AddObservation(transform.localPosition.y);
        sensor.AddObservation(targetGoal.localPosition.x);
        sensor.AddObservation(targetGoal.localPosition.y);
        
        // Alternatively, you could just pass the distance and direction:
        // Vector2 directionToTarget = (targetGoal.localPosition - transform.localPosition).normalized;
        // sensor.AddObservation(directionToTarget.x);
        // sensor.AddObservation(directionToTarget.y);
    }

    // STEP 3: Actions and Rewards
    public override void OnActionReceived(ActionBuffers actions)
    {
        // Get the discrete action chosen by the network (0, 1, 2, or 3)
        int moveAction = actions.DiscreteActions[0];
        Vector2 movement = Vector2.zero;

        switch (moveAction)
        {
            case 0: movement = Vector2.up; break;
            case 1: movement = Vector2.down; break;
            case 2: movement = Vector2.left; break;
            case 3: movement = Vector2.right; break;
        }

        // Apply movement
        transform.localPosition += (Vector3)movement * moveSpeed * Time.deltaTime;

        // Reward System
        float distanceToTarget = Vector2.Distance(transform.localPosition, targetGoal.localPosition);

        // Tiny penalty for existing (encourages finding the goal faster)
        AddReward(-0.001f); 

        // Did we reach the goal?
        if (distanceToTarget < 1.0f)
        {
            SetReward(1.0f); // Massive reward
            EndEpisode();    // Stop the current run and start over
        }

        // Did we fall off the map or hit a wall? (Example logic)
        if (transform.localPosition.x > 20 || transform.localPosition.x < -20)
        {
            SetReward(-1.0f); // Massive penalty
            EndEpisode();
        }
    }

    // STEP 4: Manual Control for Testing
    // This allows YOU to play as the agent to ensure the actions map correctly
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<int> discreteActions = actionsOut.DiscreteActions;
        
        if (Input.GetKey(KeyCode.W)) discreteActions[0] = 0;
        else if (Input.GetKey(KeyCode.S)) discreteActions[0] = 1;
        else if (Input.GetKey(KeyCode.A)) discreteActions[0] = 2;
        else if (Input.GetKey(KeyCode.D)) discreteActions[0] = 3;
    }
}