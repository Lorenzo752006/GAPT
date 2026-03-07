using UnityEngine;

// This class represents a single node (cell) in the grid used for pathfinding
public class PathNode
{
    public Vector2Int position;
    
    // gCost = actual cost from the start node to this node
    public int gCost;

    // hCost = heuristic cost estimate to reach the goal from this node
    public int hCost;

    // fCost = total cost (gCost + hCost) used to prioritize nodes in A*
    public int fCost => gCost + hCost;

    
    public PathNode parent;

    // Constructor: runs when a new PathNode is created
    // It sets the node's grid position
    public PathNode(Vector2Int pos)
    {
        position = pos;
    }
}