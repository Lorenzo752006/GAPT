using UnityEngine;
using TMPro;

public class AIModeSwitcher : MonoBehaviour
{
    [Header("Task 4 References")]
    private EnemyFSM task4FSM;

    [Header("Task 13 References")]
    private MASAgentAI task13MAS;

    [Header("Task 16 References")]
    private MCTSManager task16MCTS;

    // Optional: Reference text components to display current status on your UI
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
        if (task13MAS == null) task13MAS = FindFirstObjectByType<MASAgentAI>();
        if (task16MCTS == null) task16MCTS = FindFirstObjectByType<MCTSManager>();
    }

    // Button Hook for Task 4 FSM
    public void ToggleTask4FSM()
    {
        if (task4FSM == null) FindSceneReferences();
        if (task4FSM == null) return;

        // Toggle the enum
        task4FSM.aiMode = (task4FSM.aiMode == ComplexityMode.Complex) 
            ? ComplexityMode.Basic 
            : ComplexityMode.Complex;

        Debug.Log($"UI Action: Task 4 FSM flipped to {task4FSM.aiMode}");
        UpdateVisuals();
    }

    // Button Hook for Task 13 Multi-Agent System
    public void ToggleTask13MAS()
    {
        if (task13MAS == null) FindSceneReferences();
        if (task13MAS == null) return;

        // Toggle using your MAS script's complexity enum name configuration
        // (Assuming your MAS uses the same ComplexityMode structure)
        task13MAS.agentMode = (task13MAS.agentMode == MASAgentAI.ComplexityMode.Complex) 
            ? MASAgentAI.ComplexityMode.Basic 
            : MASAgentAI.ComplexityMode.Complex;

        Debug.Log($"UI Action: Task 13 MAS flipped to {task13MAS.agentMode}");
        UpdateVisuals();
    }

    // Button Hook for Task 16 MCTS
    public void ToggleTask16MCTS()
    {
        if (task16MCTS == null) FindSceneReferences();
        if (task16MCTS == null) return;

        // Toggle the target mode enum
        task16MCTS.aiMode = (task16MCTS.aiMode == MCTSManager.ComplexityMode.Complex) 
            ? MCTSManager.ComplexityMode.Basic 
            : MCTSManager.ComplexityMode.Complex;

        Debug.Log($"UI Action: Task 16 MCTS flipped to {task16MCTS.aiMode}");
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        // Update UI Text markers if assigned to show active states inside the build
        if (task4StatusText != null && task4FSM != null) 
            task4StatusText.text = $"FSM: {task4FSM.aiMode}";
        
        if (task13StatusText != null && task13MAS != null) 
            task13StatusText.text = $"MAS: {task13MAS.agentMode}";
            
        if (task16StatusText != null && task16MCTS != null) 
            task16StatusText.text = $"MCTS: {task16MCTS.aiMode}";
    }
}