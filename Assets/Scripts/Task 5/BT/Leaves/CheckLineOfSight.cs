using UnityEngine;

/// <summary>
/// Leaf node that checks whether there is an unobstructed line of sight
/// between the enemy and the player.
/// 
/// Casts a ray from enemy to player. If a wall (wallLayer) blocks the
/// path before reaching the player, returns Failure. Otherwise returns Success.
/// </summary>
public class CheckLineOfSight : BTNode
{
    private Transform enemy;
    private Transform player;
    private LayerMask wallLayer;

    public CheckLineOfSight(Transform enemy, Transform player, LayerMask wallLayer)
    {
        this.enemy = enemy;
        this.player = player;
        this.wallLayer = wallLayer;
    }

    public override BTNodeStatus Tick()
    {
        if (player == null || enemy == null)
            return BTNodeStatus.Failure;

        Vector2 origin = enemy.position;
        Vector2 target = player.position;
        Vector2 direction = target - origin;
        float distance = direction.magnitude;

        RaycastHit2D hit = Physics2D.Raycast(origin, direction.normalized, distance, wallLayer);

        // If the ray hit a wall before reaching the player, there is no line of sight
        if (hit.collider != null)
            return BTNodeStatus.Failure;

        return BTNodeStatus.Success;
    }
}
