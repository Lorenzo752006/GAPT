using UnityEngine;
using UnityEngine.InputSystem;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

// Task 14 - Imitation Learning (GAIL), on the shared dungeon grid.
//
// This is the in-engine half of the task. The "basic vs complex" distinction lives
// in TRAINING (see Task14_GAIL.yaml):
//   BASIC   = behavioral cloning only (mimics the demo, can't recover from mistakes).
//   COMPLEX = GAIL reward signal + PPO exploration, so it learns recovery tactics.
//
// Both use this same agent. Record a human demo with a DemonstrationRecorder using
// the Heuristic() controls below, then train with mlagents-learn.
//
// Behavior Parameters (set in the Inspector to match this script):
//   Vector Observation > Space Size = 8
//   Actions > Discrete Branches = 1, Branch 0 Size = 5
//   Behavior Name = "ImitationAgent"
[RequireComponent(typeof(Unity.MLAgents.Policies.BehaviorParameters))]
public class ImitationAgentController : Unity.MLAgents.Agent
{
    [Header("Goal")]
    [Tooltip("Cell the agent is learning to reach. If empty, the 'Player'-tagged object is used.")]
    public Transform target;

    [Header("Episode")]
    [Tooltip("Randomize the agent's start cell each episode for generalization.")]
    public bool randomizeStart = true;

    private GridManager gm;

    public override void Initialize()
    {
        gm = GridManager.Instance;
        if (target == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) target = p.transform;
        }
    }

    public override void OnEpisodeBegin()
    {
        if (gm == null) gm = GridManager.Instance;
        if (gm == null) return;

        if (randomizeStart)
            transform.position = gm.GridToWorld(RandomFloorCell().x, RandomFloorCell().y);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        if (gm == null) { sensor.AddObservation(new float[8]); return; }

        Vector2Int me = gm.WorldToGrid(transform.position);
        Vector2Int goal = target != null ? gm.WorldToGrid(target.position) : me;

        // Normalized positions (4 floats).
        sensor.AddObservation((float)me.x / gm.Width);
        sensor.AddObservation((float)me.y / gm.Height);
        sensor.AddObservation((float)goal.x / gm.Width);
        sensor.AddObservation((float)goal.y / gm.Height);

        // Local walkability: up, down, left, right (4 floats) so it can avoid walls.
        sensor.AddObservation(gm.IsWalkable(me.x, me.y + 1) ? 1f : 0f);
        sensor.AddObservation(gm.IsWalkable(me.x, me.y - 1) ? 1f : 0f);
        sensor.AddObservation(gm.IsWalkable(me.x - 1, me.y) ? 1f : 0f);
        sensor.AddObservation(gm.IsWalkable(me.x + 1, me.y) ? 1f : 0f);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        if (gm == null) return;

        Vector2Int me = gm.WorldToGrid(transform.position);
        Vector2Int dir = ActionToDir(actions.DiscreteActions[0]);
        Vector2Int next = me + dir;

        if (dir != Vector2Int.zero && gm.IsWalkable(next.x, next.y))
            transform.position = gm.GridToWorld(next.x, next.y); // grid snap move
        else if (dir != Vector2Int.zero)
            AddReward(-0.01f); // tried to walk into a wall / out of bounds

        // Small time penalty encourages efficient routes.
        AddReward(-0.001f);

        // Reached the goal cell?
        if (target != null && gm.WorldToGrid(transform.position) == gm.WorldToGrid(target.position))
        {
            SetReward(1f);
            EndEpisode();
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var d = actionsOut.DiscreteActions;
        d[0] = 0;
        var kb = Keyboard.current;
        if (kb == null) return;
        if (kb.upArrowKey.isPressed) d[0] = 1;
        else if (kb.downArrowKey.isPressed) d[0] = 2;
        else if (kb.leftArrowKey.isPressed) d[0] = 3;
        else if (kb.rightArrowKey.isPressed) d[0] = 4;
    }

    private static Vector2Int ActionToDir(int a)
    {
        switch (a)
        {
            case 1: return Vector2Int.up;
            case 2: return Vector2Int.down;
            case 3: return Vector2Int.left;
            case 4: return Vector2Int.right;
            default: return Vector2Int.zero;
        }
    }

    private Vector2Int RandomFloorCell()
    {
        for (int i = 0; i < 100; i++)
        {
            int x = Random.Range(0, gm.Width);
            int y = Random.Range(0, gm.Height);
            if (gm.IsWalkable(x, y)) return new Vector2Int(x, y);
        }
        return new Vector2Int(1, 1);
    }
}
