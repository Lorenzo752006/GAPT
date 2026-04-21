using UnityEngine;

public class BasicEnemyFSM : MonoBehaviour
{
    public enum SimpleState { Wander, Chase, Attack, Flee }
    public SimpleState currentState = SimpleState.Wander;
    
    private bool isMoving = false;
    private Vector3 targetWorldPos;

    [Header("Stats")]
    [SerializeField] private float health = 100f;
    [SerializeField] private float fleeThreshold = 25f; // Flee if health < 25
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
            HandleGridMove(true); // Move Away
            Debug.Log("Fleeing Started");
        }
        else if (distance < attackRange) 
        {
            currentState = SimpleState.Attack;
            Debug.Log("Attacking Started");
        } 
        else if (distance < chaseRange) 
        {
            currentState = SimpleState.Chase;
            HandleGridMove(false); // Move Toward
            Debug.Log("Chasing Started");
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

        if (away)
        {
            // --- FLEE LOGIC ---
            if (currentGridPos.x <= playerGridPos.x) moveDir.x = -1; // Player is right, move left
            else moveDir.x = 1;                                      // Player is left, move right

            // If move hits a wall, try Y axis
            if (!GridManager.Instance.IsWalkable(currentGridPos.x + moveDir.x, currentGridPos.y))
            {
                moveDir.x = 0;
                if (currentGridPos.y <= playerGridPos.y) moveDir.y = -1;
                else moveDir.y = 1;
            }
        }
        else
        {
            // --- CHASE LOGIC ---
            if (currentGridPos.x < playerGridPos.x) moveDir.x = 1;
            else if (currentGridPos.x > playerGridPos.x) moveDir.x = -1;
            else if (currentGridPos.y < playerGridPos.y) moveDir.y = 1;
            else if (currentGridPos.y > playerGridPos.y) moveDir.y = -1;
        }

        Vector2Int targetGrid = currentGridPos + moveDir;

        // Only move if tile is actually walkable
        if (GridManager.Instance.IsWalkable(targetGrid.x, targetGrid.y))
        {
            targetWorldPos = GridManager.Instance.GridToWorld(targetGrid.x, targetGrid.y);
            isMoving = true;
        }
    }
}