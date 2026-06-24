using UnityEngine;
using UnityEngine.InputSystem;
using Task9; 

public class Task9PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 12f; 
    [SerializeField] private float moveCooldown = 0.1f;

    [Header("Training (Random Walk)")]
    [SerializeField] private bool isRandomWalk = false; 
    [SerializeField] private float randomMoveInterval = 1.0f; // Seconds between random moves
    
    private Vector2Int gridPosition;
    private Vector3 targetWorldPosition;
    private bool isMoving;
    private float cooldownTimer;
    private float randomWalkTimer; // Tracks time for the next random move
    private bool isInitialized = false;

    private void Start()
    {
        targetWorldPosition = transform.position;
    }

    public void SetPosition(Vector2Int newGridPos)
    {
        if (GridManagerTask9.Instance != null)
        {
            gridPosition = newGridPos;
            targetWorldPosition = GridManagerTask9.Instance.GridToWorld(newGridPos.x, newGridPos.y);
            transform.position = targetWorldPosition;
            
            isMoving = false;
            cooldownTimer = 0f; 
            randomWalkTimer = 0f; // Reset timer on new episode
            isInitialized = true; 
        }
    }

    private void Update()
    {
        if (!isInitialized) return;

        if (isRandomWalk)
        {
            HandleRandomWalk();
        }
        else
        {
            HandleInput();
        }

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

    private void HandleRandomWalk()
    {
        if (isMoving) return;

        randomWalkTimer += Time.deltaTime;

        if (randomWalkTimer >= randomMoveInterval)
        {
            randomWalkTimer = 0f;
            
            // Pick a random direction: Up, Down, Left, Right
            Vector2Int[] directions = { 
                Vector2Int.up, 
                Vector2Int.down, 
                Vector2Int.left, 
                Vector2Int.right 
            };
            
            Vector2Int chosenDir = directions[Random.Range(0, directions.Length)];
            
            // TryMove already checks if the tile is walkable!
            TryMove(chosenDir);
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