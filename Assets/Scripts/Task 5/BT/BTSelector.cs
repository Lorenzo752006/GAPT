/// <summary>
/// Runs children in order. Succeeds immediately if any child succeeds.
/// Returns Running if a child is still running.
/// Returns Failure only if ALL children fail.
/// 
/// This is a "reactive" selector: it always re-evaluates from the first
/// child so that higher-priority branches can pre-empt lower ones.
/// </summary>
public class BTSelector : BTComposite
{
    public BTSelector(params BTNode[] nodes) : base(nodes) { }

    public override BTNodeStatus Tick()
    {
        for (int i = 0; i < children.Count; i++)
        {
            BTNodeStatus status = children[i].Tick();

            switch (status)
            {
                case BTNodeStatus.Running:
                    return BTNodeStatus.Running;

                case BTNodeStatus.Success:
                    return BTNodeStatus.Success;
            }
        }

        return BTNodeStatus.Failure;
    }
}
