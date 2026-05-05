/// <summary>
/// Runs children in order. Fails immediately if any child fails.
/// Returns Running if a child is still running.
/// Returns Success only if ALL children succeed.
/// 
/// This is a "reactive" sequence: it always re-evaluates from the first
/// child so that conditions are rechecked every tick.
/// </summary>
public class BTSequence : BTComposite
{
    public BTSequence(params BTNode[] nodes) : base(nodes) { }

    public override BTNodeStatus Tick()
    {
        for (int i = 0; i < children.Count; i++)
        {
            BTNodeStatus status = children[i].Tick();

            switch (status)
            {
                case BTNodeStatus.Running:
                    return BTNodeStatus.Running;

                case BTNodeStatus.Failure:
                    return BTNodeStatus.Failure;
            }
        }

        return BTNodeStatus.Success;
    }
}
