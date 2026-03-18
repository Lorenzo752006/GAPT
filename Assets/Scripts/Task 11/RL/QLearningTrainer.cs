using UnityEngine;

/// <summary>
/// MonoBehaviour that runs fast Q-Learning training episodes on the GridManager grid.
/// 
/// Attach to any GameObject in the scene. On Start, it runs thousands of simulated
/// episodes where a virtual agent explores the map, receiving rewards for reaching
/// the goal (player position) and punishments for hitting walls.
/// 
/// The trained Q-Table is then available for QLearningEnemyController to use
/// at runtime, giving enemies learned navigation knowledge.
/// 
/// Training is headless — no visual movement occurs. The simulation loop runs
/// entirely in-memory on the logical grid, making it extremely fast.
/// </summary>
public class QLearningTrainer : MonoBehaviour
{
    public static QLearningTrainer Instance { get; private set; }

    [Header("Goal Target")]
    [SerializeField] private Transform player;
    [SerializeField] private int goalX = 18;
    [SerializeField] private int goalY = 13;
    [SerializeField] private bool usePlayerPositionAsGoal = true;

    [Header("Training Parameters")]
    [Tooltip("Total number of training episodes to run.")]
    [SerializeField] private int totalEpisodes = 5000;

    [Tooltip("Maximum steps per episode before giving up.")]
    [SerializeField] private int maxStepsPerEpisode = 200;

    [Tooltip("How fast the agent learns. Range 0-1.")]
    [SerializeField] private float learningRate = 0.1f;

    [Tooltip("How much future rewards are valued. Range 0-1.")]
    [SerializeField] private float discountFactor = 0.95f;

    [Tooltip("Starting exploration rate. Decays over training.")]
    [SerializeField] private float startExplorationRate = 1.0f;

    [Tooltip("Minimum exploration rate (never stops exploring entirely).")]
    [SerializeField] private float minExplorationRate = 0.01f;

    [Tooltip("Rate at which exploration decays per episode.")]
    [SerializeField] private float explorationDecay = 0.995f;

    [Header("Rewards")]
    [SerializeField] private float rewardGoal = 100f;
    [SerializeField] private float penaltyWall = -10f;
    [SerializeField] private float penaltyStep = -1f;

    [Header("Retraining")]
    [Tooltip("If true, retrain the Q-Table every time the goal changes position.")]
    [SerializeField] private bool retrainOnGoalChange = true;

    [Tooltip("How often to check if the goal moved (seconds). 0 = every frame.")]
    [SerializeField] private float retrainCheckInterval = 1f;

    [Tooltip("Episodes to run when retraining (can be fewer than initial training).")]
    [SerializeField] private int retrainEpisodes = 2000;

    [Header("Debug")]
    [SerializeField] private bool showTrainingLog = true;
    [SerializeField] private bool showQValueGizmos = true;

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

    /// <summary>
    /// Returns the trained Q-Learning agent (and its Q-Table).
    /// </summary>
    public QLearningAgent Agent => agent;

    /// <summary>
    /// True when initial training is complete and the Q-Table is ready.
    /// </summary>
    public bool IsReady => isTrainingComplete;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        GridManager gm = GridManager.Instance;
        if (gm == null)
        {
            Debug.LogError("[QLearningTrainer] GridManager not found! Cannot train.");
            trainingStatus = "ERROR: No GridManager";
            return;
        }

        // Initialize the agent with a snapshot of the grid
        agent = new QLearningAgent(
            gm.grid, gm.Width, gm.Height,
            learningRate, discountFactor, startExplorationRate,
            rewardGoal, penaltyWall, penaltyStep
        );

        // Defer initial training to the first Update so all other Start() methods
        // (e.g. the player controller setting its grid position) have finished.
        needsInitialTraining = true;
    }

    private void Update()
    {
        // Run deferred initial training on the first frame
        if (needsInitialTraining)
        {
            needsInitialTraining = false;
            UpdateGoalPosition();
            Train(totalEpisodes);
        }

        if (!isTrainingComplete || !retrainOnGoalChange) return;

        // Periodically check if the goal has moved
        retrainTimer -= Time.deltaTime;
        if (retrainTimer > 0f) return;
        retrainTimer = retrainCheckInterval;

        Vector2Int newGoal = GetGoalGridPosition();
        if (newGoal != lastGoalPosition)
        {
            if (showTrainingLog)
                Debug.Log($"[QLearningTrainer] Goal moved to ({newGoal.x},{newGoal.y}). Retraining...");

            agent.SetGoal(newGoal);
            agent.Table.Reset();
            agent.ExplorationRate = startExplorationRate;
            Train(retrainEpisodes);
            lastGoalPosition = newGoal;
        }
    }

    /// <summary>
    /// Runs the fast training simulation loop.
    /// All episodes run in a single frame — no coroutines needed.
    /// </summary>
    private void Train(int episodes)
    {
        trainingStatus = "Training...";
        float epsilon = agent.ExplorationRate;

        int logInterval = Mathf.Max(1, episodes / 10);

        for (int episode = 0; episode < episodes; episode++)
        {
            int steps = agent.RunEpisode(maxStepsPerEpisode);

            // Decay exploration: gradually shift from random exploration to exploitation
            epsilon *= explorationDecay;
            epsilon = Mathf.Max(epsilon, minExplorationRate);
            agent.ExplorationRate = epsilon;

            episodesCompleted = episode + 1;
            currentExplorationRate = epsilon;
            lastEpisodeSteps = steps;

            if (showTrainingLog && (episode + 1) % logInterval == 0)
            {
                Debug.Log($"[QLearningTrainer] Episode {episode + 1}/{episodes} | " +
                          $"Steps: {steps} | Epsilon: {epsilon:F4}");
            }
        }

        isTrainingComplete = true;
        trainingStatus = $"Complete ({episodes} episodes)";

        if (showTrainingLog)
        {
            Debug.Log($"[QLearningTrainer] Training complete! {episodes} episodes. " +
                      $"Final epsilon: {epsilon:F4}");
        }
    }

    private void UpdateGoalPosition()
    {
        Vector2Int goal = GetGoalGridPosition();

        // Validate the goal is on a walkable cell
        GridManager gm = GridManager.Instance;
        if (gm != null && !gm.IsWalkable(goal.x, goal.y))
        {
            if (showTrainingLog)
                Debug.LogWarning($"[QLearningTrainer] Goal ({goal.x},{goal.y}) is not walkable! Searching for nearest walkable cell...");

            goal = FindNearestWalkable(goal, gm);

            if (showTrainingLog)
                Debug.Log($"[QLearningTrainer] Corrected goal to ({goal.x},{goal.y}).");
        }

        if (showTrainingLog)
            Debug.Log($"[QLearningTrainer] Initial goal set to ({goal.x},{goal.y}).");

        agent.SetGoal(goal);
        lastGoalPosition = goal;
    }

    /// <summary>
    /// Finds the nearest walkable cell to the given position using a BFS-like expanding search.
    /// </summary>
    private Vector2Int FindNearestWalkable(Vector2Int pos, GridManager gm)
    {
        for (int radius = 1; radius < Mathf.Max(gm.Width, gm.Height); radius++)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    if (Mathf.Abs(dx) != radius && Mathf.Abs(dy) != radius) continue;
                    int nx = pos.x + dx;
                    int ny = pos.y + dy;
                    if (gm.IsWalkable(nx, ny))
                        return new Vector2Int(nx, ny);
                }
            }
        }
        // Fallback: return original (shouldn't happen on a valid map)
        return pos;
    }

    private Vector2Int GetGoalGridPosition()
    {
        if (usePlayerPositionAsGoal && player != null)
        {
            return GridManager.Instance.WorldToGrid(player.position);
        }
        return new Vector2Int(goalX, goalY);
    }

    // ???????????????????????????????????????????????
    //  GIZMOS — Visualize Q-values on the grid
    // ???????????????????????????????????????????????

    private void OnDrawGizmosSelected()
    {
        if (!showQValueGizmos || agent == null || !isTrainingComplete) return;

        DrawQValueGizmos();
    }

    private void DrawQValueGizmos()
    {
        GridManager gm = GridManager.Instance;
        if (gm == null) return;

        QTable table = agent.Table;

        // Find the global max Q-value for normalization
        float globalMax = 0.001f;
        for (int x = 0; x < gm.Width; x++)
        {
            for (int y = 0; y < gm.Height; y++)
            {
                if (gm.grid[x, y] != CellType.Floor) continue;
                int state = table.PositionToState(x, y);
                float maxQ = table.GetMaxQ(state);
                if (maxQ > globalMax) globalMax = maxQ;
            }
        }

        for (int x = 0; x < gm.Width; x++)
        {
            for (int y = 0; y < gm.Height; y++)
            {
                if (gm.grid[x, y] != CellType.Floor) continue;

                Vector3 worldPos = gm.GridToWorld(x, y);
                int state = table.PositionToState(x, y);
                int bestAction = table.GetBestAction(state);
                float maxQ = table.GetMaxQ(state);

                // Color intensity based on Q-value (brighter = higher value)
                float intensity = Mathf.Clamp01(maxQ / globalMax);
                Gizmos.color = new Color(0f, intensity, 1f - intensity, 0.5f);
                Gizmos.DrawCube(worldPos, Vector3.one * gm.CellSize * 0.8f);

                // Draw an arrow showing the best action direction
                if (maxQ > 0)
                {
                    Vector2Int dir = QTable.ActionToDirection(bestAction);
                    Vector3 arrowEnd = worldPos + new Vector3(dir.x, dir.y, 0f) * gm.CellSize * 0.35f;
                    Gizmos.color = new Color(1f, 1f, 0f, 0.8f);
                    Gizmos.DrawLine(worldPos, arrowEnd);
                    Gizmos.DrawWireSphere(arrowEnd, 0.08f);
                }
            }
        }

        // Highlight the goal
        Vector3 goalWorld = gm.GridToWorld(agent.GoalPosition.x, agent.GoalPosition.y);
        Gizmos.color = new Color(0f, 1f, 0f, 0.8f);
        Gizmos.DrawWireSphere(goalWorld, gm.CellSize * 0.5f);
        float s = gm.CellSize * 0.3f;
        Gizmos.DrawLine(goalWorld + Vector3.left * s, goalWorld + Vector3.right * s);
        Gizmos.DrawLine(goalWorld + Vector3.down * s, goalWorld + Vector3.up * s);
    }
}
