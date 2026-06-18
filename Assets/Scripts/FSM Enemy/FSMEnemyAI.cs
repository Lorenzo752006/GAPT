using UnityEngine;

public class EnemyFSM : MonoBehaviour
{
    public enum SimpleState { Wander, Chase, Attack, Flee }
    public enum ComplexityMode { Basic, Complex }
    [Header("AI Settings")]
    public ComplexityMode aiMode = ComplexityMode.Complex;
    public SimpleState currentState = SimpleState.Wander;
    
    private bool isMoving = false;
    private Vector3 targetWorldPos;

    [Header("Stats")]
    [SerializeField] private float health = 100f;
    [SerializeField] private float fleeThreshold = 25f; 
    [SerializeField] private float chaseRange = 5f;
    [SerializeField] private float attackRange = 1.2f;

    private GridPlayerController player;

    void Start() {
        player = FindFirstObjectByType<GridPlayerController>();
        
        int spawnX = GridManager.Instance.Width - 2;
        int spawnY = GridManager.Instance.Height - 2;
    
        transform.position = GridManager.Instance.GridToWorld(spawnX, spawnY);
    }

    void Update()
    {
        float distance = Vector3.Distance(transform.position, player.transform.position);

        if (isMoving)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetWorldPos, Time.deltaTime * 2f);
            if (Vector3.Distance(transform.position, targetWorldPos) < 0.01f)
            {
                transform.position = targetWorldPos;
                isMoving = false;
            }
            return; 
        }

        if (health < fleeThreshold && distance < chaseRange) 
        {
            currentState = SimpleState.Flee;
            HandleGridMove(true); 
            Debug.Log($"Fleeing Started ({aiMode} Mode)");
        }
        else if (distance < attackRange) 
        {
            currentState = SimpleState.Attack;
            Debug.Log("Attacking Started");
        } 
        else if (distance < chaseRange) 
        {
            currentState = SimpleState.Chase;
            HandleGridMove(false); 
            Debug.Log($"Chasing Started ({aiMode} Mode)");
        } 
        else 
        {
            currentState = SimpleState.Wander;
            Debug.Log("Wandering Started");
        }
    }

    private void HandleGridMove(bool away)
    {
        Vector2Int currentGridPos = GridManager.Instance.WorldToGrid(transform.position);
        Vector2Int playerGridPos = GridManager.Instance.WorldToGrid(player.transform.position);
        Vector2Int moveDir = Vector2Int.zero;

        if (aiMode == ComplexityMode.Basic)
        {
            if (away)
            {
                if (currentGridPos.x <= playerGridPos.x) moveDir.x = -1;
                else moveDir.x = 1;
            }
            else
            {
                if (currentGridPos.x < playerGridPos.x) moveDir.x = 1;
                else if (currentGridPos.x > playerGridPos.x) moveDir.x = -1;
                else if (currentGridPos.y < playerGridPos.y) moveDir.y = 1;
                else if (currentGridPos.y > playerGridPos.y) moveDir.y = -1;
            }
        }
        else
        {
            if (away)
            {
                if (currentGridPos.x <= playerGridPos.x) moveDir.x = -1; 
                else moveDir.x = 1;                                      

                if (!GridManager.Instance.IsWalkable(currentGridPos.x + moveDir.x, currentGridPos.y))
                {
                    moveDir.x = 0;
                    if (currentGridPos.y <= playerGridPos.y) moveDir.y = -1;
                    else moveDir.y = 1;
                }
            }
            else
            {
                if (currentGridPos.x < playerGridPos.x) moveDir.x = 1;
                else if (currentGridPos.x > playerGridPos.x) moveDir.x = -1;
                else if (currentGridPos.y < playerGridPos.y) moveDir.y = 1;
                else if (currentGridPos.y > playerGridPos.y) moveDir.y = -1;
            }
        }

        Vector2Int targetGrid = currentGridPos + moveDir;

        if (GridManager.Instance.IsWalkable(targetGrid.x, targetGrid.y))
        {
            targetWorldPos = GridManager.Instance.GridToWorld(targetGrid.x, targetGrid.y);
            isMoving = true;
        }
    }
}