using UnityEngine;

/// <summary>
/// Leaf node that commands the EnemyLocomotion to flee from the player.
/// Always returns Running while the enemy is fleeing.
/// </summary>
public class FleeFromPlayer : BTNode
{
    private EnemyLocomotion locomotion;
    private Transform player;

    public FleeFromPlayer(EnemyLocomotion locomotion, Transform player)
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
