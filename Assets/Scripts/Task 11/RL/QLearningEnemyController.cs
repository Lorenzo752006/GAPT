using UnityEngine;

/// <summary>
/// MonoBehaviour that controls an enemy using the trained Q-Table.
/// 
/// Instead of hardcoded chase/flee logic, this enemy consults the Q-Table
/// to decide which direction to move. The Q-Table was trained by thousands
/// of simulated episodes, so the enemy has "learned" the optimal path to
/// the player from every position on the map — including navigating around
/// walls and through doorways.
/// 
/// The enemy uses the existing EnemyLocomotionTask6 steering system for
/// smooth, physics-based movement. The Q-Table decides WHERE to go;
/// the steering system handles HOW to get there.
/// </summary>
[RequireComponent(typeof(EnemyLocomotionTask6))]
public class QLearningEnemyController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;

    [Header("Decision Timing")]
    [Tooltip("How often the enemy queries the Q-Table for a new direction (seconds).")]
    [SerializeField] private float decisionInterval = 0.3f;

    [Header("Waypoint")]
    [Tooltip("How close the enemy must be to the cell center to count as 'in' that cell.")]
    [SerializeField] private float cellEntryThreshold = 0.35f;

    [Tooltip("How many cells ahead to place the waypoint (gives steering more lead).")]
    [SerializeField] private int lookAheadCells = 3;

    [Header("Debug")]
    [SerializeField] private bool showGizmos = true;

    [Header("Runtime Info (Read Only)")]
    [SerializeField] private string currentAction = "—";
    [SerializeField] private string currentGridPos = "—";
    [SerializeField] private string qValues = "—";

    private EnemyLocomotionTask6 locomotion;
    private Transform waypointHelper;
    private float decisionTimer;
    private Vector2Int confirmedGrid;
    private int lastAction = -1;
    private bool hasConfirmedCell;

    private void Start()
    {
        locomotion = GetComponent<EnemyLocomotionTask6>();

        // Create a lightweight transform as the steering target
        waypointHelper = new GameObject($"{gameObject.name}_QLWaypoint").transform;
        waypointHelper.position = transform.position;

        locomotion.SetTarget(waypointHelper);
        locomotion.SetFlee(false);

        // Q-Learning waypoints are 1 cell apart — reduce stop distance so
        // the steering system doesn't brake before reaching them.
        locomotion.SetStopDistance(0.05f);
    }

    private void Update()
    {
        QLearningTrainer trainer = QLearningTrainer.Instance;
        if (trainer == null || !trainer.IsReady) return;
        if (player == null) return;

        GridManager gm = GridManager.Instance;
        if (gm == null) return;

        // Determine which grid cell the enemy is firmly inside
        Vector2Int rawGrid = gm.WorldToGrid(transform.position);
        Vector3 cellCenter = gm.GridToWorld(rawGrid.x, rawGrid.y);
        float distToCenter = Vector2.Distance(transform.position, cellCenter);

        // Only update the confirmed cell when close to a cell center
        // This prevents flip-flopping at cell borders
        if (distToCenter < cellEntryThreshold)
        {
            if (!hasConfirmedCell || rawGrid != confirmedGrid)
            {
                confirmedGrid = rawGrid;
                hasConfirmedCell = true;
            }
        }

        if (!hasConfirmedCell)
        {
            // First frame fallback: just use raw position
            confirmedGrid = rawGrid;
            hasConfirmedCell = true;
        }

        currentGridPos = $"({confirmedGrid.x},{confirmedGrid.y})";

        decisionTimer -= Time.deltaTime;

        // Make a new decision if:
        // 1. Timer expired, OR
        // 2. We've entered a new cell, OR
        // 3. First frame (no decision yet)
        bool needsDecision = decisionTimer <= 0f || lastAction < 0;

        if (needsDecision)
        {
            MakeDecision(trainer.Agent, gm, confirmedGrid);
            decisionTimer = decisionInterval;
        }
    }

    private void MakeDecision(QLearningAgent agent, GridManager gm, Vector2Int currentGrid)
    {
        // Clamp position to grid bounds
        int x = Mathf.Clamp(currentGrid.x, 0, gm.Width - 1);
        int y = Mathf.Clamp(currentGrid.y, 0, gm.Height - 1);

        int state = agent.Table.PositionToState(x, y);
        int bestAction = agent.Table.GetBestAction(state);

        // Walk multiple cells ahead following the Q-Table policy to create
        // a further-out waypoint. This gives the steering system a distant
        // target so it maintains momentum instead of braking every cell.
        int wpX = x;
        int wpY = y;

        for (int step = 0; step < lookAheadCells; step++)
        {
            int s = agent.Table.PositionToState(wpX, wpY);
            int action = agent.Table.GetBestAction(s);
            Vector2Int dir = QTable.ActionToDirection(action);
            int nextX = wpX + dir.x;
            int nextY = wpY + dir.y;

            if (gm.IsWalkable(nextX, nextY))
            {
                wpX = nextX;
                wpY = nextY;
                // Use the first step's action as the reported action
                if (step == 0) bestAction = action;
            }
            else
            {
                break;
            }
        }

        Vector3 targetWorld = gm.GridToWorld(wpX, wpY);
        waypointHelper.position = targetWorld;

        // If we didn't move at all (stuck), try fallback actions from current cell
        if (wpX == x && wpY == y)
        {
            for (int a = 0; a < QTable.ActionCount; a++)
            {
                if (a == bestAction) continue;
                Vector2Int altDir = QTable.ActionToDirection(a);
                int altX = x + altDir.x;
                int altY = y + altDir.y;
                if (gm.IsWalkable(altX, altY))
                {
                    waypointHelper.position = gm.GridToWorld(altX, altY);
                    bestAction = a;
                    break;
                }
            }
        }

        lastAction = bestAction;

        // Update debug info
        currentAction = ((QTable.Action)bestAction).ToString();
        qValues = agent.Table.DebugState(state);
    }

    private void OnDestroy()
    {
        if (waypointHelper != null)
            Destroy(waypointHelper.gameObject);
    }

    // ???????????????????????????????????????????????
    //  GIZMOS
    // ???????????????????????????????????????????????

    private void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;
        if (!Application.isPlaying) return;

        // Draw line from enemy to current waypoint
        if (waypointHelper != null)
        {
            Gizmos.color = new Color(0f, 1f, 0.5f, 0.9f);
            Gizmos.DrawLine(transform.position, waypointHelper.position);
            Gizmos.DrawWireSphere(waypointHelper.position, 0.2f);
        }

        // Draw the best action arrow at the enemy's current cell
        if (lastAction >= 0)
        {
            Vector2Int dir = QTable.ActionToDirection(lastAction);
            Vector3 arrowStart = transform.position;
            Vector3 arrowEnd = arrowStart + new Vector3(dir.x, dir.y, 0f) * 0.6f;
            Gizmos.color = new Color(1f, 1f, 0f, 0.9f);
            Gizmos.DrawLine(arrowStart, arrowEnd);
            Gizmos.DrawSphere(arrowEnd, 0.1f);
        }

#if UNITY_EDITOR
        GUIStyle style = new GUIStyle();
        style.normal.textColor = new Color(0f, 1f, 0.5f);
        style.fontStyle = FontStyle.Bold;
        style.fontSize = 11;
        style.alignment = TextAnchor.MiddleCenter;
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 1.0f,
            $"QL: {currentAction}",
            style
        );
#endif
    }
}
