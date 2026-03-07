using UnityEngine;

/// <summary>
/// Leaf node that makes the enemy patrol between random walkable grid points.
/// Optionally uses A* pathfinding to navigate around walls.
/// Returns Running while patrolling.
/// </summary>
public class Patrol : BTNode
{
    private EnemyLocomotionTask5 locomotion;
    private Transform enemy;
    private Transform waypointTarget;
    private PathFollower pathFollower;
    private float arrivalThreshold;
    private float waypointTimer;
    private float waypointTimeout;
    private bool usePathfinding;

    public Patrol(EnemyLocomotionTask5 locomotion, Transform enemy, Transform waypointTarget,
                  float arrivalThreshold = 0.5f, float waypointTimeout = 8f,
                  bool usePathfinding = true)
    {
        this.locomotion = locomotion;
        this.enemy = enemy;
        this.waypointTarget = waypointTarget;
        this.arrivalThreshold = arrivalThreshold;
        this.waypointTimeout = waypointTimeout;
        this.usePathfinding = usePathfinding;
        this.pathFollower = new PathFollower(enemy, waypointTarget, arrivalThreshold);
        PickNewDestination();
    }

    /// <summary>
    /// The PathFollower used by this node, exposed for gizmo drawing.
    /// Returns null when pathfinding is disabled.
    /// </summary>
    public PathFollower PathFollower => usePathfinding ? pathFollower : null;

    public override BTNodeStatus Tick()
    {
        if (locomotion == null || enemy == null)
            return BTNodeStatus.Failure;

        locomotion.SetTarget(waypointTarget);
        locomotion.SetFlee(false);

        waypointTimer += Time.deltaTime;

        if (usePathfinding)
        {
            bool stillFollowing = pathFollower.Tick();
            if (!stillFollowing || waypointTimer >= waypointTimeout)
            {
                PickNewDestination();
            }
        }
        else
        {
            float dist = Vector2.Distance(enemy.position, waypointTarget.position);
            if (dist <= arrivalThreshold || waypointTimer >= waypointTimeout)
            {
                PickNewDestination();
            }
        }

        return BTNodeStatus.Running;
    }

    private void PickNewDestination()
    {
        waypointTimer = 0f;
        GridManager gm = GridManager.Instance;
        if (gm == null) return;

        for (int attempt = 0; attempt < 30; attempt++)
        {
            int x = Random.Range(1, gm.Width - 1);
            int y = Random.Range(1, gm.Height - 1);
            if (gm.IsWalkable(x, y))
            {
                if (usePathfinding)
                {
                    if (pathFollower.SetDestination(new Vector2Int(x, y)))
                        return;
                }
                else
                {
                    waypointTarget.position = gm.GridToWorld(x, y);
                    return;
                }
            }
        }
    }
}
