using UnityEngine;
using UnityEngine.InputSystem; // Add this namespace

public class GridPlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float moveCooldown = 0.15f;

    private Vector2Int gridPosition;
    private Vector3 targetWorldPosition;
    private bool isMoving;
    private float cooldownTimer;

    [Header("Starting Position")]
    [SerializeField] private int startX = 1;
    [SerializeField] private int startY = 1;

    private void Start()
    {
        // Place the player at a valid starting grid position
        gridPosition = new Vector2Int(startX, startY);
        targetWorldPosition = GridManager.Instance.GridToWorld(gridPosition.x, gridPosition.y);
        transform.position = targetWorldPosition;
    }

    private void Update()
    {
        HandleInput();
        SmoothMove();
    }

    private void HandleInput()
    {
        if (isMoving)
            return;

        cooldownTimer -= Time.deltaTime;
        if (cooldownTimer > 0f)
            return;

        Vector2Int direction = Vector2Int.zero;
        Keyboard kb = Keyboard.current;

        if (kb == null)
            return;

        if (kb.wKey.isPressed || kb.upArrowKey.isPressed)
            direction = Vector2Int.up;
        else if (kb.sKey.isPressed || kb.downArrowKey.isPressed)
            direction = Vector2Int.down;
        else if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)
            direction = Vector2Int.left;
        else if (kb.dKey.isPressed || kb.rightArrowKey.isPressed)
            direction = Vector2Int.right;

        if (direction == Vector2Int.zero)
            return;

        TryMove(direction);
    }

    private void TryMove(Vector2Int direction)
    {
        Vector2Int targetGrid = gridPosition + direction;

        // Ask the GridManager if the target cell is walkable
        if (GridManager.Instance.IsWalkable(targetGrid.x, targetGrid.y))
        {
            gridPosition = targetGrid;
            targetWorldPosition = GridManager.Instance.GridToWorld(gridPosition.x, gridPosition.y);
            isMoving = true;
            cooldownTimer = moveCooldown;
        }
    }

    private void SmoothMove()
    {
        if (!isMoving)
            return;

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetWorldPosition,
            moveSpeed * Time.deltaTime
        );

        if (Vector3.Distance(transform.position, targetWorldPosition) < 0.01f)
        {
            transform.position = targetWorldPosition;
            isMoving = false;
        }
    }

    /// <summary>
    /// Returns the player's current logical grid position.
    /// </summary>
    public Vector2Int GetGridPosition()
    {
        return gridPosition;
    }
}