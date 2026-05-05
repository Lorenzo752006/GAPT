/// <summary>
/// Abstract base class for every node in the Behaviour Tree.
/// </summary>
public abstract class BTNode
{
    /// <summary>
    /// Evaluate this node and return its status.
    /// </summary>
    public abstract BTNodeStatus Tick();
}
