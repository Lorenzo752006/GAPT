using UnityEngine;

/// <summary>
/// Assembles and ticks the enemy Behaviour Tree.
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

    [Header("Runtime Visuals")]
    [SerializeField] private bool showRuntimeVisuals = true;

    private BTNode root;
    private EnemyLocomotionTask5 locomotion;
    private Transform waypointHelper;
    private EnemyBTVisualizer visualizer;

    private enum EnemyState { Patrol, Chase, Flee, Investigate }
    private EnemyState currentState = EnemyState.Patrol;
    private EnemyState previousState = EnemyState.Patrol;
    private BTNodeStatus lastRootStatus;
    private bool hasLineOfSight;

    private Vector3 lastKnownPlayerPosition;
    private bool hasLastKnownLocation;
    private SearchArea searchAreaNode;
    private Patrol patrolNode;

    [Header("Runtime Info (Read Only)")]
    [SerializeField] private string activeBehaviour = "-";
    [SerializeField] private string lastTickResult = "-";

    public Transform Player => player;
    public float ChaseRange => chaseRange;
    public float FleeRange => fleeRange;
    public float SearchRadius => searchRadius;
    public string CurrentStateName => currentState.ToString();
    public string PreviousStateName => previousState.ToString();
    public string LastTickResult => lastRootStatus.ToString();
    public bool HasLineOfSight => hasLineOfSight;
    public bool HasLastKnownLocation => hasLastKnownLocation;
    public Vector3 LastKnownPlayerPosition => lastKnownPlayerPosition;
    public PathFollower ActivePathFollower
    {
        get
        {
            switch (currentState)
            {
                case EnemyState.Investigate:
                    return searchAreaNode?.PathFollower;
                case EnemyState.Patrol:
                    return patrolNode?.PathFollower;
                default:
                    return null;
            }
        }
    }
    public Vector3? ActiveSearchCentre => hasLastKnownLocation ? lastKnownPlayerPosition : null;
    public Color CurrentStateColor => GetStateColor();
    public bool RuntimeVisualsVisible => showRuntimeVisuals;

    private void Start()
    {
        locomotion = GetComponent<EnemyLocomotionTask5>();

        waypointHelper = new GameObject($"{gameObject.name}_PatrolWaypoint").transform;
        waypointHelper.position = transform.position;

        BuildTree();
        SetupRuntimeVisuals();
    }

    private void BuildTree()
    {
        LayerMask wallLayer = locomotion.GetWallLayer();

        var isPlayerInChaseRange = new CheckDistanceToPlayer(transform, player, chaseRange);
        var isPlayerInFleeRange = new CheckDistanceToPlayer(transform, player, fleeRange);
        var canSeePlayer = new CheckLineOfSight(transform, player, wallLayer);
        var canSeePlayerForFlee = new CheckLineOfSight(transform, player, wallLayer);

        var losCheckChase = new BTAction(() =>
        {
            BTNodeStatus result = canSeePlayer.Tick();
            hasLineOfSight = result == BTNodeStatus.Success;
            return result;
        });

        var losCheckFlee = new BTAction(() =>
        {
            BTNodeStatus result = canSeePlayerForFlee.Tick();
            hasLineOfSight = result == BTNodeStatus.Success;
            return result;
        });

        var flee = new BTAction(() =>
        {
            currentState = EnemyState.Flee;
            locomotion.SetTarget(player);
            locomotion.SetFlee(true);
            return BTNodeStatus.Running;
        });

        var chase = new BTAction(() =>
        {
            currentState = EnemyState.Chase;
            locomotion.SetTarget(player);
            locomotion.SetFlee(false);
            RecordLastKnownPosition();
            return BTNodeStatus.Running;
        });

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

            if (result == BTNodeStatus.Success)
            {
                hasLastKnownLocation = false;
                return BTNodeStatus.Failure;
            }
            return BTNodeStatus.Running;
        });

        var investigateBranch = new BTSequence(investigateCheck, investigateAction);

        patrolNode = new Patrol(locomotion, transform, waypointHelper, usePathfinding: usePathfinding);

        var fleeBranch = new BTSequence(isPlayerInFleeRange, losCheckFlee, flee);
        var chaseBranch = new BTSequence(isPlayerInChaseRange, losCheckChase, chase);

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

    private void SetupRuntimeVisuals()
    {
        visualizer = GetComponent<EnemyBTVisualizer>();
        if (visualizer == null)
            visualizer = gameObject.AddComponent<EnemyBTVisualizer>();

        visualizer.Initialize(this);
        visualizer.SetVisible(showRuntimeVisuals);
    }

    public void ToggleRuntimeVisuals()
    {
        SetRuntimeVisualsVisible(!showRuntimeVisuals);
    }

    public void SetRuntimeVisualsVisible(bool visible)
    {
        showRuntimeVisuals = visible;

        if (visualizer == null)
            SetupRuntimeVisuals();
        else
            visualizer.SetVisible(showRuntimeVisuals);
    }

    public void ShowRuntimeVisuals()
    {
        SetRuntimeVisualsVisible(true);
    }

    public void HideRuntimeVisuals()
    {
        SetRuntimeVisualsVisible(false);
    }

    public void ToggleRuntimeVisualsForAllEnemies()
    {
        bool nextVisible = !showRuntimeVisuals;
        EnemyBT[] enemies = FindObjectsByType<EnemyBT>(FindObjectsSortMode.None);

        for (int i = 0; i < enemies.Length; i++)
        {
            enemies[i].SetRuntimeVisualsVisible(nextVisible);
        }
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
}
