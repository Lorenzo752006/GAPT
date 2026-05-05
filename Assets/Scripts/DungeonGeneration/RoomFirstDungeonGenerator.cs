using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
using System.Linq;

// Add limits for offsets and corridors Done
// Add Array for different wall type placment Done
// Implement reandom object placment Done
public class RoomFirstDungeonGenerator : AbstarctDungeonGenerator
{
    [SerializeField]
    private int minRoomWidth = 4, minRoomHeight = 4;
    [SerializeField]
    private Vector2Int dungeonSize = new Vector2Int(10,20);
    [SerializeField]
    [Range(0,2)]
    private int corridorSize = 1;

    [SerializeField]
    [Range(0,3)]
    private int offset = 1;

    protected override void runProceduralGeneration()
    {
        CreateRooms();
    }

    private void CreateRooms()
    {
        var roomList = ProceduralGenerationAlgorithms.binarySpacePartition(
            new BoundsInt((Vector3Int)startPosition, (Vector3Int)dungeonSize),
            minRoomWidth,
            minRoomHeight
        );

        HashSet<Vector2Int> roomFloor = createSimpleRooms(roomList);
        HashSet<Vector2Int> floor = new HashSet<Vector2Int>(roomFloor);

        List<Vector2Int> roomCenters = new List<Vector2Int>();
        foreach (var room in roomList)
        {
            roomCenters.Add((Vector2Int)Vector3Int.RoundToInt(room.center));
        }

        var corridorsList = connectRooms(roomCenters);
        HashSet<Vector2Int> corridorFloor = new HashSet<Vector2Int>();
        foreach (var corridor in corridorsList)
        {
            HashSet<Vector2Int> painted = PaintCorridor(corridor, corridorSize);
            corridorFloor.UnionWith(painted);
        }

        floor.UnionWith(corridorFloor);

        tileMapVisualizer.paintFloorTiles(floor);
        tileMapVisualizer.placeDecorations(roomFloor);
        WallGenerator.createWalls(floor, tileMapVisualizer);
    }


    List<List<Vector2Int>> connectRooms(List<Vector2Int> roomCenters)
    {
        List<List<Vector2Int>> corridors = new List<List<Vector2Int>>();
        var currentRoomCenter = roomCenters[Random.Range(0, roomCenters.Count)];
        roomCenters.Remove(currentRoomCenter);

        while(roomCenters.Count > 0)
        {
            Vector2Int closest = findClosestPointTo(currentRoomCenter, roomCenters);
            roomCenters.Remove(closest);

            List<Vector2Int> newCorridor = createCorridor(currentRoomCenter, closest).ToList();
            corridors.Add(newCorridor);

            currentRoomCenter = closest;
        }

        return corridors;
    }

    private HashSet<Vector2Int> createCorridor(Vector2Int currentRoomCenter, Vector2Int destination)
    {
        HashSet<Vector2Int> corridor = new HashSet<Vector2Int>();
        var position = currentRoomCenter;
        corridor.Add(position);

        if (Random.value < 0.5f)
        {
            while (position.y != destination.y)
            {
                position += (destination.y > position.y) ? Vector2Int.up : Vector2Int.down;
                corridor.Add(position);
            }
            while (position.x != destination.x)
            {
                position += (destination.x > position.x) ? Vector2Int.right : Vector2Int.left;
                corridor.Add(position);
            }
        }
        else
        {
            while (position.x != destination.x)
            {
                position += (destination.x > position.x) ? Vector2Int.right : Vector2Int.left;
                corridor.Add(position);
            }
            while (position.y != destination.y)
            {
                position += (destination.y > position.y) ? Vector2Int.up : Vector2Int.down;
                corridor.Add(position);
            }
        }

        return corridor;
    }

    private Vector2Int findClosestPointTo(Vector2Int currentRoomCenter, List<Vector2Int> roomCenters)
    {
       Vector2Int closest = Vector2Int.zero;
       float distance = float.MaxValue;

       foreach (var position in roomCenters)
       {
            float currentDistance = Vector2.Distance(position,currentRoomCenter);
            if(currentDistance < distance)
            {
                distance = currentDistance;
                closest = position;
            }
       }
       return closest;
    }

    private HashSet<Vector2Int> createSimpleRooms(List<BoundsInt> roomList)
    {
        HashSet<Vector2Int> floor = new  HashSet<Vector2Int>();
        foreach (var room in roomList)
        {
            for(int col = offset; col < room.size.x - offset; col++)
            {
                for (int row = offset; row < room.size.y - offset; row++)
                {
                    Vector2Int position = (Vector2Int)room.min + new Vector2Int(col,row);
                    floor.Add(position);
                }
            }
        }
        return floor;
    }

    HashSet<Vector2Int> PaintCorridor(List<Vector2Int> corridor, int size)
    {
        HashSet<Vector2Int> paintedCorridor = new HashSet<Vector2Int>();
        Vector2Int previous = corridor[0];

        foreach (var position in corridor)
        {
            if (position.x != previous.x && position.y != previous.y)
            {
                PaintAt(new Vector2Int(position.x, previous.y), size, paintedCorridor);
            }

            PaintAt(position, size, paintedCorridor);
            previous = position;
        }

        return paintedCorridor;
    }



    void PaintAt(Vector2Int pos, int size, HashSet<Vector2Int> set)
    {
    
        BoundsInt brush = new BoundsInt(
            new Vector3Int(pos.x - size, pos.y - size, 0), 
            new Vector3Int(size * 2 + 1, size * 2 + 1, 1)
        );

        foreach (var p in brush.allPositionsWithin)
        {
            set.Add((Vector2Int)p);
        }
    }

    
}
