using System.Collections.Generic;
using UnityEngine;

namespace Task9
{
    public class Task9BasicGen : RoomFirstDungeonGenerator
    {
        [Header("Generation Settings")]
        [SerializeField] private Vector2Int genSize = new Vector2Int(50, 50);
        
        [Header("Growth Probabilities")]
        [Tooltip("Increase to create more open areas, decrease for more walls. Value should be between 0 and 1.")]
        [SerializeField] [Range(0, 1f)] private float floorBoost = 0.6f;

        void Start()
        {
            runProceduralGeneration();
        }


        public override void generateDungeon()
        {
            runProceduralGeneration();
        }

        protected override void runProceduralGeneration()
        {
            tileMapVisualizer.Clear();
            
            // Generate Raw Layout
            HashSet<Vector2Int> rawMap = GenerateHardcodedMap();
            
            // Polish with Cellular Automata-style smoothing
            HashSet<Vector2Int> polishedMap = SmoothMap(rawMap);
            polishedMap = SmoothMap(polishedMap);
            
            // Ensure connectivity
            HashSet<Vector2Int> finalMap = KeepOnlyLargestArea(polishedMap);

            // Visualization
            tileMapVisualizer.paintFloorTiles(finalMap);
            WallGenerator.createWalls(finalMap, tileMapVisualizer);

            // Sync Grid BEFORE Spawning
            if (GridManagerTask9.Instance != null)
            {
                GridManagerTask9.Instance.InitializeFromTilemap(finalMap);
            }

            // Spawn Entities at new points
            SpawnEntities(finalMap);
        }

        private HashSet<Vector2Int> GenerateHardcodedMap()
        {
            HashSet<Vector2Int> newFloor = new HashSet<Vector2Int>();
            
            
            for (int x = 0; x < genSize.x; x++)
            {
                for (int y = 0; y < genSize.y; y++)
                {
                    
                    if (Random.value < floorBoost) 
                    {
                        newFloor.Add(new Vector2Int(x, y) + startPosition);
                    }
                }
            }
            return newFloor;
        }

        private HashSet<Vector2Int> SmoothMap(HashSet<Vector2Int> floorPositions)
        {
            HashSet<Vector2Int> smoothedFloor = new HashSet<Vector2Int>();
            for (int x = 0; x < genSize.x; x++)
            {
                for (int y = 0; y < genSize.y; y++)
                {
                    Vector2Int pos = new Vector2Int(x, y) + startPosition;
                    int neighbors = 0;

                    for (int i = -1; i <= 1; i++)
                    {
                        for (int j = -1; j <= 1; j++)
                        {
                            if (i == 0 && j == 0) continue;
                            if (floorPositions.Contains(pos + new Vector2Int(i, j))) neighbors++;
                        }
                    }

                    if (neighbors > 4) smoothedFloor.Add(pos);
                    else if (floorPositions.Contains(pos) && neighbors >= 2) smoothedFloor.Add(pos);
                }
            }
            return smoothedFloor;
        }

        private HashSet<Vector2Int> KeepOnlyLargestArea(HashSet<Vector2Int> floorPositions)
        {
            if (floorPositions.Count == 0) return floorPositions;
            List<HashSet<Vector2Int>> areas = new List<HashSet<Vector2Int>>();
            HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

            foreach (var pos in floorPositions)
            {
                if (visited.Contains(pos)) continue;
                HashSet<Vector2Int> area = new HashSet<Vector2Int>();
                Stack<Vector2Int> stack = new Stack<Vector2Int>();
                stack.Push(pos);
                while (stack.Count > 0)
                {
                    Vector2Int curr = stack.Pop();
                    if (visited.Contains(curr) || !floorPositions.Contains(curr)) continue;
                    visited.Add(curr); area.Add(curr);
                    stack.Push(curr + Vector2Int.up); stack.Push(curr + Vector2Int.down);
                    stack.Push(curr + Vector2Int.left); stack.Push(curr + Vector2Int.right);
                }
                areas.Add(area);
            }
            areas.Sort((a, b) => b.Count.CompareTo(a.Count));
            return areas.Count > 0 ? areas[0] : new HashSet<Vector2Int>();
        }

        private void SpawnEntities(HashSet<Vector2Int> finalMap)
        {
            List<Vector2Int> floorList = new List<Vector2Int>(finalMap);
            if (floorList.Count < 2) return;

            // Reset Player Position
            Task9PlayerController player = FindAnyObjectByType<Task9PlayerController>();
            Vector2Int playerSpawnGrid = floorList[Random.Range(0, floorList.Count / 4)]; // Choose from first quarter for variety
            
            if (player != null) 
            {
                player.SetPosition(playerSpawnGrid);
                if (player.TryGetComponent<Rigidbody2D>(out var pRb)) pRb.linearVelocity = Vector2.zero;
            }

            // Find Furthest point from the player for the Enemy
            floorList.Sort((a, b) => Vector2.Distance(playerSpawnGrid, b).CompareTo(Vector2.Distance(playerSpawnGrid, a)));
            Vector2Int enemySpawnGrid = floorList[0];

            // Teleport and Reset Enemy
            EnemyPathAgentTask9 enemy = FindAnyObjectByType<EnemyPathAgentTask9>();
            if (enemy != null && GridManagerTask9.Instance != null)
            {
                enemy.transform.position = GridManagerTask9.Instance.GridToWorld(enemySpawnGrid.x, enemySpawnGrid.y);
                
                // Stop any leftover physics momentum
                if (enemy.TryGetComponent<Rigidbody2D>(out var eRb))
                {
                    eRb.linearVelocity = Vector2.zero;
                    eRb.angularVelocity = 0f;
                }

                enemy.enabled = true;
            }
        }
    }
}