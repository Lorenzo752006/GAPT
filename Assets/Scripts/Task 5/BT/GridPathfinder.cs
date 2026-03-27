using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A* pathfinding on the GridManager grid.
/// Finds the shortest walkable path between two grid cells.
/// </summary>
public static class GridPathfinder
{
    private struct PathNode
    {
        public Vector2Int Position;
        public Vector2Int Parent;
        public int G; // cost from start
        public int H; // heuristic to end
        public int F; // G + H
    }

    private static readonly Vector2Int[] Directions =
    {
        Vector2Int.up,
        Vector2Int.down,
        Vector2Int.left,
        Vector2Int.right,
        new Vector2Int(1, 1),
        new Vector2Int(1, -1),
        new Vector2Int(-1, 1),
        new Vector2Int(-1, -1)
    };

    /// <summary>
    /// Finds a path from start to end on the grid.
    /// Returns a list of grid positions from start to end (inclusive),
    /// or an empty list if no path exists.
    /// </summary>
    public static List<Vector2Int> FindPath(Vector2Int start, Vector2Int end)
    {
        GridManager gm = GridManager.Instance;
        if (gm == null)
            return new List<Vector2Int>();

        // If the end isn't walkable, no path
        if (!gm.IsWalkable(end.x, end.y))
            return new List<Vector2Int>();

        // If start equals end, trivial
        if (start == end)
            return new List<Vector2Int> { start };

        var openSet = new List<PathNode>();
        var closedSet = new HashSet<Vector2Int>();
        var allNodes = new Dictionary<Vector2Int, PathNode>();

        var startNode = new PathNode
        {
            Position = start,
            Parent = start,
            G = 0,
            H = Heuristic(start, end),
            F = Heuristic(start, end)
        };
        openSet.Add(startNode);
        allNodes[start] = startNode;

        while (openSet.Count > 0)
        {
            // Find node with lowest F
            int bestIdx = 0;
            for (int i = 1; i < openSet.Count; i++)
            {
                if (openSet[i].F < openSet[bestIdx].F ||
                    (openSet[i].F == openSet[bestIdx].F && openSet[i].H < openSet[bestIdx].H))
                {
                    bestIdx = i;
                }
            }

            PathNode current = openSet[bestIdx];
            openSet.RemoveAt(bestIdx);

            if (current.Position == end)
                return ReconstructPath(allNodes, end);

            closedSet.Add(current.Position);

            for (int d = 0; d < Directions.Length; d++)
            {
                Vector2Int neighbor = current.Position + Directions[d];

                if (closedSet.Contains(neighbor))
                    continue;

                if (!gm.IsWalkable(neighbor.x, neighbor.y))
                    continue;

                // For diagonal moves, ensure both adjacent cardinal cells are walkable
                // to prevent cutting through wall corners
                if (Directions[d].x != 0 && Directions[d].y != 0)
                {
                    if (!gm.IsWalkable(current.Position.x + Directions[d].x, current.Position.y) ||
                        !gm.IsWalkable(current.Position.x, current.Position.y + Directions[d].y))
                        continue;
                }

                // Diagonal costs 14 (approx sqrt(2)*10), cardinal costs 10
                bool isDiagonal = Directions[d].x != 0 && Directions[d].y != 0;
                int moveCost = isDiagonal ? 14 : 10;
                int tentativeG = current.G + moveCost;

                if (allNodes.TryGetValue(neighbor, out PathNode existing))
                {
                    if (tentativeG >= existing.G)
                        continue;

                    // Remove old entry from open set
                    for (int i = openSet.Count - 1; i >= 0; i--)
                    {
                        if (openSet[i].Position == neighbor)
                        {
                            openSet.RemoveAt(i);
                            break;
                        }
                    }
                }

                var neighborNode = new PathNode
                {
                    Position = neighbor,
                    Parent = current.Position,
                    G = tentativeG,
                    H = Heuristic(neighbor, end),
                    F = tentativeG + Heuristic(neighbor, end)
                };
                allNodes[neighbor] = neighborNode;
                openSet.Add(neighborNode);
            }
        }

        // No path found
        return new List<Vector2Int>();
    }

    private static int Heuristic(Vector2Int a, Vector2Int b)
    {
        // Chebyshev distance (supports diagonal movement)
        int dx = Mathf.Abs(a.x - b.x);
        int dy = Mathf.Abs(a.y - b.y);
        return 10 * (dx + dy) + (14 - 2 * 10) * Mathf.Min(dx, dy);
    }

    private static List<Vector2Int> ReconstructPath(Dictionary<Vector2Int, PathNode> allNodes, Vector2Int end)
    {
        var path = new List<Vector2Int>();
        Vector2Int current = end;

        while (true)
        {
            path.Add(current);
            PathNode node = allNodes[current];
            if (node.Parent == current)
                break;
            current = node.Parent;
        }

        path.Reverse();
        return path;
    }
}
