using System.Collections.Generic;
using UnityEngine;
using Task9; 

namespace Task9
{
    public class PathfinderTask9 : MonoBehaviour
    {
        public PathMode currentMode = PathMode.AStar;

        public List<Vector2Int> FindPath(Vector2Int start, Vector2Int goal)
        {
            if (GridManagerTask9.Instance == null) return null;

            if (currentMode == PathMode.BFS)
                return FindPathBFS(start, goal);
            else
                return FindPathAStar(start, goal);
        }




        #region BFS & AStar Logic
        private List<Vector2Int> FindPathBFS(Vector2Int start, Vector2Int goal)
        {
            Queue<PathNode> open = new Queue<PathNode>();
            HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
            open.Enqueue(new PathNode(start));
            visited.Add(start);

            while (open.Count > 0)
            {
                PathNode current = open.Dequeue();
                if (current.position == goal) return RetracePath(current);

                foreach (Vector2Int neighbor in GetNeighbors(current.position))
                {
                    // Check Task 9 Grid
                    if (!GridManagerTask9.Instance.IsWalkable(neighbor.x, neighbor.y) || visited.Contains(neighbor))
                        continue;

                    open.Enqueue(new PathNode(neighbor) { parent = current });
                    visited.Add(neighbor);
                }
            }
            return null;
        }


        
        private List<Vector2Int> FindPathAStar(Vector2Int start, Vector2Int goal)
        {
            List<PathNode> open = new List<PathNode>();
            HashSet<Vector2Int> closed = new HashSet<Vector2Int>();
            open.Add(new PathNode(start) { gCost = 0, hCost = Heuristic(start, goal) });

            while (open.Count > 0)
            {
                PathNode current = open[0];
                for (int i = 1; i < open.Count; i++)
                {
                    if (open[i].fCost < current.fCost || (open[i].fCost == current.fCost && open[i].hCost < current.hCost))
                        current = open[i];
                }

                open.Remove(current);
                closed.Add(current.position);

                if (current.position == goal) return RetracePath(current);

                foreach (Vector2Int neighborPos in GetNeighbors(current.position))
                {
                    if (!GridManagerTask9.Instance.IsWalkable(neighborPos.x, neighborPos.y) || closed.Contains(neighborPos))
                        continue;

                    bool isDiagonal = neighborPos.x != current.position.x && neighborPos.y != current.position.y;
                    int moveCost = isDiagonal ? 14 : 10;
                    int tentativeG = current.gCost + moveCost;

                    PathNode neighborNode = open.Find(n => n.position == neighborPos);
                    if (neighborNode == null)
                        open.Add(new PathNode(neighborPos) { gCost = tentativeG, hCost = Heuristic(neighborPos, goal), parent = current });
                    else if (tentativeG < neighborNode.gCost)
                    {
                        neighborNode.gCost = tentativeG;
                        neighborNode.parent = current;
                    }
                }
            }
            return null;
        }
        #endregion

        // Heuristic function for A* (Euclidean distance)
        private int Heuristic(Vector2Int a, Vector2Int b) => Mathf.RoundToInt(Vector2Int.Distance(a, b) * 10);

        private List<Vector2Int> GetNeighbors(Vector2Int pos)
        {
            List<Vector2Int> neighbors = new List<Vector2Int>();
            
            // Check Cardinal Directions (Up, Down, Left, Right) - Always Safe
            Vector2Int[] cardinals = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            foreach (var dir in cardinals)
            {
                Vector2Int neighbor = pos + dir;
                if (GridManagerTask9.Instance.IsWalkable(neighbor.x, neighbor.y))
                {
                    neighbors.Add(neighbor);
                }
            }

            // Check Diagonal Directions - Only add if the "Side" tiles are also walkable
            // This prevents cutting through a crack between two diagonal walls
            CheckDiagonal(pos, new Vector2Int(1, 1), neighbors);   // Top-Right
            CheckDiagonal(pos, new Vector2Int(-1, 1), neighbors);  // Top-Left
            CheckDiagonal(pos, new Vector2Int(1, -1), neighbors);  // Bottom-Right
            CheckDiagonal(pos, new Vector2Int(-1, -1), neighbors); // Bottom-Left

            return neighbors;
        }

        private void CheckDiagonal(Vector2Int current, Vector2Int direction, List<Vector2Int> neighbors)
        {
            Vector2Int target = current + direction;

            // The two tiles we must pass "between" to get to the diagonal
            Vector2Int side1 = new Vector2Int(current.x + direction.x, current.y);
            Vector2Int side2 = new Vector2Int(current.x, current.y + direction.y);

            // Only allow diagonal move if the destination is floor AND BOTH sides are floor
            if (GridManagerTask9.Instance.IsWalkable(target.x, target.y) &&
                GridManagerTask9.Instance.IsWalkable(side1.x, side1.y) &&
                GridManagerTask9.Instance.IsWalkable(side2.x, side2.y))
            {
                neighbors.Add(target);
            }
        }

        private List<Vector2Int> RetracePath(PathNode endNode)
        {
            List<Vector2Int> path = new List<Vector2Int>();
            PathNode current = endNode;
            while (current != null) { path.Add(current.position); current = current.parent; }
            path.Reverse();
            return path;
        }
    }
}