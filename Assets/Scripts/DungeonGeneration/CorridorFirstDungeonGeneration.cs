using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class CorridorFirstDungeonGeneration : SimpleRandomWalkDungeonGenerator
{
    [SerializeField]
    private int corridorLength = 14, corridorCount = 5;
    [SerializeField]
    [Range(0.1f,1f)]
    private float roomPercent = 0.8f;
    [SerializeField]
    private int corridorSize = 3;

    protected override void runProceduralGeneration()
    {
        CorridorFirstGeneration();
    }

    private void CorridorFirstGeneration()
    {
        HashSet<Vector2Int> floorPositions = new HashSet<Vector2Int>();
        HashSet<Vector2Int> potentialRoomPositions = new HashSet<Vector2Int>();

        List<List<Vector2Int>> corridors = CreateCorridors(floorPositions,potentialRoomPositions);
        HashSet<Vector2Int> roomPositions = CreateRooms(potentialRoomPositions);

        List<Vector2Int> deadEnds =  findAllDeadEnds(floorPositions);
        createRoomsAtDeadEnd(deadEnds,roomPositions);
        floorPositions.UnionWith(roomPositions);
        
        for (int i = 0; i < corridors.Count; i++)
        {
            corridors[i] = PaintCorridor(corridors[i],corridorSize).ToList();
            floorPositions.UnionWith(corridors[i]);
        }

        tileMapVisualizer.paintFloorTiles(floorPositions);
        WallGenerator.createWalls(floorPositions,tileMapVisualizer);
    }

    List<Vector2Int> PaintCorridor(List<Vector2Int> corridor, int size)
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

        return paintedCorridor.ToList();
    }

    void PaintAt(Vector2Int pos, int size, HashSet<Vector2Int> set)
    {
        for (int x = -size; x <= size; x++)
        {
            for (int y = -size; y <= size; y++)
            {
                set.Add(pos + new Vector2Int(x, y));
            }
        }
    }

    private void createRoomsAtDeadEnd(List<Vector2Int> deadEnds, HashSet<Vector2Int> roomFloors)
    {
        foreach (var position in deadEnds)
        {
            if(roomFloors.Contains(position) == false)
            {
                var room = runRandomWalk(simpleRandomWalkSo,position);
                roomFloors.UnionWith(room);
            }
        }
    }

    private List<Vector2Int> findAllDeadEnds(HashSet<Vector2Int> floorPositions)
    {
        List<Vector2Int> deadEnds = new List<Vector2Int>();
        foreach (var position in floorPositions)
        {
            int neighnoursCount = 0;
            foreach (var direction in Direction2D.cardinalDirectionsList)
            {
                if(floorPositions.Contains(position + direction))
                    neighnoursCount++;
            }
            if(neighnoursCount == 1)
                deadEnds.Add(position); 
        }
        return deadEnds;
    }

    private HashSet<Vector2Int> CreateRooms(HashSet<Vector2Int> potentialRoomPositions)
    {
        HashSet<Vector2Int> roomPositions = new HashSet<Vector2Int>();
        int roomToCreateCount = Mathf.RoundToInt(potentialRoomPositions.Count * roomPercent);

        List<Vector2Int> roomToCreate = potentialRoomPositions
            .OrderBy(x => UnityEngine.Random.value)
            .Take(roomToCreateCount)
            .ToList();

        foreach (var roomPosition in roomToCreate)
        {
            var roomFloor = runRandomWalk(simpleRandomWalkSo,roomPosition);
            roomPositions.UnionWith(roomFloor);
        }

        return roomPositions;
    }

    private List<List<Vector2Int>> CreateCorridors(HashSet<Vector2Int> floorPositions,HashSet<Vector2Int> potentialRoomPositions)
    {
        var currentPosition = startPosition;
        potentialRoomPositions.Add(currentPosition);
        List<List<Vector2Int>> corridors = new List<List<Vector2Int>>();

        for (int i = 0; i < corridorCount; i++)
        {
            var corridor =  ProceduralGenerationAlgorithms.randomWalkCorridor(currentPosition,corridorLength);
            corridors.Add(corridor);
            currentPosition = corridor.Last();
            potentialRoomPositions.Add(currentPosition);
            floorPositions.UnionWith(corridor);
        }
        return corridors;
    }
}
