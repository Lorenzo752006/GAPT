using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(EnemyLocomotionTask6))]
public class QLearningEnemyController : MonoBehaviour
{
    [Header("Path Following")]
    [SerializeField] private float waypointTolerance = 0.3f;

    [Header("Debug")]
    [SerializeField] private bool showGizmos = true;

    private EnemyLocomotionTask6 locomotion;
    private Transform waypointHelper;
    
    private List<Vector3> completePath = new List<Vector3>();
    private int currentWaypointIndex = 0;
    private bool pathExtracted = false;
    private bool hasReachedGoal = false;

    // --- PART 2: EVENT SUBSCRIPTION ---
    private void OnEnable()
    {
        // Listen for when training finishes
        if (QLearningTrainer.Instance != null)
            QLearningTrainer.Instance.OnRetrainingComplete += RefreshPath;
    }

    private void OnDisable()
    {
        // Stop listening if this object is destroyed
        if (QLearningTrainer.Instance != null)
            QLearningTrainer.Instance.OnRetrainingComplete -= RefreshPath;
    }

    private void RefreshPath()
{
    pathExtracted = false;
    hasReachedGoal = false; // Reset this so it doesn't trigger immediately
    completePath.Clear();
    currentWaypointIndex = 0;
    
    // Explicitly tell locomotion to stop and wait for the new target
    if (locomotion != null) locomotion.SetTarget(null);

    Debug.Log("[QLearningEnemy] Path Reset - Extracting new path...");
}

    private void Start()
    {
        locomotion = GetComponent<EnemyLocomotionTask6>();
        waypointHelper = new GameObject($"{gameObject.name}_QLWaypoint").transform;
        waypointHelper.position = transform.position;

        locomotion.SetTarget(waypointHelper);
        locomotion.SetFlee(false);
        locomotion.SetStopDistance(0.1f);
    }

    private void Update()
    {
        QLearningTrainer trainer = QLearningTrainer.Instance;
        GridManager gm = GridManager.Instance;

        if (trainer == null || !trainer.IsReady || gm == null) 
        {
            if (pathExtracted) 
            {
                pathExtracted = false; 
                completePath.Clear();
                locomotion.SetTarget(null);
            }
            return;
        }

        if (!pathExtracted)
    {
        ExtractEntirePath(trainer.Agent, gm);
        pathExtracted = true;
        hasReachedGoal = false;

        // --- ADD THIS LINE ---
        // Ensure the locomotion script is actually looking at our helper
        if (completePath.Count > 0) 
        {
            waypointHelper.position = completePath[0];
            locomotion.SetTarget(waypointHelper); 
        }
        return;
    }

        if (currentWaypointIndex < completePath.Count)
        {
            Vector3 targetPosition = completePath[currentWaypointIndex];
            waypointHelper.position = targetPosition;
            Vector2 current2D = new Vector2(transform.position.x, transform.position.y);
            Vector2 target2D = new Vector2(targetPosition.x, targetPosition.y);

            if (Vector2.Distance(current2D, target2D) <= waypointTolerance)
            {
                currentWaypointIndex++;
            }
        }
        else
        {
            locomotion.SetTarget(null);
            if (!hasReachedGoal && trainer.TestRandomGoalSpawn)
            {
                hasReachedGoal = true;
                trainer.SpawnRandomGoal();
            }
        }
    }

    private void ExtractEntirePath(QLearningAgent agent, GridManager gm)
    {
        completePath.Clear();
        currentWaypointIndex = 0;
        Vector2Int currentGrid = gm.WorldToGrid(transform.position);
        int x = Mathf.Clamp(currentGrid.x, 0, gm.Width - 1);
        int y = Mathf.Clamp(currentGrid.y, 0, gm.Height - 1);

        HashSet<Vector2Int> visitedCells = new HashSet<Vector2Int>();
        int maxFailsafeSteps = gm.Width * gm.Height;
        int stepsTaken = 0;
        int previousAction = -1; 
        List<Vector3> rawPath = new List<Vector3>();

        while (stepsTaken < maxFailsafeSteps)
        {
            Vector2Int pos = new Vector2Int(x, y);
            visitedCells.Add(pos);
            rawPath.Add(gm.GridToWorld(x, y));
            int state = agent.Table.PositionToState(x, y);
            int bestAction = GetBestWalkableAction(agent, gm, state, x, y, visitedCells, previousAction);

            if (bestAction == -1) break; 
            Vector2Int dir = QTable.ActionToDirection(bestAction);
            x += dir.x; y += dir.y;
            previousAction = bestAction;
            stepsTaken++;
        }

        if (rawPath.Count > 2)
        {
            completePath.Add(rawPath[0]);
            for (int i = 1; i < rawPath.Count - 1; i++)
            {
                Vector3 prev = rawPath[i - 1]; Vector3 curr = rawPath[i]; Vector3 next = rawPath[i + 1];
                if (Vector3.Distance((curr - prev).normalized, (next - curr).normalized) > 0.01f) completePath.Add(curr);
            }
            completePath.Add(rawPath[rawPath.Count - 1]);
        }
        else { completePath = new List<Vector3>(rawPath); }
    }

    private int GetBestWalkableAction(QLearningAgent agent, GridManager gm, int state, int x, int y, HashSet<Vector2Int> visited, int previousAction)
    {
        int bestAction = -1;
        float bestValue = float.MinValue;
        bool hasValidPath = false; 

        for (int a = 0; a < QTable.ActionCount; a++)
        {
            Vector2Int dir = QTable.ActionToDirection(a);
            int testX = x + dir.x; int testY = y + dir.y;
            Vector2Int testPos = new Vector2Int(testX, testY);

            if (gm.IsWalkable(testX, testY) && !visited.Contains(testPos))
            {
                float qValue = agent.Table.GetQ(state, a);
                if (Mathf.Abs(qValue) > 0.0001f) hasValidPath = true;
                if (a == previousAction) qValue += 0.001f; 

                if (qValue > bestValue) { bestValue = qValue; bestAction = a; }
            }
        }
        return hasValidPath ? bestAction : -1;
    }

    private void OnDestroy() { if (waypointHelper != null) Destroy(waypointHelper.gameObject); }
    private void OnDrawGizmos()
    {
        if (!showGizmos || !Application.isPlaying || completePath == null) return;
         Gizmos.color = Color.cyan;
        for (int i = 0; i < completePath.Count - 1; i++)
        {
            Gizmos.DrawLine(completePath[i], completePath[i + 1]);
            Gizmos.DrawWireSphere(completePath[i], 0.1f);
        }
    }
}