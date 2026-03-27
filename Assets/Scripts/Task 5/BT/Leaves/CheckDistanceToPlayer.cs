using UnityEngine;

/// <summary>
/// Leaf node that checks whether the player is within a given distance.
/// Returns Success if the player is within range, Failure otherwise.
/// </summary>
public class CheckDistanceToPlayer : BTNode
{
    private Transform enemy;
    private Transform player;
    private float range;

    public CheckDistanceToPlayer(Transform enemy, Transform player, float range)
    {
        this.enemy = enemy;
        this.player = player;
        this.range = range;
    }

    public override BTNodeStatus Tick()
    {
        if (player == null || enemy == null)
            return BTNodeStatus.Failure;

        float distance = Vector2.Distance(enemy.position, player.position);
        return distance <= range ? BTNodeStatus.Success : BTNodeStatus.Failure;
    }
}
