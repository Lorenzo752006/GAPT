using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(EnemyLocomotionTask6))]
public class QLearningEnemyController : MonoBehaviour
{
    [Header("Path Following")]
    [SerializeField] private float waypointTolerance = 0.3f;

    [Header("Heatmap Debug")]
    [SerializeField] private bool showHeatmap = false;

    private EnemyLocomotionTask6 locomotion;
    private Transform waypointHelper;
    private QLearningTask11Visualizer visualizer;

    private List<Vector3> completePath = new List<Vector3>();
    private int currentWaypointIndex;
    private bool pathExtracted;
    private bool hasReachedGoal;

    public IReadOnlyList<Vector3> CompletePath => completePath;
    public int CurrentWaypointIndex => currentWaypointIndex;
    public bool HeatmapVisible => showHeatmap;

    private void OnEnable()
    {
        if (QLearningTrainer.Instance != null)
            QLearningTrainer.Instance.OnRetrainingComplete += RefreshPath;
    }

    private void OnDisable()
    {
        if (QLearningTrainer.Instance != null)
            QLearningTrainer.Instance.OnRetrainingComplete -= RefreshPath;
    }

    private void Start()
    {
        locomotion = GetComponent<EnemyLocomotionTask6>();
        waypointHelper = new GameObject($"{gameObject.name}_QLWaypoint").transform;
        waypointHelper.position = transform.position;

        locomotion.SetTarget(waypointHelper);
        locomotion.SetFlee(false);
        locomotion.SetStopDistance(0.1f);

        SetupRuntimeVisuals();
    }

    private void Update()
    {
        QLearningTrainer trainer = QLearningTrainer.Instance;
        GridManager grid = GridManager.Instance;

        if (trainer == null || !trainer.IsReady || grid == null)
        {
            ClearPathIfNeeded();
            return;
        }

        if (!pathExtracted)
        {
            ExtractEntirePath(trainer.Agent, grid);
            pathExtracted = true;
            hasReachedGoal = false;

            if (completePath.Count > 0)
            {
                waypointHelper.position = completePath[0];
                locomotion.SetTarget(waypointHelper);
            }

            return;
        }

        FollowPath(trainer);
    }

    private void SetupRuntimeVisuals()
    {
        visualizer = GetComponent<QLearningTask11Visualizer>();
        if (visualizer == null)
            visualizer = gameObject.AddComponent<QLearningTask11Visualizer>();

        visualizer.Initialize(this);
        visualizer.SetVisible(true);
        visualizer.SetHeatmapVisible(showHeatmap);
    }

    public void ToggleRuntimeVisuals()
    {
        SetHeatmapVisible(!showHeatmap);
    }

    public void SetRuntimeVisualsVisible(bool visible)
    {
        SetHeatmapVisible(visible);
    }

    public void SetHeatmapVisible(bool visible)
    {
        showHeatmap = visible;

        if (visualizer == null)
            SetupRuntimeVisuals();
        else
            visualizer.SetHeatmapVisible(showHeatmap);
    }

    public void ShowRuntimeVisuals()
    {
        SetHeatmapVisible(true);
    }

    public void HideRuntimeVisuals()
    {
        SetHeatmapVisible(false);
    }

    public void ToggleRuntimeVisualsForAllEnemies()
    {
        bool nextVisible = !showHeatmap;
        QLearningEnemyController[] enemies = FindObjectsByType<QLearningEnemyController>(FindObjectsSortMode.None);

        for (int i = 0; i < enemies.Length; i++)
        {
            enemies[i].SetHeatmapVisible(nextVisible);
        }
    }

    private void RefreshPath()
    {
        pathExtracted = false;
        hasReachedGoal = false;
        completePath.Clear();
        currentWaypointIndex = 0;

        if (locomotion != null)
            locomotion.SetTarget(null);

        Debug.Log("[QLearningEnemy] Path reset. Extracting a new path.");
    }

    private void ClearPathIfNeeded()
    {
        if (!pathExtracted)
            return;

        pathExtracted = false;
        completePath.Clear();

        if (locomotion != null)
            locomotion.SetTarget(null);
    }

    private void FollowPath(QLearningTrainer trainer)
    {
        if (currentWaypointIndex < completePath.Count)
        {
            Vector3 targetPosition = completePath[currentWaypointIndex];
            waypointHelper.position = targetPosition;

            Vector2 current2D = new Vector2(transform.position.x, transform.position.y);
            Vector2 target2D = new Vector2(targetPosition.x, targetPosition.y);

            if (Vector2.Distance(current2D, target2D) <= waypointTolerance)
                currentWaypointIndex++;

            return;
        }

        locomotion.SetTarget(null);
        if (!hasReachedGoal && trainer.TestRandomGoalSpawn)
        {
            hasReachedGoal = true;
            trainer.SpawnRandomGoal();
        }
    }

    private void ExtractEntirePath(QLearningAgent agent, GridManager grid)
    {
        completePath.Clear();
        currentWaypointIndex = 0;

        Vector2Int currentGrid = grid.WorldToGrid(transform.position);
        int x = Mathf.Clamp(currentGrid.x, 0, grid.Width - 1);
        int y = Mathf.Clamp(currentGrid.y, 0, grid.Height - 1);

        HashSet<Vector2Int> visitedCells = new HashSet<Vector2Int>();
        int maxFailsafeSteps = grid.Width * grid.Height;
        int previousAction = -1;
        List<Vector3> rawPath = new List<Vector3>();

        for (int step = 0; step < maxFailsafeSteps; step++)
        {
            Vector2Int position = new Vector2Int(x, y);
            visitedCells.Add(position);
            rawPath.Add(grid.GridToWorld(x, y));

            int state = agent.Table.PositionToState(x, y);
            int bestAction = GetBestWalkableAction(agent, grid, state, x, y, visitedCells, previousAction);
            if (bestAction == -1)
                break;

            Vector2Int direction = QTable.ActionToDirection(bestAction);
            x += direction.x;
            y += direction.y;
            previousAction = bestAction;
        }

        SimplifyPath(rawPath);
    }

    private int GetBestWalkableAction(QLearningAgent agent, GridManager grid, int state, int x, int y,
                                      HashSet<Vector2Int> visited, int previousAction)
    {
        int bestAction = -1;
        float bestValue = float.MinValue;
        bool hasValidPath = false;

        for (int action = 0; action < QTable.ActionCount; action++)
        {
            Vector2Int direction = QTable.ActionToDirection(action);
            int testX = x + direction.x;
            int testY = y + direction.y;
            Vector2Int testPosition = new Vector2Int(testX, testY);

            if (!grid.IsWalkable(testX, testY) || visited.Contains(testPosition))
                continue;

            float qValue = agent.Table.GetQ(state, action);
            if (Mathf.Abs(qValue) > 0.0001f)
                hasValidPath = true;

            if (action == previousAction)
                qValue += 0.001f;

            if (qValue > bestValue)
            {
                bestValue = qValue;
                bestAction = action;
            }
        }

        return hasValidPath ? bestAction : -1;
    }

    private void SimplifyPath(List<Vector3> rawPath)
    {
        if (rawPath.Count <= 2)
        {
            completePath = new List<Vector3>(rawPath);
            return;
        }

        completePath.Add(rawPath[0]);
        for (int i = 1; i < rawPath.Count - 1; i++)
        {
            Vector3 previous = rawPath[i - 1];
            Vector3 current = rawPath[i];
            Vector3 next = rawPath[i + 1];
            bool changedDirection = Vector3.Distance((current - previous).normalized, (next - current).normalized) > 0.01f;

            if (changedDirection)
                completePath.Add(current);
        }

        completePath.Add(rawPath[rawPath.Count - 1]);
    }

    private void OnDestroy()
    {
        if (waypointHelper != null)
            Destroy(waypointHelper.gameObject);
    }
}
