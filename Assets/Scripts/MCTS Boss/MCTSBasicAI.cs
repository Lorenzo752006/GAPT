using UnityEngine;

public class BasicBoss : MonoBehaviour
{
    [SerializeField] private GridPlayerController player;
    [SerializeField] private float moveSpeed = 5f;

    private Vector2Int gridPosition;
    private Vector3 targetWorldPosition;
    private bool isMoving;

    void Start() {
        player = FindFirstObjectByType<GridPlayerController>();
    
        int spawnX = GridManager.Instance.Width - 2;
        int spawnY = GridManager.Instance.Height - 2;

        gridPosition = new Vector2Int(spawnX, spawnY);
    
        targetWorldPosition = GridManager.Instance.GridToWorld(spawnX, spawnY);
        transform.position = targetWorldPosition;

        isMoving = false;
    }

    private void Update()
    {
        if (isMoving) { SmoothMove(); return; }

        Vector2Int playerGrid = player.GetGridPosition();
        Vector2Int direction = Vector2Int.zero;

        int deltaX = playerGrid.x - gridPosition.x;
        int deltaY = playerGrid.y - gridPosition.y;

        // Naive greedy choice: step along the axis with the largest distance
        if (Mathf.Abs(deltaX) >= Mathf.Abs(deltaY) && deltaX != 0)
        {
            direction.x = deltaX > 0 ? 1 : -1;
        }
        else if (deltaY != 0)
        {
            direction.y = deltaY > 0 ? 1 : -1;
        }

        Vector2Int targetGrid = gridPosition + direction;

        // Basic check: Only move if it's not a wall. If it is a wall, it stops (trapped)
        if (GridManager.Instance.IsWalkable(targetGrid.x, targetGrid.y))
        {
            gridPosition = targetGrid;
            targetWorldPosition = GridManager.Instance.GridToWorld(gridPosition.x, gridPosition.y);
            isMoving = true;
        }
    }

    private void SmoothMove()
    {
        transform.position = Vector3.MoveTowards(transform.position, targetWorldPosition, moveSpeed * Time.deltaTime);
        if (Vector3.Distance(transform.position, targetWorldPosition) < 0.01f)
        {
            transform.position = targetWorldPosition;
            isMoving = false;
        }
    }

    public Vector2Int GetGridPosition() { 
        return gridPosition;
    }

    public void SyncPosition(Vector2Int newGridPos, Vector3 newWorldPos)
    {
        gridPosition = newGridPos;
        targetWorldPosition = newWorldPos;
        isMoving = false;
    }
}