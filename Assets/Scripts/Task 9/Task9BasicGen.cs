using System.Collections.Generic;
using UnityEngine;

public class Task9BasicGen : RoomFirstDungeonGenerator
{
    [SerializeField] private Vector2Int genSize = new Vector2Int(50, 50);
    [SerializeField] [Range(0, 1)] private float floorChance = 0.4f;

    protected override void runProceduralGeneration()
    {

        tileMapVisualizer.Clear();  
        
        HashSet<Vector2Int> floorPositions = new HashSet<Vector2Int>(); 
        
        for (int x = 0; x < genSize.x; x++)
        {
            for (int y = 0; y < genSize.y; y++)
            {
                if (Random.value < floorChance)
                    floorPositions.Add(new Vector2Int(x, y) + startPosition);
            }
        }

        tileMapVisualizer.paintFloorTiles(floorPositions);
        // This will create walls around all floor tiles, including isolated ones, which may not be ideal for gameplay but serves as a basic example of procedural generation.
        WallGenerator.createWalls(floorPositions, tileMapVisualizer);
    }
}