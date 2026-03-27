/// <summary>
/// Decorator that inverts the result of its child.
/// Success becomes Failure, Failure becomes Success, Running stays Running.
/// </summary>
public class BTInverter : BTNode
{
    private BTNode child;

    public BTInverter(BTNode child)
    {
        this.child = child;
    }

    public override BTNodeStatus Tick()
    {
        BTNodeStatus status = child.Tick();

        switch (status)
        {
            case BTNodeStatus.Success:
                return BTNodeStatus.Failure;
            case BTNodeStatus.Failure:
                return BTNodeStatus.Success;
            default:
                return BTNodeStatus.Running;
        }
    }
}
