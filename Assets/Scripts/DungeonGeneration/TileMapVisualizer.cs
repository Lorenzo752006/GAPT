using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Tilemaps;
using Random = UnityEngine.Random;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;

public class TileMapVisualizer : MonoBehaviour
{
    [SerializeField] private Tilemap floorTileMap, wallTileMap,decorationTileMap;
    [SerializeField] private TileBase floorTile;
    [SerializeField] private TileBase WallTile;



    [Header("Basic Wall Set")]
    [SerializeField] private Tile[] wallTopTile;        
    [SerializeField] private Tile[] wallBottomTile;    
    [SerializeField] private Tile[] wallLeftSideTile;       
    [SerializeField] private Tile[] wallRightSideTile;       

    [Header("Basic Corners")]
    [SerializeField] private Tile wallInnerCornerUpRightDiagonalUpRight; 
    [SerializeField] private Tile wallInnerCornerUpLeftDiagonalUpLeft;

    [Header("Diagonal Corners")]
    [SerializeField] private Tile wallOuterCornerTopLeft; 
    [SerializeField] private Tile wallOuterCornerTopRight;

    [Header("Uncatagorized Tile")]
    [SerializeField] private Tile TopTile; 
    [SerializeField] private Tile defaultWallTile;
    [SerializeField] private Tile Test;

    [Header("Decoration Placement Parmaters")]
    [SerializeField] private Tile[] decorationTiles;
    [SerializeField] private float noiseScale = 0.1f;

    [Range(0f,1f)]
    [SerializeField] private float decorationThreshold = 0.65f;
    [SerializeField] private float randomSeed = 0f;

    public void paintFloorTiles(IEnumerable<Vector2Int> floorPositions)
    {   
        foreach (var position in floorPositions)
        {
            paintTile(position,floorTileMap,floorTile);
        }
        floorTileMap.RefreshAllTiles();
    }

    private void paintTile(Vector2Int position, Tilemap tileMap, TileBase tile)
    {
        Vector3Int tilePosition = new Vector3Int(position.x, position.y, 0);
        tileMap.SetTile(tilePosition, tile);
    }

    public void Clear()
    {
        floorTileMap.ClearAllTiles();
        wallTileMap.ClearAllTiles();
        decorationTileMap.ClearAllTiles();
    }

    private bool wallCheckDone = false;
    private bool wallArraysValid = true;

    private void CheckWallArrays()
    {
        if (wallCheckDone) return; 

        wallCheckDone = true;

        if (wallTopTile == null || wallTopTile.Length == 0 ||
            wallBottomTile == null || wallBottomTile.Length == 0 ||
            wallLeftSideTile == null || wallLeftSideTile.Length == 0 ||
            wallRightSideTile == null || wallRightSideTile.Length == 0)
        {
            wallArraysValid = false; 
            Debug.LogError("Wall arrays are missing or empty! No walls will be painted.");
        }
    }
    
    internal void paintWall(Vector2Int position, int type)
    {
        CheckWallArrays();
        if (!wallArraysValid) return;

        TileBase wallTile = null;


        // Inner Corner Tiles
        if((type & 0b00001010) == 10) wallTile = TopTile;
        else if((type & 0b00000101) == 5) wallTile = wallBottomTile[Random.Range(0, wallBottomTile.Length)];
        else if ((type & 0b00001111) == 3) wallTile = wallInnerCornerUpRightDiagonalUpRight; // Up + Right + Diagonal UpRight
        else if ((type & 0b00001111) == 9) wallTile = wallInnerCornerUpLeftDiagonalUpLeft; // Up + Left + Diagonal UpLeft put before this 
        else if ((type & 0b01000110) == 70) wallTile = wallBottomTile[Random.Range(0, wallBottomTile.Length)]; // Down + Right + Diagonal DownRight
        else if ((type & 0b10001100) == 140) wallTile = wallBottomTile[Random.Range(0, wallBottomTile.Length)]; // Down + Left + Diagonal DownLeft

        


        else if ((type & 0b00000111) == 7)  wallTile = wallBottomTile[Random.Range(0, wallBottomTile.Length)]; // Up + Right + Down 
        else if ((type & 0b00001101) == 13)  wallTile = wallBottomTile[Random.Range(0, wallBottomTile.Length)];// Up + Down + Left

        // T shape Cases
        else if((type & 0b10110000) == 176) wallTile = wallInnerCornerUpLeftDiagonalUpLeft; // UpRight + UpLeft + DownLeft
        else if((type & 0b01110000) == 112) wallTile = wallInnerCornerUpRightDiagonalUpRight; // UpRight + UpLeft + DownRight
        

        else if((type & 0b11010100) == 212) wallTile = wallBottomTile[Random.Range(0, wallBottomTile.Length)]; // DownRight + DownLeft + UpRight + Up + Down
        else if((type & 0b11100100) == 228) wallTile = wallBottomTile[Random.Range(0, wallBottomTile.Length)]; // DownRight + DownLeft + UpLeft  + Up + Down

        // T shape Cases More Checks
        else if((type & 0b11010000) == 208) wallTile = TopTile; // DownRight + DownLeft + UpRight
        else if((type & 0b11100000) == 224) wallTile = TopTile; // DownRight + DownLeft + UpLeft

        // --- Straight Paths ---
        else if((type & 0b00001010) == 10) wallTile = TopTile; // Left + Right
        else if((type & 0b00000101) == 5)  wallTile = wallLeftSideTile[Random.Range(0, wallLeftSideTile.Length)]; // Up + Down
        


        // 3 Diagonals + UP
        else if((type & 0b10110001) == 177) wallTile = TopTile; // UpRight + UpLeft + DownLeft + UP
        else if((type & 0b01110001) == 113) wallTile = TopTile; // UpRight + UpLeft + DownRight + UP
        else if((type & 0b10111000) == 184) wallTile = wallLeftSideTile[Random.Range(0, wallLeftSideTile.Length)]; // UpRight + UpLeft + DownLeft + LEFT
        else if((type & 0b11101000) == 232) wallTile = wallLeftSideTile[Random.Range(0, wallLeftSideTile.Length)]; // DownRight + DownLeft + UpLeft + LEFT
        else if((type & 0b01110010) == 114) wallTile = wallRightSideTile[Random.Range(0, wallRightSideTile.Length)]; // UpRight + UpLeft + DownRight + RIGHT
        else if((type & 0b11010010) == 210) wallTile = wallRightSideTile[Random.Range(0, wallRightSideTile.Length)]; // DownRight + DownLeft + UpRight + RIGHT


        // Outer Corner Tiles
        else if(type == 128) wallTile = wallRightSideTile[Random.Range(0, wallRightSideTile.Length)]; // Diagonal DownLeft
        else if(type == 64)  wallTile = wallLeftSideTile[Random.Range(0, wallLeftSideTile.Length)]; // Diagonal DownRight
        else if(type == 32)  wallTile = wallOuterCornerTopLeft; // Digonal UpLeft
        else if(type == 16)  wallTile = wallOuterCornerTopRight; // Diagonal UpRight

        // Wall Tiles
        else if ((type & 0b00000001) == 1) wallTile = wallTopTile[Random.Range(0, wallTopTile.Length)]; // up
        else if ((type & 0b00000010) == 2) wallTile = wallLeftSideTile[Random.Range(0, wallLeftSideTile.Length)]; // Right
        else if ((type & 0b00000100) == 4) wallTile = wallBottomTile[Random.Range(0, wallBottomTile.Length)];  // Down
        else if ((type & 0b00001000) == 8) wallTile = wallRightSideTile[Random.Range(0, wallRightSideTile.Length)]; // left

        // default wall TIle
        else wallTile = defaultWallTile;

        if (wallTile != null)
        {
            paintTile(position, wallTileMap, wallTile);
        }
    }
    
    internal void placeDecorations(HashSet<Vector2Int> floor)
    {

        if (decorationTiles == null || decorationTiles.Length == 0)
            return;

        int decorCount = decorationTiles.Length;
        System.Random rng = new System.Random((int)randomSeed);

        foreach (var pos in floor)
        {
            float noise = Mathf.PerlinNoise(
                (pos.x + randomSeed) * noiseScale,
                (pos.y + randomSeed) * noiseScale
            );

            if (noise > decorationThreshold)
            {
                Tile decorationTile = decorationTiles[rng.Next(decorCount)];
                paintTile(pos, decorationTileMap, decorationTile);
            }
        }

        decorationTileMap.RefreshAllTiles();
    }



}
                // if((type & 0b00110000) == 16)
                //     wallTile = wallInnerCornerUpRightDiagonalUpRight;
                // else if((type & 0b00110000) == 32)
                //     wallTile = wallInnerCornerUpLeftDiagonalUpLeft;
                // else
                //     wallTile = TopTile;
                // wallTile = TopTile;

                //          if(floorUp)
                //     if((type & 0b00110000) == 16)
                //         wallTile = wallInnerCornerUpRightDiagonalUpRight;
                //     else if((type & 0b00110000) == 32)
                //         wallTile = wallInnerCornerUpLeftDiagonalUpLeft;
                //     else if((type & 0b00110001) == 33){
                //         Debug.Log(type); 
                //         wallTile = wallInnerCornerUpRightDiagonalUpRight;
                //     }
                //     else
                //         wallTile = Test;
                // else
                //     wallTile = TopTile;