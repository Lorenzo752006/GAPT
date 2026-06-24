using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Task9
{
    public class GridManagerTask9 : MonoBehaviour
    {
        public static GridManagerTask9 Instance { get; private set; }

        [Header("References")]
        [SerializeField] private Tilemap floorTilemap; 
        
        [Header("Grid Settings")]
        [SerializeField] private float cellSize = 1f;

        // We use a HashSet for fast lookup of walkable coordinates
        private HashSet<Vector2Int> walkableTiles = new HashSet<Vector2Int>();

        public float CellSize => cellSize;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        // This is called by Task9PatternGen after it finishes painting tiles
        public void InitializeFromTilemap(HashSet<Vector2Int> floorPositions)
        {
            // Simply store the positions that the generator just painted as floors
            walkableTiles = new HashSet<Vector2Int>(floorPositions);
            Debug.Log($"GridManagerTask9 synced with {walkableTiles.Count} walkable tiles.");
        }

        public bool IsWalkable(int x, int y)
        {
            // If the coordinate is in our floor HashSet, it's walkable
            return walkableTiles.Contains(new Vector2Int(x, y));
        }

        // Helper to check if a world position is on a floor tile
        public bool IsWorldPosWalkable(Vector3 worldPos)
        {
            Vector2Int gridPos = WorldToGrid(worldPos);
            return IsWalkable(gridPos.x, gridPos.y);
        }

        public Vector3 GridToWorld(int x, int y)
        {
            // Using Tilemap's built-in conversion to ensure centers match perfectly
            return floorTilemap.GetCellCenterWorld(new Vector3Int(x, y, 0));
        }

        public Vector2Int WorldToGrid(Vector3 worldPos)
        {
            if (floorTilemap == null) return Vector2Int.zero; 
            
            Vector3Int cellPos = floorTilemap.WorldToCell(worldPos);
            return new Vector2Int(cellPos.x, cellPos.y);
        }
    }
}