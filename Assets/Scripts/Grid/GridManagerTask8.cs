using UnityEngine;

namespace Task8Dungeon
{
    public enum CellType
    {
        Floor,
        Wall
    }

    public class GridManagerTask8 : MonoBehaviour
    {
        public static GridManagerTask8 Instance { get; private set; }

        [Header("Grid Settings")]
        [SerializeField] private int width = 20;
        [SerializeField] private int height = 15;
        [SerializeField] private float cellSize = 1f;

        [Header("Prefabs")]
        [SerializeField] private GameObject floorPrefab;
        [SerializeField] private GameObject wallPrefab;

        public CellType[,] grid;

        public int Width => width;
        public int Height => height;
        public float CellSize => cellSize;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // GenerateDefaultMap();
            RenderMap();
        }

        /// <summary>
        /// Generates a default dungeon room: floor interior surrounded by walls.
        /// </summary>
        // private void GenerateDefaultMap()
        // {
        //     grid = new CellType[width, height];

        //     for (int x = 0; x < width; x++)
        //     {
        //         for (int y = 0; y < height; y++)
        //         {
        //             // Border walls
        //             if (x == 0 || x == width - 1 || y == 0 || y == height - 1)
        //             {
        //                 grid[x, y] = CellType.Wall;
        //             }
        //             else
        //             {
        //                 grid[x, y] = CellType.Floor;
        //             }
        //         }
        //     }

        //     // Add some interior walls to make it interesting
        //     // Horizontal wall segment
        //     for (int x = 4; x <= 10; x++)
        //     {
        //         grid[x, 7] = CellType.Wall;
        //     }
        //     // Gap in wall for passage
        //     grid[7, 7] = CellType.Floor;

        //     // Vertical wall segment
        //     for (int y = 3; y <= 11; y++)
        //     {
        //         grid[14, y] = CellType.Wall;
        //     }
        //     // Gap
        //     grid[14, 6] = CellType.Floor;

        //     // Small room in corner
        //     for (int x = 2; x <= 5; x++)
        //     {
        //         grid[x, 11] = CellType.Wall;
        //     }
        //     grid[5, 11] = CellType.Floor; // doorway
        //     for (int y = 11; y <= 13; y++)
        //     {
        //         grid[2, y] = CellType.Wall;
        //     }
        // }

        /// <summary>
        /// Instantiates visual tiles for each cell.
        /// </summary>
        private void RenderMap()
        {
            Transform mapParent = new GameObject("Map").transform;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Vector3 worldPos = GridToWorld(x, y);
                    GameObject prefab = grid[x, y] == CellType.Wall ? wallPrefab : floorPrefab;
                    GameObject tile = Instantiate(prefab, worldPos, Quaternion.identity, mapParent);
                    tile.name = $"Tile_{x}_{y}_{grid[x, y]}";
                }
            }
        }

        /// <summary>
        /// Returns true if the given grid coordinate is a floor (walkable).
        /// </summary>
        public bool IsWalkable(int x, int y)
        {
            if (x < 0 || x >= width || y < 0 || y >= height)
                return false;

            return grid[x, y] == CellType.Floor;
        }

        /// <summary>
        /// Returns true if the given grid coordinate is within bounds.
        /// </summary>
        public bool IsInBounds(int x, int y)
        {
            return x >= 0 && x < width && y >= 0 && y < height;
        }

        /// <summary>
        /// Gets the cell type at the given grid coordinate.
        /// </summary>
        public CellType GetCell(int x, int y)
        {
            if (!IsInBounds(x, y))
                return CellType.Wall;
            return grid[x, y];
        }

        /// <summary>
        /// Sets a cell type at the given grid coordinate.
        /// </summary>
        public void SetCell(int x, int y, CellType type)
        {
            if (IsInBounds(x, y))
                grid[x, y] = type;
        }

        /// <summary>
        /// Converts grid coordinates to world position (center of the cell).
        /// </summary>
        public Vector3 GridToWorld(int x, int y)
        {
            return new Vector3(x * cellSize, y * cellSize, 0f);
        }

        /// <summary>
        /// Converts a world position to grid coordinates.
        /// </summary>
        public Vector2Int WorldToGrid(Vector3 worldPos)
        {
            int x = Mathf.RoundToInt(worldPos.x / cellSize);
            int y = Mathf.RoundToInt(worldPos.y / cellSize);
            return new Vector2Int(x, y);
        }
    }
}