using UnityEngine;

/// <summary>
/// Leaf node that commands the EnemyLocomotion to flee from the player.
/// Always returns Running while the enemy is fleeing.
/// </summary>
public class FleeFromPlayer : BTNode
{
    private EnemyLocomotionTask5 locomotion;
    private Transform player;

    public FleeFromPlayer(EnemyLocomotionTask5 locomotion, Transform player)
    {
        this.locomotion = locomotion;
        this.player = player;
    }

    public override BTNodeStatus Tick()
    {
        if (player == null || locomotion == null)
            return BTNodeStatus.Failure;

        locomotion.SetTarget(player);
        locomotion.SetFlee(true);
        return BTNodeStatus.Running;
    }
}
