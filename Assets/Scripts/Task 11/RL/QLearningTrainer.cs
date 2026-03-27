using UnityEngine;
using System.Collections.Generic;
using System; // Added for Action

public class QLearningTrainer : MonoBehaviour
{
    public static QLearningTrainer Instance { get; private set; }

    // --- NEW EVENT FOR PART 2 ---
    public Action OnRetrainingComplete; 

    [Header("Goal Target")]
    [SerializeField] private Transform player;
    [SerializeField] private int goalX = 18;
    [SerializeField] private int goalY = 13;
    [SerializeField] private bool usePlayerPositionAsGoal = true;

    [Header("Training Parameters")]
    [SerializeField] private int totalEpisodes = 50000;
    [SerializeField] private int maxStepsPerEpisode = 1000;
    
    [Tooltip("How fast the agent learns. Range 0-1.")]
    [SerializeField] private float learningRate = 0.2f; 

    [Tooltip("How much future rewards are valued. Range 0-1.")]
    [SerializeField] private float discountFactor = 0.95f; 

    [SerializeField] private float startExplorationRate = 1.0f;
    [SerializeField] private float minExplorationRate = 0.01f;
    [SerializeField] private float explorationDecay = 0.9999f; 

    [Header("Rewards")]
    [SerializeField] private float rewardGoal = 1000f; 
    [SerializeField] private float penaltyWall = -15f; 
    [SerializeField] private float penaltyStep = -1f;  

    [Header("Retraining")]
    [SerializeField] private bool retrainOnGoalChange = true;
    [SerializeField] private float retrainCheckInterval = 1f;
    [SerializeField] private int retrainEpisodes = 10000;

    [Header("Debug")]
    [SerializeField] private bool showTrainingLog = true;
    [SerializeField] private bool showQValueGizmos = true;

    [Header("Testing")]
    public bool TestRandomGoalSpawn = false; 

    [Header("Runtime Info (Read Only)")]
    [SerializeField] private string trainingStatus = "Not Started";
    [SerializeField] private int episodesCompleted;
    [SerializeField] private float currentExplorationRate;
    [SerializeField] private int lastEpisodeSteps;

    private QLearningAgent agent;
    private Vector2Int lastGoalPosition;
    private float retrainTimer;
    private bool isTrainingComplete;
    private bool needsInitialTraining;

    public QLearningAgent Agent => agent;
    public bool IsReady => isTrainingComplete;

    public void SpawnRandomGoal()
    {
        GridManager gm = GridManager.Instance;
        if (gm == null || agent == null) return;

        List<Vector2Int> walkableCells = new List<Vector2Int>();
        for (int x = 0; x < gm.Width; x++)
        {
            for (int y = 0; y < gm.Height; y++)
            {
                if (gm.IsWalkable(x, y) && (x != agent.CurrentPosition.x || y != agent.CurrentPosition.y))
                {
                    walkableCells.Add(new Vector2Int(x, y));
                }
            }
        }

        if (walkableCells.Count > 0)
        {
            Vector2Int randomGoal = walkableCells[UnityEngine.Random.Range(0, walkableCells.Count)];
            goalX = randomGoal.x;
            goalY = randomGoal.y;
            usePlayerPositionAsGoal = false; 

            agent.SetGoal(randomGoal);
            lastGoalPosition = randomGoal;
            agent.Table.Reset(); 
            agent.ExplorationRate = startExplorationRate; 

            Train(retrainEpisodes); 

            if (showTrainingLog)
                Debug.Log($"[QLearningTrainer] New goal at {randomGoal}. Retraining complete.");
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        GridManager gm = GridManager.Instance;
        if (gm == null) return;
        agent = new QLearningAgent(gm.grid, gm.Width, gm.Height, learningRate, discountFactor, startExplorationRate, rewardGoal, penaltyWall, penaltyStep);
        needsInitialTraining = true;
    }

    private void Update()
    {
        if (needsInitialTraining)
        {
            needsInitialTraining = false;
            UpdateGoalPosition();
            Train(totalEpisodes);
        }

        if (!isTrainingComplete || !retrainOnGoalChange) return;

        retrainTimer -= Time.deltaTime;
        if (retrainTimer <= 0f)
        {
            retrainTimer = retrainCheckInterval;
            Vector2Int currentGoal = GetGoalGridPosition();
            if (currentGoal != lastGoalPosition)
            {
                agent.SetGoal(currentGoal);
                agent.Table.Reset();
                agent.ExplorationRate = startExplorationRate;
                Train(retrainEpisodes);
                lastGoalPosition = currentGoal;
            }
        }
    }

    private void Train(int episodes)
    {
        trainingStatus = "Training...";
        float epsilon = agent.ExplorationRate;
        for (int episode = 0; episode < episodes; episode++)
        {
            int steps = agent.RunEpisode(maxStepsPerEpisode);
            epsilon *= explorationDecay;
            epsilon = Mathf.Max(epsilon, minExplorationRate);
            agent.ExplorationRate = epsilon;
            episodesCompleted = episode + 1;
            currentExplorationRate = epsilon;
            lastEpisodeSteps = steps;
        }
        isTrainingComplete = true;
        trainingStatus = $"Complete ({episodes} episodes)";

        // --- TRIGGER THE EVENT ---
        OnRetrainingComplete?.Invoke();
    }

    private void UpdateGoalPosition()
    {
        Vector2Int goal = GetGoalGridPosition();
        GridManager gm = GridManager.Instance;
        if (gm != null && !gm.IsWalkable(goal.x, goal.y)) goal = FindNearestWalkable(goal, gm);
        agent.SetGoal(goal);
        lastGoalPosition = goal;
    }

    private Vector2Int FindNearestWalkable(Vector2Int pos, GridManager gm)
    {
        for (int radius = 1; radius < Mathf.Max(gm.Width, gm.Height); radius++)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    if (Mathf.Abs(dx) != radius && Mathf.Abs(dy) != radius) continue;
                    int nx = pos.x + dx; int ny = pos.y + dy;
                    if (gm.IsWalkable(nx, ny)) return new Vector2Int(nx, ny);
                }
            }
        }
        return pos;
    }

    private Vector2Int GetGoalGridPosition()
    {
        if (usePlayerPositionAsGoal && player != null) return GridManager.Instance.WorldToGrid(player.position);
        return new Vector2Int(goalX, goalY);
    }

    private void OnDrawGizmosSelected() { if (showQValueGizmos && agent != null && isTrainingComplete) DrawQValueGizmos(); }
    private void DrawQValueGizmos()
    {
        GridManager gm = GridManager.Instance;
        if (gm == null) return;
        QTable table = agent.Table;
        float globalMax = 0.001f;
        for (int x = 0; x < gm.Width; x++)
        {
            for (int y = 0; y < gm.Height; y++)
            {
                if (gm.grid[x, y] != CellType.Floor) continue;
                float maxQ = table.GetMaxQ(agent.GetCurrentStateInfo(x, y));
                if (maxQ > globalMax) globalMax = maxQ;
            }
        }
        for (int x = 0; x < gm.Width; x++)
        {
            for (int y = 0; y < gm.Height; y++)
            {
                if (gm.grid[x, y] != CellType.Floor) continue;
                Vector3 worldPos = gm.GridToWorld(x, y);
                int state = agent.GetCurrentStateInfo(x, y);
                float maxQ = table.GetMaxQ(state);
                int bestAction = table.GetBestAction(state);
                float intensity = Mathf.Clamp01(maxQ / globalMax);
                Gizmos.color = new Color(0f, intensity, 1f - intensity, 0.4f);
                Gizmos.DrawCube(worldPos, Vector3.one * gm.CellSize * 0.8f);
                if (maxQ > 0)
                {
                    Vector2Int dir = QTable.ActionToDirection(bestAction);
                    Vector3 arrowEnd = worldPos + new Vector3(dir.x, dir.y, 0f) * gm.CellSize * 0.35f;
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawLine(worldPos, arrowEnd);
                }
            }
        }
    }
}