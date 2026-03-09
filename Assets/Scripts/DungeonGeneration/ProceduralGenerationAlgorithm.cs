using System.Collections.Generic;
using UnityEditor.PackageManager;
using UnityEngine;

public static class ProceduralGenerationAlgorithms
{
    public static HashSet<Vector2Int> simpleRandomWalk(Vector2Int startPosition,int walkLength)
    {
        HashSet<Vector2Int> path = new HashSet<Vector2Int>();

        path.Add(startPosition);
        var previousPosition = startPosition;

        for (int i = 0; i < walkLength; i++)
        {
            var newPosition = previousPosition + Direction2D.GetRandomCardinalDirection();
            path.Add(newPosition);
            previousPosition = newPosition;
        }

        return path;
    }

    public static List<Vector2Int> randomWalkCorridor(Vector2Int startPosition,int corridorLength)
    {
        List<Vector2Int> corridor = new List<Vector2Int>();
        var direction = Direction2D.GetRandomCardinalDirection();
        var currentPosition = startPosition;

        corridor.Add(currentPosition);
        
        for (int i = 0; i < corridorLength; i++)
        {
            currentPosition += direction;
            corridor.Add(currentPosition);
        }
        return corridor;
    }

    public static List<BoundsInt> binarySpacePartition(BoundsInt spaceToSplit,int minWidth, int minHeight)
    {
        Queue<BoundsInt> roomsQueue = new Queue<BoundsInt>();
        List<BoundsInt> roomsList = new List<BoundsInt>();

        roomsQueue.Enqueue(spaceToSplit);
        while(roomsQueue.Count > 0)
        {
            var room = roomsQueue.Dequeue();
            if(room.size.y >= minHeight && room.size.x >= minWidth)
            {
                if(Random.value < 0.5f)
                {
                    if(room.size.y >= minHeight * 2)
                    {
                        splitHorizontally(minHeight,roomsQueue,room);
                    }else if(room.size.x >= minWidth * 2)
                    {
                        splitVertically(minWidth,roomsQueue,room);
                    }
                    else if(room.size.x >= minWidth && room.size.y >= minHeight)
                    {
                        roomsList.Add(room);
                    }

                }
                else
                {
                    if(room.size.x >= minHeight * 2)
                    {
                        splitVertically(minWidth,roomsQueue,room);
                    }else if(room.size.y >= minWidth * 2)
                    {
                        splitHorizontally(minHeight,roomsQueue,room);
                    }
                    else if(room.size.x >= minWidth && room.size.y >= minHeight)
                    {
                        roomsList.Add(room);
                    }
                }
            }
        }
        return roomsList;
    }

    private static void splitVertically(int minWidth, Queue<BoundsInt> roomsQueue, BoundsInt room)
    {
        var xSplit = Random.Range(1, room.size.x - minWidth);
        BoundsInt room1 = new BoundsInt(
            room.min,
            new Vector3Int(xSplit, room.size.y, room.size.z)
        );
        BoundsInt room2 = new BoundsInt(
            new Vector3Int(room.min.x + xSplit,room.min.y,room.min.z),
            new Vector3Int(room.size.x - xSplit,room.size.y,room.size.z)
        );
        roomsQueue.Enqueue(room1);
        roomsQueue.Enqueue(room2);
    }

    private static void splitHorizontally(int minHeight, Queue<BoundsInt> roomsQueue, BoundsInt room)
    {
        var ySplit = Random.Range(1, room.size.y - minHeight);
        BoundsInt room1 = new BoundsInt(
            room.min,
            new Vector3Int(room.size.x, ySplit, room.size.z)
        );
        BoundsInt room2 = new BoundsInt(
            new Vector3Int(room.min.x,room.min.y + ySplit,room.min.z),
            new Vector3Int(room.size.x ,room.size.y - ySplit,room.size.z)
        );
        roomsQueue.Enqueue(room1);
        roomsQueue.Enqueue(room2);
    }

    
}

public static class Direction2D
{
    public static List<Vector2Int> cardinalDirectionsList = new List<Vector2Int>()
    {
      Vector2Int.up,
      Vector2Int.right,
      Vector2Int.down,
      Vector2Int.left  
    };

    public static List<Vector2Int> allDirectionsList = new List<Vector2Int>
    {
        new Vector2Int(0, 1),   // 0: Up
        new Vector2Int(1, 0),   // 1: Right
        new Vector2Int(0, -1),  // 2: Down
        new Vector2Int(-1, 0),  // 3: Left
        new Vector2Int(1, 1),   // 4: Up-Right
        new Vector2Int(-1, 1),  // 5: Up-Left
        new Vector2Int(1, -1),  // 6: Down-Right
        new Vector2Int(-1, -1)  // 7: Down-Left
    };

    public static Vector2Int GetRandomCardinalDirection()
    {
        return cardinalDirectionsList[Random.Range(0,cardinalDirectionsList.Count)];
    }
}
