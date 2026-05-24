using UnityEngine;
using System.Collections.Generic;

public class MCTSComplexAI : MonoBehaviour
{
    [Header("MCTS Settings")]
    [SerializeField] private int simulationsPerTurn = 1000; // How many "futures" to imagine
    [SerializeField] private int maxSimulationDepth = 10;   // How far into the future to look
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
        if (isMoving)
        {
            SmoothMove();
        }
        else
        {
            // Execute the MCTS decision loop
            Vector2Int bestMove = RunMCTS();
            ExecuteMove(bestMove);
        }
    }

    private Vector2Int RunMCTS()
    {
        Vector2Int[] possibleMoves = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right, Vector2Int.zero };
        Dictionary<Vector2Int, float> moveScores = new Dictionary<Vector2Int, float>();

        foreach (var move in possibleMoves)
        {
            moveScores[move] = 0;
            
            // Check if move is valid in the real world
            Vector2Int testPos = gridPosition + move;
            if (move != Vector2Int.zero && !GridManager.Instance.IsWalkable(testPos.x, testPos.y))
                continue;

            // Run thousands of simulations for this specific starting move
            for (int i = 0; i < simulationsPerTurn; i++)
            {
                moveScores[move] += SimulatePlayout(testPos);
            }
        }

        // Find the move with the highest average win ratio/score
        Vector2Int bestAction = Vector2Int.zero;
        float highestScore = float.NegativeInfinity;

        foreach (var score in moveScores)
        {
            if (score.Value > highestScore)
            {
                highestScore = score.Value;
                bestAction = score.Key;
            }
        }

        return bestAction;
    }

    // "Imagine" the future without affecting the real world
    private float SimulatePlayout(Vector2Int startingPos)
    {
        Vector2Int simulatedBossPos = startingPos;
        Vector2Int simulatedPlayerPos = player.GetGridPosition();

        for (int depth = 0; depth < maxSimulationDepth; depth++)
        {
            // Simple Win/Loss condition for simulation: Did we catch the player?
            if (simulatedBossPos == simulatedPlayerPos) return 1.0f;

            // Randomly move the "imaginary" boss
            simulatedBossPos = GetRandomValidMove(simulatedBossPos);
        }

        // If time ran out, score based on proximity (Heuristic)
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