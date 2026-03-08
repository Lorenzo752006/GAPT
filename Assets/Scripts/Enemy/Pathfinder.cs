using System.Collections.Generic;
using UnityEngine;

// Pathfinding system supporting BFS (basic) and A* (complex).
// Designed to work with steering-based enemies.

public enum PathMode
{
    BFS,
    AStar
}

public class Pathfinder : MonoBehaviour
{
    [Header("Pathfinding Mode")]
    public PathMode currentMode = PathMode.AStar;

    // Public function used by enemies to request a path
    public List<Vector2Int> FindPath(Vector2Int start, Vector2Int goal)
    {
        if (currentMode == PathMode.BFS)
            return FindPathBFS(start, goal);
        else
            return FindPathAStar(start, goal);
    }

    #region BFS (Basic AI)

    private List<Vector2Int> FindPathBFS(Vector2Int start, Vector2Int goal)
    {
        Queue<PathNode> open = new Queue<PathNode>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        PathNode startNode = new PathNode(start);
        open.Enqueue(startNode);
        visited.Add(start);

        while (open.Count > 0)
        {
            PathNode current = open.Dequeue();

            if (current.position == goal)
                return RetracePath(current);

            foreach (Vector2Int neighbor in GetNeighbors(current.position))
            {
                if (!GridManager.Instance.IsWalkable(neighbor.x, neighbor.y) || visited.Contains(neighbor))
                    continue;

                PathNode neighborNode = new PathNode(neighbor);
                neighborNode.parent = current;

                open.Enqueue(neighborNode);
                visited.Add(neighbor);
            }
        }

        return null; // No path found
    }

    #endregion

    #region A* (Complex AI - Euclidean Distance)

    private List<Vector2Int> FindPathAStar(Vector2Int start, Vector2Int goal)
    {
        List<PathNode> open = new List<PathNode>();
        HashSet<Vector2Int> closed = new HashSet<Vector2Int>();

        PathNode startNode = new PathNode(start);
        startNode.gCost = 0;
        startNode.hCost = Heuristic(start, goal);

        open.Add(startNode);

        while (open.Count > 0)
        {
            // Find node with lowest fCost
            PathNode current = open[0];

            foreach (PathNode node in open)
            {
                if (node.fCost < current.fCost ||
                    (node.fCost == current.fCost && node.hCost < current.hCost))
                {
                    current = node;
                }
            }

            open.Remove(current);
            closed.Add(current.position);

            // Goal reached
            if (current.position == goal)
                return RetracePath(current);

            foreach (Vector2Int neighborPos in GetNeighbors(current.position))
            {
                if (!GridManager.Instance.IsWalkable(neighborPos.x, neighborPos.y) ||
                    closed.Contains(neighborPos))
                    continue;

                // Determine movement type
                bool isDiagonal =
                    neighborPos.x != current.position.x &&
                    neighborPos.y != current.position.y;

                int moveCost = isDiagonal ? 14 : 10;

                int tentativeG = current.gCost + moveCost;

                PathNode neighborNode = open.Find(n => n.position == neighborPos);

                if (neighborNode == null)
                {
                    neighborNode = new PathNode(neighborPos);
                    neighborNode.gCost = tentativeG;
                    neighborNode.hCost = Heuristic(neighborPos, goal);
                    neighborNode.parent = current;

                    open.Add(neighborNode);
                }
                else if (tentativeG < neighborNode.gCost)
                {
                    neighborNode.gCost = tentativeG;
                    neighborNode.parent = current;
                }
            }
        }

        return null; // No path found
    }

    #endregion

    #region Utility Functions

    // Euclidean heuristic (straight-line distance)
    private int Heuristic(Vector2Int a, Vector2Int b)
    {
        return Mathf.RoundToInt(Vector2Int.Distance(a, b) * 10);
    }

    // Returns valid neighbours including diagonals
    private List<Vector2Int> GetNeighbors(Vector2Int pos)
    {
        List<Vector2Int> neighbors = new List<Vector2Int>();

        Vector2Int[] dirs =
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right,
            new Vector2Int(1,1),
            new Vector2Int(-1,1),
            new Vector2Int(1,-1),
            new Vector2Int(-1,-1)
        };

        foreach (Vector2Int dir in dirs)
        {
            Vector2Int neighbor = pos + dir;

            if (GridManager.Instance.IsInBounds(neighbor.x, neighbor.y))
                neighbors.Add(neighbor);
        }

        return neighbors;
    }

    // Converts node chain into a list of grid coordinates
    private List<Vector2Int> RetracePath(PathNode endNode)
    {
        List<Vector2Int> path = new List<Vector2Int>();

        PathNode current = endNode;

        while (current != null)
        {
            path.Add(current.position);
            current = current.parent;
        }

        path.Reverse();

        return path;
    }

    #endregion
}