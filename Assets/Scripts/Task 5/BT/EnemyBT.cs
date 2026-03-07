using UnityEngine;

/// <summary>
/// Assembles and ticks a Behaviour Tree for enemy AI.
/// 
/// Decision tree structure:
///   Selector (root)
///   ??? Sequence: "Flee" (highest priority — requires flee range + LOS)
///   ?   ??? CheckDistanceToPlayer (within flee range)
///   ?   ??? CheckLineOfSight
///   ?   ??? FleeFromPlayer
///   ??? Sequence: "Chase"
///   ?   ??? CheckDistanceToPlayer (within chase range)
///   ?   ??? CheckLineOfSight
///   ?   ??? MoveTowardsPlayer (records last known position)
///   ??? Sequence: "Investigate"
///   ?   ??? HasLastKnownLocation? (condition)
///   ?   ??? SearchArea (patrol near last known position)
///   ??? Patrol (wander when nothing to investigate)
/// </summary>
[RequireComponent(typeof(EnemyLocomotionTask5))]
public class EnemyBT : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;

    [Header("Behaviour Tuning")]
    [SerializeField] private float chaseRange = 8f;
    [SerializeField] private float fleeRange = 2f;

    [Header("Investigation")]
    [SerializeField] private float searchRadius = 3f;
    [SerializeField] private float searchDuration = 6f;

    [Header("Pathfinding")]
    [SerializeField] private bool usePathfinding = true;

    [Header("Debug")]
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private bool showGizmosAlways = false;

    private BTNode root;
    private EnemyLocomotionTask5 locomotion;
    private Transform waypointHelper;

    // Tracked state for gizmos and inspector
    private enum EnemyState { Patrol, Chase, Flee, Investigate }
    private EnemyState currentState = EnemyState.Patrol;
    private EnemyState previousState = EnemyState.Patrol;
    private BTNodeStatus lastRootStatus;
    private bool hasLineOfSight;

    // Last known location tracking
    private Vector3 lastKnownPlayerPosition;
    private bool hasLastKnownLocation;
    private SearchArea searchAreaNode;
    private Patrol patrolNode;

    // Expose state as a readable string in the inspector
    [Header("Runtime Info (Read Only)")]
    [SerializeField] private string activeBehaviour = "—";
    [SerializeField] private string lastTickResult = "—";

    private void Start()
    {
        locomotion = GetComponent<EnemyLocomotionTask5>();

        // Create a lightweight transform to act as the patrol waypoint target
        waypointHelper = new GameObject($"{gameObject.name}_PatrolWaypoint").transform;
        waypointHelper.position = transform.position;

        BuildTree();
    }

    private void BuildTree()
    {
        LayerMask wallLayer = locomotion.GetWallLayer();

        // Condition nodes
        var isPlayerInChaseRange = new CheckDistanceToPlayer(transform, player, chaseRange);
        var isPlayerInFleeRange = new CheckDistanceToPlayer(transform, player, fleeRange);
        var canSeePlayer = new CheckLineOfSight(transform, player, wallLayer);
        var canSeePlayerForFlee = new CheckLineOfSight(transform, player, wallLayer);

        // Track LOS for gizmo display (used by chase branch)
        var losCheckChase = new BTAction(() =>
        {
            BTNodeStatus result = canSeePlayer.Tick();
            hasLineOfSight = result == BTNodeStatus.Success;
            return result;
        });

        // Separate LOS check for flee (also updates gizmo flag)
        var losCheckFlee = new BTAction(() =>
        {
            BTNodeStatus result = canSeePlayerForFlee.Tick();
            hasLineOfSight = result == BTNodeStatus.Success;
            return result;
        });

        // ?? FLEE: just run away, no last-known recording ??
        var flee = new BTAction(() =>
        {
            currentState = EnemyState.Flee;
            locomotion.SetTarget(player);
            locomotion.SetFlee(true);
            return BTNodeStatus.Running;
        });

        // ?? CHASE: record last known position while pursuing ??
        var chase = new BTAction(() =>
        {
            currentState = EnemyState.Chase;
            locomotion.SetTarget(player);
            locomotion.SetFlee(false);
            RecordLastKnownPosition();
            return BTNodeStatus.Running;
        });

        // ?? INVESTIGATE: search the last known location ??
        searchAreaNode = new SearchArea(locomotion, transform, waypointHelper,
                                        searchRadius, searchDuration, usePathfinding: usePathfinding);

        var investigateCheck = new BTAction(() =>
        {
            return hasLastKnownLocation ? BTNodeStatus.Success : BTNodeStatus.Failure;
        });

        var investigateAction = new BTAction(() =>
        {
            currentState = EnemyState.Investigate;
            BTNodeStatus result = searchAreaNode.Tick();

            // Search complete ? clear the last known location, fall to patrol
            if (result == BTNodeStatus.Success)
            {
                hasLastKnownLocation = false;
                return BTNodeStatus.Failure;
            }
            return BTNodeStatus.Running;
        });

        var investigateBranch = new BTSequence(investigateCheck, investigateAction);

        // ?? PATROL ??
        patrolNode = new Patrol(locomotion, transform, waypointHelper, usePathfinding: usePathfinding);

        // ?? TREE ASSEMBLY ??
        // Flee branch: flee range + LOS ? flee (top priority, no memory)
        var fleeBranch = new BTSequence(isPlayerInFleeRange, losCheckFlee, flee);

        // Chase branch: chase range + LOS ? chase (records last known)
        var chaseBranch = new BTSequence(isPlayerInChaseRange, losCheckChase, chase);

        // Root: flee > chase > investigate > patrol
        root = new BTSelector(
            fleeBranch,
            chaseBranch,
            investigateBranch,
            new BTAction(() =>
            {
                currentState = EnemyState.Patrol;
                return patrolNode.Tick();
            })
        );
    }

    private void RecordLastKnownPosition()
    {
        if (player == null) return;
        lastKnownPlayerPosition = player.position;
        hasLastKnownLocation = true;
        searchAreaNode.SetSearchCentre(lastKnownPlayerPosition);
    }

    private void Update()
    {
        if (root != null)
        {
            lastRootStatus = root.Tick();
            activeBehaviour = currentState.ToString();
            lastTickResult = lastRootStatus.ToString();
            previousState = currentState;
        }
    }

    private void OnDestroy()
    {
        if (waypointHelper != null)
            Destroy(waypointHelper.gameObject);
    }

    // ?????????????????????????????????????????????
    //  GIZMOS
    // ?????????????????????????????????????????????

    private void OnDrawGizmos()
    {
        if (showGizmosAlways)
            DrawBTGizmos();
    }

    private void OnDrawGizmosSelected()
    {
        if (showGizmos)
            DrawBTGizmos();
    }

    private void DrawBTGizmos()
    {
        Vector3 pos = transform.position;

        // ?? Chase range (yellow wire circle) ??
        Gizmos.color = new Color(1f, 0.92f, 0.016f, 0.6f);
        DrawWireCircle(pos, chaseRange, 48);

        // ?? Flee range (red wire circle) ??
        Gizmos.color = new Color(1f, 0.25f, 0.2f, 0.8f);
        DrawWireCircle(pos, fleeRange, 32);

        // ?? State-dependent visuals ??
        if (!Application.isPlaying) return;

        // ?? Line-of-sight indicator ??
        if (player != null)
        {
            float dist = Vector2.Distance(pos, player.position);
            if (dist <= chaseRange)
            {
                // Green = clear LOS, Magenta = blocked
                Gizmos.color = hasLineOfSight
                    ? new Color(0f, 1f, 0.3f, 0.4f)
                    : new Color(1f, 0f, 1f, 0.4f);
                DrawDashedLine(pos, player.position, 0.25f);
            }
        }

        // ?? Last known position marker ??
        if (hasLastKnownLocation)
        {
            Gizmos.color = new Color(1f, 0.85f, 0f, 0.7f);
            Gizmos.DrawWireSphere(lastKnownPlayerPosition, 0.35f);
            float s = 0.2f;
            Gizmos.DrawLine(lastKnownPlayerPosition + Vector3.left * s, lastKnownPlayerPosition + Vector3.right * s);
            Gizmos.DrawLine(lastKnownPlayerPosition + Vector3.down * s, lastKnownPlayerPosition + Vector3.up * s);

            if (currentState == EnemyState.Investigate)
            {
                Gizmos.color = new Color(1f, 0.85f, 0f, 0.25f);
                DrawWireCircle(lastKnownPlayerPosition, searchRadius, 32);
            }
        }

        switch (currentState)
        {
            case EnemyState.Chase:
                if (player != null)
                {
                    Gizmos.color = new Color(1f, 0.6f, 0f, 0.9f);
                    Gizmos.DrawLine(pos, player.position);
                    Gizmos.DrawWireSphere(player.position, 0.3f);
                }
                Gizmos.color = new Color(1f, 0.6f, 0f, 0.35f);
                Gizmos.DrawSphere(pos + Vector3.up * 0.6f, 0.15f);
                break;

            case EnemyState.Flee:
                if (player != null)
                {
                    Gizmos.color = new Color(1f, 0.15f, 0.1f, 0.9f);
                    DrawDashedLine(pos, player.position, 0.4f);
                    Gizmos.DrawWireSphere(player.position, 0.3f);
                }
                Gizmos.color = new Color(1f, 0.15f, 0.1f, 0.35f);
                Gizmos.DrawSphere(pos + Vector3.up * 0.6f, 0.15f);
                break;

            case EnemyState.Investigate:
                DrawPathGizmo(searchAreaNode?.PathFollower, new Color(1f, 0.85f, 0f, 0.7f));
                Gizmos.color = new Color(1f, 0.85f, 0f, 0.35f);
                Gizmos.DrawSphere(pos + Vector3.up * 0.6f, 0.15f);
                break;

            case EnemyState.Patrol:
                DrawPathGizmo(patrolNode?.PathFollower, new Color(0f, 0.9f, 1f, 0.7f));
                Gizmos.color = new Color(0f, 0.9f, 1f, 0.35f);
                Gizmos.DrawSphere(pos + Vector3.up * 0.6f, 0.15f);
                break;
        }

#if UNITY_EDITOR
        GUIStyle style = new GUIStyle();
        style.normal.textColor = GetStateColor();
        style.fontStyle = FontStyle.Bold;
        style.fontSize = 11;
        style.alignment = TextAnchor.MiddleCenter;
        UnityEditor.Handles.Label(pos + Vector3.up * 1.0f, currentState.ToString(), style);
#endif
    }

    /// <summary>
    /// Draws the A* path as connected line segments with node markers.
    /// </summary>
    private void DrawPathGizmo(PathFollower follower, Color pathColor)
    {
        if (follower == null || follower.CurrentPath == null || follower.CurrentPath.Count == 0)
            return;

        GridManager gm = GridManager.Instance;
        if (gm == null) return;

        var path = follower.CurrentPath;
        int currentIdx = follower.CurrentIndex;

        // Draw line from enemy to current target node
        if (currentIdx < path.Count)
        {
            Gizmos.color = pathColor;
            Vector3 currentNodeWorld = gm.GridToWorld(path[currentIdx].x, path[currentIdx].y);
            Gizmos.DrawLine(transform.position, currentNodeWorld);
        }

        // Draw remaining path segments
        Color dimColor = new Color(pathColor.r, pathColor.g, pathColor.b, pathColor.a * 0.5f);
        for (int i = Mathf.Max(currentIdx, 0); i < path.Count - 1; i++)
        {
            Gizmos.color = dimColor;
            Vector3 from = gm.GridToWorld(path[i].x, path[i].y);
            Vector3 to = gm.GridToWorld(path[i + 1].x, path[i + 1].y);
            Gizmos.DrawLine(from, to);
        }

        // Draw small spheres at each remaining path node
        for (int i = currentIdx; i < path.Count; i++)
        {
            Vector3 nodeWorld = gm.GridToWorld(path[i].x, path[i].y);
            bool isFinal = (i == path.Count - 1);
            Gizmos.color = isFinal ? pathColor : dimColor;
            Gizmos.DrawWireSphere(nodeWorld, isFinal ? 0.25f : 0.12f);
        }
    }

    private Color GetStateColor()
    {
        switch (currentState)
        {
            case EnemyState.Chase:       return new Color(1f, 0.6f, 0f);
            case EnemyState.Flee:        return new Color(1f, 0.15f, 0.1f);
            case EnemyState.Investigate: return new Color(1f, 0.85f, 0f);
            case EnemyState.Patrol:      return new Color(0f, 0.9f, 1f);
            default: return Color.white;
        }
    }

    private static void DrawWireCircle(Vector3 center, float radius, int segments)
    {
        float step = 2f * Mathf.PI / segments;
        Vector3 prev = center + new Vector3(radius, 0f, 0f);
        for (int i = 1; i <= segments; i++)
        {
            float angle = i * step;
            Vector3 next = center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }

    private static void DrawDashedLine(Vector3 from, Vector3 to, float dashLength)
    {
        Vector3 dir = to - from;
        float length = dir.magnitude;
        dir /= length;
        bool draw = true;
        float covered = 0f;
        while (covered < length)
        {
            float segLen = Mathf.Min(dashLength, length - covered);
            if (draw)
                Gizmos.DrawLine(from + dir * covered, from + dir * (covered + segLen));
            covered += segLen;
            draw = !draw;
        }
    }
}
