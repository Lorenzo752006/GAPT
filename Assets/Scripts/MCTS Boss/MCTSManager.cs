using UnityEngine;
using System.Collections.Generic;

public class ComplexBoss : MonoBehaviour
{
    [Header("MCTS Settings")]
    [SerializeField] private int simulationsPerTurn = 1000; 
    [SerializeField] private int maxSimulationDepth = 10;   
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float moveCooldown = 0.3f; 

    private GridPlayerController player;
    private Vector2Int gridPosition;
    private Vector3 targetWorldPosition;
    private bool isMoving;
    private float cooldownTimer; 

    private void Update()
    {
        if (player == null)
        {
            player = FindFirstObjectByType<GridPlayerController>();
            if (player == null) return;
        }

        if (isMoving)
        {
            SmoothMove();
        }
        else
        {
            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer > 0f) return;

            Vector2Int bestMove = RunMCTS();
            if (bestMove != Vector2Int.zero)
            {
                ExecuteMove(bestMove);
                cooldownTimer = moveCooldown; 
            }
        }
    }

    private Vector2Int RunMCTS()
    {
        Vector2Int[] possibleMoves = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        Dictionary<Vector2Int, float> moveScores = new Dictionary<Vector2Int, float>();
        
        Vector2Int bestAction = Vector2Int.zero;
        float highestScore = float.NegativeInfinity;

        foreach (var move in possibleMoves)
        {
            Vector2Int testPos = gridPosition + move;
            
            if (!GridManager.Instance.IsWalkable(testPos.x, testPos.y))
                continue;

            moveScores[move] = 0;

            for (int i = 0; i < simulationsPerTurn; i++)
            {
                moveScores[move] += SimulatePlayout(testPos);
            }

            if (moveScores[move] > highestScore)
            {
                highestScore = moveScores[move];
                bestAction = move;
            }
        }

        return bestAction;
    }

    private float SimulatePlayout(Vector2Int startingPos)
    {
        Vector2Int simulatedBossPos = startingPos;
        Vector2Int simulatedPlayerPos = player.GetGridPosition();

        for (int depth = 0; depth < maxSimulationDepth; depth++)
        {
            if (simulatedBossPos == simulatedPlayerPos) 
                return 1.0f; 

            simulatedBossPos = GetRandomValidMove(simulatedBossPos);
        }

        float dist = Vector2Int.Distance(simulatedBossPos, simulatedPlayerPos);
        return 1.0f / (dist + 1.0f); 
    }

    private Vector2Int GetRandomValidMove(Vector2Int currentPos)
    {
        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        Vector2Int dir = dirs[Random.Range(0, dirs.Length)];
        Vector2Int target = currentPos + dir;

        return GridManager.Instance.IsWalkable(target.x, target.y) ? target : currentPos;
    }

    private void ExecuteMove(Vector2Int direction)
    {
        gridPosition += direction;
        targetWorldPosition = GridManager.Instance.GridToWorld(gridPosition.x, gridPosition.y);
        isMoving = true;
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

    // Manager Visibility Interfaces
    public Vector2Int GetGridPosition()
    {
        return gridPosition;
    }

    public void SyncPosition(Vector2Int newGridPos, Vector3 newWorldPos)
    {
        gridPosition = newGridPos;
        targetWorldPosition = newWorldPos;
        isMoving = false;
        cooldownTimer = moveCooldown; 
    }
}