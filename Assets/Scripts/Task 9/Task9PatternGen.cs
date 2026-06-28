using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Task9;
using UnityEngine.SceneManagement;

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

    [Header("Persistence")]
    [SerializeField] private DungeonData savedData;

    [Header("Permanent Map Settings")]
    [SerializeField] private Task12SavedMapData permanentMap; 
    [SerializeField] private bool usePermanentMap = false;
    
    // -----------------------------------------------------------------------------------------------------------------------------------

    private bool hasLearned = false;


    // These dictionaries store the learned probabilities of placing a floor tile based on the presence of neighboring tiles.
    private Dictionary<bool, List<bool>> horizontalRules = new Dictionary<bool, List<bool>>() 
    { 
        { true, new List<bool>() }, { false, new List<bool>() } 
    };
    private Dictionary<bool, List<bool>> verticalRules = new Dictionary<bool, List<bool>>() 
    { 
        { true, new List<bool>() }, { false, new List<bool>() } 
    };
    private Dictionary<bool, List<bool>> diagonalRules = new Dictionary<bool, List<bool>>() 
    { 
        { true, new List<bool>() }, { false, new List<bool>() } 
    };

    // -----------------------------------------------------------------------------------------------------------------------------------

    private void Start()
    {
        GenerateNewRandomMap();
    }

    public void GenerateNewRandomMap()
    {
        // Only learn once to save CPU time
        if (!hasLearned)
        {
            LearnFromTilemap();
            hasLearned = true;
        }

        runProceduralGeneration();
    }



    public override void generateDungeon()
    {
        GenerateNewRandomMap();
    }




    protected override void runProceduralGeneration()
    {
        HashSet<Vector2Int> finalMap = new HashSet<Vector2Int>();

        // Determine the map layout (Load vs Generate)
        if (usePermanentMap && permanentMap != null && permanentMap.floorPositions.Count > 0)
        {
            foreach (var pos in permanentMap.floorPositions)
            {
                finalMap.Add(pos + startPosition);
            }
            Debug.Log("Loading Permanent Map Layout...");
        }
        else
        {
            HashSet<Vector2Int> rawMap = GenerateNewMap();

            // Polishing Phase
            HashSet<Vector2Int> polishedMap = SmoothMap(rawMap);
            polishedMap = SmoothMap(polishedMap);
            polishedMap = SmoothMap(polishedMap); 
            
            finalMap = KeepOnlyLargestArea(polishedMap);
        }

        // 2. Visualization
        tileMapVisualizer.Clear();
        tileMapVisualizer.paintFloorTiles(finalMap);
        WallGenerator.createWalls(finalMap, tileMapVisualizer);

        // Update GridManager BEFORE spawning anything
        if (GridManagerTask9.Instance != null)
        {
            GridManagerTask9.Instance.InitializeFromTilemap(finalMap);
        }

        // Spawn Entities (This handles the initial scene start setup)
        SpawnEntities(finalMap);
    }


    // -----------------------------------------------------------------------------------------------------------------------------------

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


    [ContextMenu("Save Learned Patterns")]
    public void SavePatternsToFile()
    {
        if (savedData == null) { Debug.LogError("Attach the DungeonData file!"); return; }

        savedData.horizontalTrue = new List<bool>(horizontalRules[true]);
        savedData.horizontalFalse = new List<bool>(horizontalRules[false]);
        savedData.verticalTrue = new List<bool>(verticalRules[true]);
        savedData.verticalFalse = new List<bool>(verticalRules[false]);
        savedData.diagonalTrue = new List<bool>(diagonalRules[true]);
        savedData.diagonalFalse = new List<bool>(diagonalRules[false]);

    #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(savedData);
        UnityEditor.AssetDatabase.SaveAssets();
    #endif
        Debug.Log("Success! Patterns saved to " + savedData.name);
    }



    private HashSet<Vector2Int> GenerateNewMap()
    {
        HashSet<Vector2Int> newFloor = new HashSet<Vector2Int>();
        bool[,] grid = new bool[genSize.x, genSize.y];

        // Seed the first tile so the logic has a starting point
        grid[0, 0] = true; 
        newFloor.Add(new Vector2Int(0, 0) + startPosition);

        List<bool> choices = new List<bool>(); 

        for (int x = 0; x < genSize.x; x++)
        {
            for (int y = 0; y < genSize.y; y++)
            {
                // Skip the seed tile we already placed
                if (x == 0 && y == 0) continue;

                // Protective boundary checks to prevent IndexOutOfRangeException
                bool left = (x > 0) ? grid[x - 1, y] : false;
                bool down = (y > 0) ? grid[x, y - 1] : false;
                bool downLeft = (x > 0 && y > 0) ? grid[x - 1, y - 1] : false;

                choices.Clear();
                
                // pull from saved data file
                choices.AddRange(left ? savedData.horizontalTrue : savedData.horizontalFalse);
                choices.AddRange(down ? savedData.verticalTrue : savedData.verticalFalse);
                choices.AddRange(downLeft ? savedData.diagonalTrue : savedData.diagonalFalse);

                if (choices.Count > 0)
                {
                    bool decision = choices[Random.Range(0, choices.Count)];
                    
                    // Floor Boost helps bridge gaps if the learned data is too "sparse"
                    if (!decision && (left || down || downLeft) && Random.value < floorBoost) 
                        decision = true;

                    grid[x, y] = decision;
                    if (decision) newFloor.Add(new Vector2Int(x, y) + startPosition);
                }
            }
        }
        return newFloor;
    }

    // -----------------------------------------------------------------------------------------------------------------------------------


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


    // -----------------------------------------------------------------------------------------------------------------------------------


    private void SpawnEntities(HashSet<Vector2Int> finalMap)
    {
        List<Vector2Int> floorList = new List<Vector2Int>(finalMap);
        if (floorList.Count < 2) return;

        // Sync Player
        Task9PlayerController player = FindAnyObjectByType<Task9PlayerController>();
        Vector2Int playerGridPos = floorList[0];
        if (player != null) player.SetPosition(playerGridPos);

        // Find Spacious Tiles
        List<Vector2Int> spaciousTiles = new List<Vector2Int>();
        foreach (var tile in floorList)
        {
            if (finalMap.Contains(tile + Vector2Int.up) && finalMap.Contains(tile + Vector2Int.down) &&
                finalMap.Contains(tile + Vector2Int.left) && finalMap.Contains(tile + Vector2Int.right))
                spaciousTiles.Add(tile);
        }

        List<Vector2Int> candidateList = spaciousTiles.Count > 0 ? spaciousTiles : floorList;
        candidateList.Sort((a, b) => Vector2.Distance(playerGridPos, b).CompareTo(Vector2.Distance(playerGridPos, a)));
        Vector2Int bestEnemyPos = candidateList[0];

        // Robust Enemy Finding (Works for Scene 9 and Scene 12)
        
        EnemyPathAgentTask9 pathEnemy = FindAnyObjectByType<EnemyPathAgentTask9>();
        GameObject enemyObj = (pathEnemy != null) ? pathEnemy.gameObject : GameObject.FindGameObjectWithTag("Enemy");

        if (enemyObj != null)
        {
            Vector3 spawnWorldPos = (GridManagerTask9.Instance != null) 
                ? GridManagerTask9.Instance.GridToWorld(bestEnemyPos.x, bestEnemyPos.y) 
                : new Vector3(bestEnemyPos.x, bestEnemyPos.y, 0);

            enemyObj.transform.position = spawnWorldPos;

            if (enemyObj.TryGetComponent<Rigidbody2D>(out var rb)) rb.linearVelocity = Vector2.zero;
            
            // Check if we are in the Task 12 scene
            if (SceneManager.GetActiveScene().name == "Task12") 
            {
                if (pathEnemy != null) pathEnemy.enabled = false; 
                Debug.Log("Enemy movement disabled for Task 12.");
            }
            else
            {
                if (pathEnemy != null) pathEnemy.enabled = true;
                Debug.Log("Enemy movement enabled.");
            }
        }
    }


    public Vector2 GetRandomFloorPosition()
    {
        // Access the tilemap visualizer to get the list of floor tiles currently painted
        // Safety check: find all active floor tiles in the current grid
        List<Vector2Int> floors = new List<Vector2Int>();
        for (int x = 0; x < genSize.x; x++)
        {
            for (int y = 0; y < genSize.y; y++)
            {
                if (GridManagerTask9.Instance.IsWalkable(x, y)) 
                    floors.Add(new Vector2Int(x, y));
            }
        }

        if (floors.Count > 0)
        {
            Vector2Int randomTile = floors[Random.Range(0, floors.Count)];
            return GridManagerTask9.Instance.GridToWorld(randomTile.x, randomTile.y);
        }

        return Vector2.zero; // Fallback
    }




    // FOR TASK 12 -----------------------------------------------------------------------------------------------------------------------------------

    
    [ContextMenu("Save Current Map to Asset")]
    public void SaveToPermanentAsset()
    {
        if (permanentMap == null)
        {
            Debug.LogError("Please assign a SavedMapData asset to the 'permanentMap' slot!");
            return;
        }

        permanentMap.floorPositions.Clear();

        // We look at what is currently in the GridManager to see where the floors are
        for (int x = 0; x < genSize.x; x++)
        {
            for (int y = 0; y < genSize.y; y++)
            {
                if (GridManagerTask9.Instance.IsWalkable(x, y))
                {
                    permanentMap.floorPositions.Add(new Vector2Int(x, y));
                }
            }
        }

    #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(permanentMap);
        UnityEditor.AssetDatabase.SaveAssets();
    #endif

        Debug.Log($"Successfully saved {permanentMap.floorPositions.Count} tiles to {permanentMap.name}. You can now enable 'Use Permanent Map'.");
    }

}