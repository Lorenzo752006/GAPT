using UnityEngine;
using TMPro;

public class AIModeSwitcher : MonoBehaviour
{
    [Header("Task 4 References")]
    private EnemyFSM task4FSM;

    [Header("Task 13 References (All Agents)")]
    private MASAgentAI[] task13MASAgents;
    private MASAgentAI.ComplexityMode currentTask13Mode = MASAgentAI.ComplexityMode.Basic;

    [Header("Task 16 References")]
    private MCTSManager task16MCTS;

    [Header("Optional Status Text UI")]
    [SerializeField] private TMP_Text task4StatusText;
    [SerializeField] private TMP_Text task13StatusText;
    [SerializeField] private TMP_Text task16StatusText;

    private void Start()
    {
        // Dynamically find the handlers in the scene at startup
        FindSceneReferences();
        UpdateVisuals();
    }

    private void FindSceneReferences()
    {
        if (task4FSM == null) task4FSM = FindFirstObjectByType<EnemyFSM>();
        if (task16MCTS == null) task16MCTS = FindFirstObjectByType<MCTSManager>();
        
        // Grab all active agents in the hierarchy
        task13MASAgents = FindObjectsByType<MASAgentAI>(FindObjectsSortMode.None);
    }

    // Button Hook for Task 4 FSM
    public void ToggleTask4FSM()
    {
        if (task4FSM == null) FindSceneReferences();
        if (task4FSM == null) return;

        task4FSM.aiMode = (task4FSM.aiMode == ComplexityMode.Complex) 
            ? ComplexityMode.Basic 
            : ComplexityMode.Complex;

        Debug.Log($"UI Action: Task 4 FSM flipped to {task4FSM.aiMode}");
        UpdateVisuals();
    }

    // Button Hook for Task 13 Multi-Agent System (Bulletproof Version)
    public void ToggleTask13MAS()
    {
        FindSceneReferences();

        // 1. Flip our localized tracking state variable first
        currentTask13Mode = (currentTask13Mode == MASAgentAI.ComplexityMode.Complex) 
            ? MASAgentAI.ComplexityMode.Basic 
            : MASAgentAI.ComplexityMode.Complex;

        // 2. Force update the UI text immediately so it never locks up
        if (task13StatusText != null) 
        {
            task13StatusText.text = $"MAS: {currentTask13Mode}";
        }

        // 3. Safely broadcast the state change down to every individual agent found
        if (task13MASAgents != null && task13MASAgents.Length > 0)
        {
            foreach (MASAgentAI agent in task13MASAgents)
            {
                if (agent != null)
                {
                    agent.agentMode = currentTask13Mode;
                }
            }
            Debug.Log($"UI Action: Flipped {task13MASAgents.Length} Task 13 Agents to {currentTask13Mode}");
        }
        else
        {
            Debug.LogWarning("UI Updated, but no active Task 13 Agents were found in the scene to switch.");
        }
    }

    // Button Hook for Task 16 MCTS
    public void ToggleTask16MCTS()
    {
        if (task16MCTS == null) FindSceneReferences();
        if (task16MCTS == null) return;

        task16MCTS.aiMode = (task16MCTS.aiMode == MCTSManager.ComplexityMode.Complex) 
            ? MCTSManager.ComplexityMode.Basic 
            : MCTSManager.ComplexityMode.Complex;

        Debug.Log($"UI Action: Task 16 MCTS flipped to {task16MCTS.aiMode}");
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        if (task4StatusText != null && task4FSM != null) 
            task4StatusText.text = $"FSM: {task4FSM.aiMode}";
        
        // Read directly from our local state tracker variable instead of inspecting an index
        if (task13StatusText != null) 
            task13StatusText.text = $"MAS: {currentTask13Mode}";
            
        if (task16StatusText != null && task16MCTS != null) 
            task16StatusText.text = $"MCTS: {task16MCTS.aiMode}";
    }
}