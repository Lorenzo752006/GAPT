using UnityEngine;
using System.Collections.Generic;

public class MCTSComplexAI : MonoBehaviour
{
    [Header("MCTS Settings")]
    [SerializeField] private int simulationsPerTurn = 1000; 
    [SerializeField] private int maxSimulationDepth = 10;   
    [SerializeField] private float moveSpeed = 5f;

    [Header("References")]
    [SerializeField] private GridPlayerController player;

    private Vector2Int gridPosition;
    private bool isMoving;
    private Vector3 targetWorldPosition;

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
        if (player == null) player = FindFirstObjectByType<GridPlayerController>();
        if (player == null) return;

        if (isMoving)
        {
            SmoothMove();
        }
        else
        {
            Vector2Int bestMove = RunMCTS();
            ExecuteMove(bestMove);
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
            if (!GridManager.Instance.IsWalkable(testPos.x, testPos.y)) continue;

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
            if (simulatedBossPos == simulatedPlayerPos) return 1.0f;
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
        if (direction == Vector2Int.zero) return;
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

    public Vector2Int GetGridPosition() 
    { 
        return gridPosition; 
    }

    public void SyncPosition(Vector2Int newGridPos, Vector3 newWorldPos)
    {
        gridPosition = newGridPos;
        targetWorldPosition = newWorldPos;
        transform.position = newWorldPos;
        isMoving = false;
    }
}