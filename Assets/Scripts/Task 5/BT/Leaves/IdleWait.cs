using UnityEngine;

/// <summary>
/// Leaf node that waits for a specified duration.
/// Returns Running while waiting, Success when the timer expires.
/// Resets automatically for the next evaluation cycle.
/// </summary>
public class IdleWait : BTNode
{
    private float duration;
    private float timer;

    public IdleWait(float duration)
    {
        this.duration = duration;
        this.timer = 0f;
    }

    public override BTNodeStatus Tick()
    {
        timer += Time.deltaTime;
        if (timer >= duration)
        {
            timer = 0f;
            return BTNodeStatus.Success;
        }
        return BTNodeStatus.Running;
    }
}
