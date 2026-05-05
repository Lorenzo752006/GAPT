using UnityEngine;
using UnityEngine.InputSystem;
using Task9; 

public class Task9PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 12f; 
    [SerializeField] private float moveCooldown = 0.1f;

    private Vector2Int gridPosition;
    private Vector3 targetWorldPosition;
    private bool isMoving;
    private float cooldownTimer;

    private void Start()
    {
        // Start at origin; Generator will move us later
        SetPosition(Vector2Int.zero);
    }

    /// <summary>
    /// Moves the player to a specific grid coordinate and snaps their world position.
    /// </summary>
    public void SetPosition(Vector2Int newGridPos)
    {
        gridPosition = newGridPos;
        if (GridManagerTask9.Instance != null)
        {
            targetWorldPosition = GridManagerTask9.Instance.GridToWorld(gridPosition.x, gridPosition.y);
            transform.position = targetWorldPosition;
            isMoving = false;
        }
    }

    private void Update()
    {
        HandleInput();
        SmoothMove();
    }

    private void HandleInput()
    {
        if (isMoving || Keyboard.current == null) return;

        cooldownTimer -= Time.deltaTime;
        if (cooldownTimer > 0f) return;

        Vector2Int direction = GetDirectionInput();
        if (direction != Vector2Int.zero)
        {
            TryMove(direction);
        }
    }

    private Vector2Int GetDirectionInput()
    {
        Keyboard kb = Keyboard.current;
        if (kb.wKey.isPressed || kb.upArrowKey.isPressed) return Vector2Int.up;
        if (kb.sKey.isPressed || kb.downArrowKey.isPressed) return Vector2Int.down;
        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) return Vector2Int.left;
        if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) return Vector2Int.right;
        return Vector2Int.zero;
    }

    private void TryMove(Vector2Int direction)
    {
        Vector2Int targetGrid = gridPosition + direction;

        if (GridManagerTask9.Instance != null && GridManagerTask9.Instance.IsWalkable(targetGrid.x, targetGrid.y))
        {
            gridPosition = targetGrid;
            targetWorldPosition = GridManagerTask9.Instance.GridToWorld(gridPosition.x, gridPosition.y);
            isMoving = true;
            cooldownTimer = moveCooldown;
        }
    }

    private void SmoothMove()
    {
        if (!isMoving) return;

        transform.position = Vector3.MoveTowards(transform.position, targetWorldPosition, moveSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetWorldPosition) < 0.001f)
        {
            transform.position = targetWorldPosition;
            isMoving = false;
        }
    }
}