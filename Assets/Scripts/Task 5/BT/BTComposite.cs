using System.Collections.Generic;

/// <summary>
/// Base class for composite nodes that hold an ordered list of children.
/// </summary>
public abstract class BTComposite : BTNode
{
    protected List<BTNode> children = new List<BTNode>();

    public BTComposite(params BTNode[] nodes)
    {
        children.AddRange(nodes);
    }

    public void AddChild(BTNode child)
    {
        children.Add(child);
    }
}
