/*
    Steps to activate MAS Agents
    1) Open Anaconda Prompt or Command Prompt
    2) Navigate to project folder
    3) Create the conda environment with 'conda create -n mlagents python=3.9 -y' //(First-time Only)//
    4) Activate agents with 'conda activate mlagents'
    5) Install all dependencies with 'pip install numpy==1.21.2 "protobuf<=3.20.3" "onnx<=1.16.2" torch torchvision torchaudio mlagents==0.30.0' //(First-time Only)// (Versions are 
       necessary for functionality)
    7) Run agents with 'mlagents-learn MASConfig.yaml --run-id=(idname)' (ID name can be changed as needed)
    8) Play Unity Project and observe
    9) To exit, halt the process from the Unity Editor, and then press CTRL & C in the prompt window
    10) Make sure to use different ID names when testing to prevent errors. Alternatively, if you wish to keep using the same ID name, add --resume to step 7's line' to continue with
        the current data, or add --force to overwrite all the data and start over with the same ID name.
*/

using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine.InputSystem;

public class MASAgentAI : Unity.MLAgents.Agent
{
    // --- MODE SWITCHER ---
    public enum ComplexityMode { Basic, Complex }
    [Header("Agent Settings")]
    public ComplexityMode agentMode = ComplexityMode.Complex;

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
        // Always observe the current local position (3 floats: X, Y, Z)
        sensor.AddObservation(transform.localPosition);
    
        // Always observe the opponent's local position (3 floats: X, Y, Z)
        if (opponent != null)
        {
            sensor.AddObservation(opponent.transform.localPosition);
        }
        else
        {
            // Padding fallback to maintain exactly 6 floats if the opponent is missing
            sensor.AddObservation(Vector3.zero);
        }
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
        float distanceBeforeMove = Vector3.Distance(transform.localPosition, opponent.localPosition);

        if (gridManager.IsWalkable(targetGrid.x, targetGrid.y))
        {
            gridPosition = targetGrid;
            transform.localPosition = gridManager.GridToWorld(gridPosition.x, gridPosition.y);
            
            // --- BASIC VS COMPLEX REWARD MODIFIERS ---
            if (agentMode == ComplexityMode.Complex)
            {
                float distanceAfterMove = Vector3.Distance(transform.localPosition, opponent.localPosition);
                // Complex mode rewards getting actively closer to the enemy, not just moving randomly
                if (distanceAfterMove < distanceBeforeMove) {
                    AddReward(0.05f); 
                } else {
                    AddReward(-0.02f); // Penalize moving away
                }
            }
            else
            {
                AddReward(0.01f); // Basic mode: just encourage valid movement
            }
        }
        else
        {
            // Penalize wall hits harder in complex mode to force tight navigation
            AddReward(agentMode == ComplexityMode.Complex ? -0.05f : -0.01f);
        }

        // Win Condition
        if (gridPosition == new Vector2Int((int)opponent.localPosition.x, (int)opponent.localPosition.y))
        {
            Debug.Log("Win Confirmed");
            AddReward(1.0f); 
            EndEpisode();    
        }

        // Standstill Prevention
        AddReward(-0.001f);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;
        if (Keyboard.current.wKey.isPressed) discreteActionsOut[0] = 1;
        else if (Keyboard.current.sKey.isPressed) discreteActionsOut[0] = 2;
        else if (Keyboard.current.aKey.isPressed) discreteActionsOut[0] = 3;
        else if (Keyboard.current.dKey.isPressed) discreteActionsOut[0] = 4;
    }
}