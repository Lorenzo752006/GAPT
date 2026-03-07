using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages following an A* path node-by-node.
/// Computes a path via GridPathfinder and advances through it,
/// setting the waypoint target to the current path node.
/// </summary>
public class PathFollower
{
    private List<Vector2Int> path = new List<Vector2Int>();
    private int currentIndex;
    private Transform enemy;
    private Transform waypointTarget;
    private float nodeArrivalThreshold;

    /// <summary>
    /// The final destination of the current path (last node), in grid coords.
    /// </summary>
    public Vector2Int Destination { get; private set; }

    /// <summary>
    /// True if we have a valid path with nodes remaining.
    /// </summary>
    public bool HasPath => path.Count > 0 && currentIndex < path.Count;

    /// <summary>
    /// True if the enemy has reached the final node of the path.
    /// </summary>
    public bool IsComplete => path.Count > 0 && currentIndex >= path.Count;

    /// <summary>
    /// The current path for gizmo drawing. Read-only.
    /// </summary>
    public List<Vector2Int> CurrentPath => path;

    /// <summary>
    /// The index of the node currently being pursued.
    /// </summary>
    public int CurrentIndex => currentIndex;

    public PathFollower(Transform enemy, Transform waypointTarget, float nodeArrivalThreshold = 0.5f)
    {
        this.enemy = enemy;
        this.waypointTarget = waypointTarget;
        this.nodeArrivalThreshold = nodeArrivalThreshold;
    }

    /// <summary>
    /// Computes an A* path from the enemy's current position to the target grid cell.
    /// Returns true if a valid path was found.
    /// </summary>
    public bool SetDestination(Vector2Int targetCell)
    {
        GridManager gm = GridManager.Instance;
        if (gm == null) return false;

        Vector2Int start = gm.WorldToGrid(enemy.position);
        Destination = targetCell;
        path = GridPathfinder.FindPath(start, targetCell);
        currentIndex = 0;

        if (path.Count == 0)
            return false;

        // Skip the first node if it's the cell the enemy is already on
        if (path.Count > 1)
            currentIndex = 1;

        UpdateWaypointTarget();
        return true;
    }

    /// <summary>
    /// Computes an A* path from the enemy's current position to a world position.
    /// Returns true if a valid path was found.
    /// </summary>
    public bool SetDestinationWorld(Vector3 worldPos)
    {
        GridManager gm = GridManager.Instance;
        if (gm == null) return false;

        Vector2Int targetCell = gm.WorldToGrid(worldPos);
        return SetDestination(targetCell);
    }

    /// <summary>
    /// Call each tick. Advances to the next node if the enemy is close enough.
    /// Returns true if the path is still being followed, false if complete or invalid.
    /// </summary>
    public bool Tick()
    {
        if (!HasPath) return false;

        float dist = Vector2.Distance(enemy.position, waypointTarget.position);
        if (dist <= nodeArrivalThreshold)
        {
            currentIndex++;
            if (currentIndex >= path.Count)
                return false; // Arrived at final destination

            UpdateWaypointTarget();
        }

        return true;
    }

    /// <summary>
    /// Clears the current path.
    /// </summary>
    public void Clear()
    {
        path.Clear();
        currentIndex = 0;
    }

    private void UpdateWaypointTarget()
    {
        if (currentIndex < path.Count)
        {
            GridManager gm = GridManager.Instance;
            if (gm != null)
                waypointTarget.position = gm.GridToWorld(path[currentIndex].x, path[currentIndex].y);
        }
    }
}
