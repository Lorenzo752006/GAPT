using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Task9;

public class Task9PatternGen : RoomFirstDungeonGenerator
{
    [Header("References")]
    [SerializeField] private RoomFirstDungeonGenerator task8Generator; 
    [SerializeField] private Tilemap teammateFloorTilemap; 

    [Header("Learning Settings")]
    [SerializeField] private int iterations = 50;
    [SerializeField] private Vector2Int genSize = new Vector2Int(50, 50);

    [Header("Advanced Tuning")]     
    [SerializeField] [Range(0, 1f)] private float floorBoost = 0.2f; // Increases floor chance manually to minimise small closed off rooms



    private Dictionary<bool, List<bool>> horizontalRules = new Dictionary<bool, List<bool>>();  
    private Dictionary<bool, List<bool>> verticalRules = new Dictionary<bool, List<bool>>(); 
    private Dictionary<bool, List<bool>> diagonalRules = new Dictionary<bool, List<bool>>(); 

    private void Start()
    {
        runProceduralGeneration();
    }


    protected override void runProceduralGeneration()
    {
        // Learning Phase
        LearnFromTilemap();

        // Generation Phase
        HashSet<Vector2Int> rawMap = GenerateNewMap();

        // Polishing Phase (Smoothing)
        HashSet<Vector2Int> polishedMap = SmoothMap(rawMap);
        polishedMap = SmoothMap(polishedMap);
        polishedMap = SmoothMap(polishedMap); // to further reduce noise and create more cohesive areas
        //(Remove unreachable islands)
        HashSet<Vector2Int> finalMap = KeepOnlyLargestArea(polishedMap);

        //Visualization
        tileMapVisualizer.Clear();
        tileMapVisualizer.paintFloorTiles(finalMap);
        WallGenerator.createWalls(finalMap, tileMapVisualizer);

        // Sync with your new Grid Manager
        if (GridManagerTask9.Instance != null)
        {
            // We pass the finalMap and the startPosition so the grid coordinates align
            GridManagerTask9.Instance.InitializeFromTilemap(finalMap);
            
            Debug.Log("GridManagerTask9 updated with new procedural map data.");
        }

        // Move the player to a valid starting position on the new map
        SpawnPlayerOnFloor(finalMap);
    
    }   


    private void SpawnPlayerOnFloor(HashSet<Vector2Int> finalMap)
    {
        // Get the first available floor tile from our set
        foreach (var pos in finalMap)
        {
            Task9PlayerController player = FindAnyObjectByType<Task9PlayerController>();
            if (player != null)
            {
                // Convert global tile position to the local grid position the Player understands
                Vector2Int localPos = pos - startPosition;
                player.SetPosition(localPos);
                
                Debug.Log($"Player spawned at local grid position: {localPos}");
                break; // Exit after moving the player once
            }
        }
    }

    private void LearnFromTilemap()
    {
        horizontalRules[true] = new List<bool>(); horizontalRules[false] = new List<bool>();
        verticalRules[true] = new List<bool>();   verticalRules[false] = new List<bool>();
        diagonalRules[true] = new List<bool>();   diagonalRules[false] = new List<bool>();

        for (int i = 0; i < iterations; i++)
        {
            task8Generator.generateDungeon(); 

            BoundsInt bounds = teammateFloorTilemap.cellBounds; 
            for (int x = bounds.xMin; x < bounds.xMax; x++)  
            {
                for (int y = bounds.yMin; y < bounds.yMax; y++)  
                {
                    Vector3Int pos = new Vector3Int(x, y, 0);   
                    bool hasTile = teammateFloorTilemap.HasTile(pos);  

                    // Learn Horizontal
                    horizontalRules[hasTile].Add(teammateFloorTilemap.HasTile(pos + Vector3Int.right));

                    // Learn Vertical
                    verticalRules[hasTile].Add(teammateFloorTilemap.HasTile(pos + Vector3Int.up));

                    // Learn Diagonal (Up-Right)
                    diagonalRules[hasTile].Add(teammateFloorTilemap.HasTile(pos + new Vector3Int(1, 1, 0)));
                }
            }
            tileMapVisualizer.Clear(); 
        }
    }



    private HashSet<Vector2Int> GenerateNewMap()
    {
        HashSet<Vector2Int> newFloor = new HashSet<Vector2Int>();   
        bool[,] grid = new bool[genSize.x, genSize.y];  

        for (int x = 1; x < genSize.x; x++)  
        {
            for (int y = 1; y < genSize.y; y++)
            {
                bool left = grid[x - 1, y];
                bool down = grid[x, y - 1];
                bool downLeft = grid[x - 1, y - 1];


                // Decision Making Based on Learned Patterns
                List<bool> choices = new List<bool>();
                choices.AddRange(horizontalRules[left]);
                choices.AddRange(verticalRules[down]);
                choices.AddRange(diagonalRules[downLeft]);

                if (choices.Count > 0)
                {
                    bool decision = choices[Random.Range(0, choices.Count)];
                    
                    // Floor Boosting 
                    // If the AI wants to place a wall, but a neighbor is a floor, 
                    // give it a second chance to "keep the room going."
                    if (!decision && (left || down || downLeft))
                    {
                        if (Random.value < floorBoost) decision = true;
                    }

                    grid[x, y] = decision;
                    if (decision) newFloor.Add(new Vector2Int(x, y) + startPosition);
                }
            }
        }
        return newFloor;
    }


    private HashSet<Vector2Int> SmoothMap(HashSet<Vector2Int> floorPositions)
    {
        HashSet<Vector2Int> smoothedFloor = new HashSet<Vector2Int>();

        // We scan the generation area
        for (int x = 0; x < genSize.x; x++)
        {
            for (int y = 0; y < genSize.y; y++)
            {
                Vector2Int pos = new Vector2Int(x, y) + startPosition;
                int neighborCount = 0;

                // Check all 8 neighbors around this tile
                for (int neighborX = x - 1; neighborX <= x + 1; neighborX++)
                {
                    for (int neighborY = y - 1; neighborY <= y + 1; neighborY++)
                    {
                        if (neighborX == x && neighborY == y) continue;
                        
                        if (floorPositions.Contains(new Vector2Int(neighborX, neighborY) + startPosition))
                        {
                            neighborCount++;
                        }
                    }
                }

                // The Rule: Only keep the tile if it's "supported" by neighbors
                // This helps to remove isolated tiles and smooth out the map
                if (neighborCount > 4) 
                    smoothedFloor.Add(pos);
                else if (neighborCount < 2) 
                    // This removes the tiny 1x1 islands
                    continue; 
                else if (floorPositions.Contains(pos) && neighborCount >= 2)
                    smoothedFloor.Add(pos);
            }
        }
        return smoothedFloor;
    }


    // This function removes any disconnected "islands" of floor, keeping only the largest connected area.
    private HashSet<Vector2Int> KeepOnlyLargestArea(HashSet<Vector2Int> floorPositions) 
    {
        if (floorPositions.Count == 0) return floorPositions;

        List<HashSet<Vector2Int>> allAreas = new List<HashSet<Vector2Int>>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        foreach (var pos in floorPositions)
        {
            if (visited.Contains(pos)) continue;

            // Start a new area search (Flood Fill)
            HashSet<Vector2Int> newArea = new HashSet<Vector2Int>();
            Stack<Vector2Int> stack = new Stack<Vector2Int>();
            stack.Push(pos);

            
            while (stack.Count > 0)
            {
                Vector2Int current = stack.Pop();
                if (visited.Contains(current) || !floorPositions.Contains(current)) continue;

                visited.Add(current);
                newArea.Add(current);
                
                stack.Push(current + Vector2Int.up);
                stack.Push(current + Vector2Int.down);
                stack.Push(current + Vector2Int.left);
                stack.Push(current + Vector2Int.right);
            }
            allAreas.Add(newArea);
        }

        // Sort by size and keep the biggest one
        allAreas.Sort((a, b) => b.Count.CompareTo(a.Count));
        return allAreas.Count > 0 ? allAreas[0] : new HashSet<Vector2Int>();
    }
}