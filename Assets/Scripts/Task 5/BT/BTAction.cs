/// <summary>
/// Generic leaf node that executes a delegate function.
/// Useful for wrapping quick inline actions without creating a dedicated class.
/// </summary>
public class BTAction : BTNode
{
    private System.Func<BTNodeStatus> action;

    public BTAction(System.Func<BTNodeStatus> action)
    {
        this.action = action;
    }

    public override BTNodeStatus Tick()
    {
        return action != null ? action.Invoke() : BTNodeStatus.Failure;
    }
}
