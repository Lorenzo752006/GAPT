using UnityEngine;

/// <summary>
/// Leaf node that commands the EnemyLocomotion to chase the player.
/// Always returns Running while the enemy is pursuing.
/// </summary>
public class MoveTowardsPlayer : BTNode
{
    private EnemyLocomotion locomotion;
    private Transform player;

    public MoveTowardsPlayer(EnemyLocomotion locomotion, Transform player)
    {
        this.locomotion = locomotion;
        this.player = player;
    }

    public override BTNodeStatus Tick()
    {
        if (player == null || locomotion == null)
            return BTNodeStatus.Failure;

        locomotion.SetTarget(player);
        locomotion.SetFlee(false);
        return BTNodeStatus.Running;
    }
}
