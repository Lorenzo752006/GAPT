using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class SimpleRandomWalkDungeonGenerator : AbstarctDungeonGenerator
{
    [SerializeField] protected SimpleRandomWalkSo simpleRandomWalkSo;

    protected override void runProceduralGeneration()
    {
        HashSet<Vector2Int> floorPositions = runRandomWalk(simpleRandomWalkSo,startPosition);
        tileMapVisualizer.paintFloorTiles(floorPositions);
        WallGenerator.createWalls(floorPositions,tileMapVisualizer);
    }

    protected HashSet<Vector2Int> runRandomWalk(SimpleRandomWalkSo parameters,Vector2Int position) 
    {
        var currentPosition = position;
        HashSet<Vector2Int> floorPositions = new HashSet<Vector2Int>();

        for (int i = 0; i < parameters.iterations; i++)
        {
            var path = ProceduralGenerationAlgorithms.simpleRandomWalk(currentPosition,parameters.walkLength);
            floorPositions.UnionWith(path);

            if(parameters.startRandonmlyEachIteration)
                currentPosition = floorPositions.ElementAt(Random.Range(0,floorPositions.Count));
        }

        return floorPositions;
    }
}
