/*
    Steps to activate MAS Agents
    1) Open Anaconda Prompt or Command Prompt
    2) Navigate to project folder
    3) Activate agents with 'conda activate mlagents'
    4) Run agents with 'mlagents-learn DungeonConfig.yaml --run-id=MAS_FinalTest' (ID name can be changed as needed)
    5) Play Unity Project and observe
*/

using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine.InputSystem;

public class MASAgentAI : Agent
{
    [SerializeField] private GridManager gridManager;
    [SerializeField] private Transform opponent;
    private Vector2Int gridPosition;
    
    public override void OnEpisodeBegin()
    {
        if (gridManager == null) return;
        if (this.GetComponent<Unity.MLAgents.Policies.BehaviorParameters>().TeamId == 0){
            gridPosition = new Vector2Int(1, 1);
        }
        else{
            gridPosition = new Vector2Int(gridManager.Width - 2, gridManager.Height - 2);
        }
        transform.localPosition = gridManager.GridToWorld(gridPosition.x, gridPosition.y);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.localPosition);
        sensor.AddObservation(opponent.localPosition);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        int moveDir = actions.DiscreteActions[0];
        Vector2Int direction = Vector2Int.zero;

        // 0: Idle, 1: Up, 2: Down, 3: Left, 4: Right
        switch (moveDir)
        {
            case 1: direction = Vector2Int.up; break;
            case 2: direction = Vector2Int.down; break;
            case 3: direction = Vector2Int.left; break;
            case 4: direction = Vector2Int.right; break;
        }

        Vector2Int targetGrid = gridPosition + direction;

        if (gridManager.IsWalkable(targetGrid.x, targetGrid.y))
        {
            gridPosition = targetGrid;
            transform.localPosition = gridManager.GridToWorld(gridPosition.x, gridPosition.y);
            AddReward(0.01f); // Encourage valid movement
        }
        else
        {
            AddReward(-0.01f); // Discourage wall collisions
        }

        // Win Condition
        if (gridPosition == new Vector2Int((int)opponent.localPosition.x, (int)opponent.localPosition.y))
        {
            Debug.Log("Win Confirmed");
            AddReward(1.0f); // Opponent found
            EndEpisode();    // Reset the round
        }

        // Standstill Prevention
        AddReward(-0.001f);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;
        if (Keyboard.current.wKey.isPressed) {
            discreteActionsOut[0] = 1;
        }
        else if (Keyboard.current.sKey.isPressed) {
            discreteActionsOut[0] = 2;
        }
        else if (Keyboard.current.aKey.isPressed) {
            discreteActionsOut[0] = 3;
        }
        else if (Keyboard.current.dKey.isPressed) {
            discreteActionsOut[0] = 4;
        }
    }
}