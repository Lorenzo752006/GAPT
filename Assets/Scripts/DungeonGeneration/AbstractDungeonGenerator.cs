using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Tilemaps;

public abstract class AbstarctDungeonGenerator : MonoBehaviour
{
    [SerializeField] protected TileMapVisualizer tileMapVisualizer = null;
    [SerializeField] protected Vector2Int startPosition = Vector2Int.zero;
    
    public void generateDungeon()
    {
        tileMapVisualizer.Clear();
        runProceduralGeneration();
    }

    protected abstract void runProceduralGeneration();
}
