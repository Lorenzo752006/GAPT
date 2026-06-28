using System.Collections.Generic;
using UnityEngine;
using Task9;

namespace Task9
{
    [RequireComponent(typeof(EnemyLocomotion))]
    public class EnemyPathAgentTask9 : MonoBehaviour
    {
        [Header("References")]
        public Transform player;
        public PathfinderTask9 pathfinder; 
        public Transform steeringTarget;

        [Header("Settings")]
        public float waypointThreshold = 0.5f;
        public float pathRecalcInterval = 0.5f;

        private List<Vector2Int> currentPath;
        private int pathIndex = 0;
        private float pathRecalcTimer = 0f;

        void Update()
        {
            // Safety check: wait for GridManager to be initialized by the Generator
            if (player == null || pathfinder == null || steeringTarget == null || GridManagerTask9.Instance == null)
                return;

            pathRecalcTimer -= Time.deltaTime;
            if (pathRecalcTimer <= 0f)
            {
                RecalculatePath();
                pathRecalcTimer = pathRecalcInterval;
            }

            FollowPath();
        }

        private void RecalculatePath()
        {
            Vector2Int start = GridManagerTask9.Instance.WorldToGrid(transform.position);
            Vector2Int goal = GridManagerTask9.Instance.WorldToGrid(player.position);

            currentPath = pathfinder.FindPath(start, goal);
            pathIndex = 0;
        }

        private void FollowPath()
        {
            if (currentPath == null || pathIndex >= currentPath.Count)
                return;

            Vector2Int waypointGrid = currentPath[pathIndex];
            Vector3 waypointWorld = GridManagerTask9.Instance.GridToWorld(waypointGrid.x, waypointGrid.y);

            steeringTarget.position = waypointWorld;

            if (Vector3.Distance(transform.position, waypointWorld) < waypointThreshold)
            {
                pathIndex++;
            }
        }

        private void OnDrawGizmos()
        {
            if (currentPath == null || GridManagerTask9.Instance == null) return;

            Gizmos.color = Color.magenta; // Magenta for Task 9 distinction
            foreach (Vector2Int node in currentPath)
            {
                Vector3 pos = GridManagerTask9.Instance.GridToWorld(node.x, node.y);
                Gizmos.DrawSphere(pos, 0.1f);
            }
        }
    }
}