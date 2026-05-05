using UnityEngine;

/// <summary>
/// Leaf node that patrols around a specific area (e.g. last known player position).
/// First navigates directly to the search centre (last known position), then
/// searches randomly within the radius. Optionally uses A* pathfinding.
/// Returns Running while searching, Success when the search duration expires.
/// </summary>
public class SearchArea : BTNode
{
    private EnemyLocomotionTask5 locomotion;
    private Transform enemy;
    private Transform waypointTarget;
    private PathFollower pathFollower;
    private Vector3 searchCentre;
    private float searchRadius;
    private float arrivalThreshold;
    private float waypointTimer;
    private float waypointTimeout;
    private float searchDuration;
    private float searchTimer;
    private bool needsNewWaypoint;
    private bool usePathfinding;
    private bool headingToCentre;

    public SearchArea(EnemyLocomotionTask5 locomotion, Transform enemy, Transform waypointTarget,
                      float searchRadius = 3f, float searchDuration = 6f,
                      float arrivalThreshold = 0.5f, float waypointTimeout = 6f,
                      bool usePathfinding = true)
    {
        this.locomotion = locomotion;
        this.enemy = enemy;
        this.waypointTarget = waypointTarget;
        this.searchRadius = searchRadius;
        this.searchDuration = searchDuration;
        this.arrivalThreshold = arrivalThreshold;
        this.waypointTimeout = waypointTimeout;
        this.usePathfinding = usePathfinding;
        this.pathFollower = new PathFollower(enemy, waypointTarget, arrivalThreshold);
        this.needsNewWaypoint = true;
        this.headingToCentre = true;
    }

    /// <summary>
    /// The PathFollower used by this node, exposed for gizmo drawing.
    /// Returns null when pathfinding is disabled.
    /// </summary>
    public PathFollower PathFollower => usePathfinding ? pathFollower : null;

    /// <summary>
    /// Sets a new centre point to search around and resets all state.
    /// The enemy will navigate to this position first before searching.
    /// </summary>
    public void SetSearchCentre(Vector3 centre)
    {
        searchCentre = centre;
        searchTimer = 0f;
        waypointTimer = 0f;
        needsNewWaypoint = true;
        headingToCentre = true;
    }

    /// <summary>
    /// Returns the current search centre.
    /// </summary>
    public Vector3 GetSearchCentre()
    {
        return searchCentre;
    }

    /// <summary>
    /// Returns the configured search radius.
    /// </summary>
    public float GetSearchRadius()
    {
        return searchRadius;
    }

    public override BTNodeStatus Tick()
    {
        if (locomotion == null || enemy == null)
            return BTNodeStatus.Failure;

        locomotion.SetTarget(waypointTarget);
        locomotion.SetFlee(false);

        // ?? PHASE 1: Head directly to last known position ??
        if (headingToCentre)
        {
            if (needsNewWaypoint)
            {
                waypointTimer = 0f;
                NavigateToCentre();
                needsNewWaypoint = false;
            }

            waypointTimer += Time.deltaTime;
            bool arrived = false;

            if (usePathfinding)
            {
                bool stillFollowing = pathFollower.Tick();
                arrived = !stillFollowing;
            }
            else
            {
                float dist = Vector2.Distance(enemy.position, searchCentre);
                arrived = dist <= arrivalThreshold;
            }

            if (arrived || waypointTimer >= waypointTimeout)
            {
                // Arrived at last known position — start searching
                headingToCentre = false;
                searchTimer = 0f;
                waypointTimer = 0f;
                needsNewWaypoint = true;
            }

            return BTNodeStatus.Running;
        }

        // ?? PHASE 2: Search around the area ??
        searchTimer += Time.deltaTime;
        if (searchTimer >= searchDuration)
        {
            searchTimer = 0f;
            needsNewWaypoint = true;
            headingToCentre = true;
            pathFollower.Clear();
            return BTNodeStatus.Success;
        }

        if (needsNewWaypoint)
        {
            PickWaypointNearCentre();
            needsNewWaypoint = false;
        }

        waypointTimer += Time.deltaTime;

        if (usePathfinding)
        {
            bool stillFollowing = pathFollower.Tick();
            if (!stillFollowing || waypointTimer >= waypointTimeout)
            {
                PickWaypointNearCentre();
            }
        }
        else
        {
            float dist = Vector2.Distance(enemy.position, waypointTarget.position);
            if (dist <= arrivalThreshold || waypointTimer >= waypointTimeout)
            {
                PickWaypointNearCentre();
            }
        }

        return BTNodeStatus.Running;
    }

    private void NavigateToCentre()
    {
        if (usePathfinding)
            pathFollower.SetDestinationWorld(searchCentre);
        else
            waypointTarget.position = searchCentre;
    }

    private void PickWaypointNearCentre()
    {
        waypointTimer = 0f;
        GridManager gm = GridManager.Instance;
        if (gm == null) return;

        Vector2Int centreGrid = gm.WorldToGrid(searchCentre);
        int cellRadius = Mathf.CeilToInt(searchRadius / gm.CellSize);

        for (int attempt = 0; attempt < 30; attempt++)
        {
            int x = centreGrid.x + Random.Range(-cellRadius, cellRadius + 1);
            int y = centreGrid.y + Random.Range(-cellRadius, cellRadius + 1);

            if (gm.IsWalkable(x, y))
            {
                Vector3 candidate = gm.GridToWorld(x, y);
                if (Vector2.Distance(candidate, searchCentre) <= searchRadius)
                {
                    if (usePathfinding)
                    {
                        if (pathFollower.SetDestination(new Vector2Int(x, y)))
                            return;
                    }
                    else
                    {
                        waypointTarget.position = candidate;
                        return;
                    }
                }
            }
        }

        // Fallback: go to the centre itself
        NavigateToCentre();
    }
}
