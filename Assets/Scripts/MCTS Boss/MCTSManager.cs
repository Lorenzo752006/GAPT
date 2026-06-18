using UnityEngine;

public class MCTSManager : MonoBehaviour
{
    public enum ComplexityMode { Basic, Complex }
    
    [Header("AI Mode Selection")]
    public ComplexityMode aiMode = ComplexityMode.Complex;

    [Header("Boss GameObjects")]
    [SerializeField] private GameObject basicBossObject;
    [SerializeField] private GameObject complexBossObject;

    private ComplexityMode lastMode;

    void Start()
    {
        lastMode = aiMode;
        ApplyModeSettings(true);
    }

    void Update()
    {
        if (aiMode != lastMode)
        {
            ApplyModeSettings(false);
            lastMode = aiMode;
        }
    }

    private void ApplyModeSettings(bool isStarting)
    {
        if (basicBossObject == null || complexBossObject == null)
        {
            Debug.LogError("MCTSManager: Please assign both Basic and Complex Boss GameObjects in the Inspector!");
            return;
        }

        if (aiMode == ComplexityMode.Basic)
        {
            if (!isStarting && complexBossObject.TryGetComponent<MCTSComplexAI>(out var complexScript))
            {
                Vector2Int currentGridPos = complexScript.GetGridPosition();
                Vector3 currentWorldPos = complexBossObject.transform.position;
                
                if (basicBossObject.TryGetComponent<BasicBoss>(out var basicScript))
                {
                    basicScript.SyncPosition(currentGridPos, currentWorldPos);
                }
            }

            basicBossObject.SetActive(true);
            complexBossObject.SetActive(false);
            Debug.Log("MCTS Mode Switched to: BASIC");
        }
        else
        {
            if (!isStarting && basicBossObject.TryGetComponent<BasicBoss>(out var basicScript))
            {
                Vector2Int currentGridPos = basicScript.GetGridPosition();
                Vector3 currentWorldPos = basicBossObject.transform.position;
                
                if (complexBossObject.TryGetComponent<MCTSComplexAI>(out var complexScript))
                {
                    complexScript.SyncPosition(currentGridPos, currentWorldPos);
                }
            }

            complexBossObject.SetActive(true);
            basicBossObject.SetActive(false);
            Debug.Log("MCTS Mode Switched to: COMPLEX");
        }
    }
}