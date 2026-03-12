using System.Collections.Generic;
using UnityEngine;

public static class WallGenerator 
{
    private static HashSet<Vector2Int> _wallPositions = new HashSet<Vector2Int>();

    public static HashSet<Vector2Int> WallPositions { get => _wallPositions;}

    public static void createWalls(HashSet<Vector2Int> floorPositions, TileMapVisualizer tileMapVisualizer)
    {
        _wallPositions.Clear();
        findWallsInDirections(floorPositions, Direction2D.allDirectionsList);

        foreach (var position in _wallPositions)
        {
            int typeAsInt = 0;
            for (int i = 0; i < Direction2D.allDirectionsList.Count; i++)
            {
                var neighbourPosition = position + Direction2D.allDirectionsList[i];
                if (floorPositions.Contains(neighbourPosition))
                {
                    typeAsInt |= (1 << i); 
                }
            }
            tileMapVisualizer.paintWall(position, typeAsInt);
        }
    }
    
    private static void findWallsInDirections(HashSet<Vector2Int> floorPositions, List<Vector2Int> directionList)
    {

        foreach (var position  in floorPositions)
        {
            foreach (var direction in directionList)
            {
                var neighbourPosition = position + direction;
                if(floorPositions.Contains(neighbourPosition) == false)
                    _wallPositions.Add(neighbourPosition);
            }
        }
    }
}

