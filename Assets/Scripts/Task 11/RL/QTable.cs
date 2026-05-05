using UnityEngine;

public class QTable
{
    public const int ActionCount = 4;
    private float[,] table;
    private int gridWidth;

    public QTable(int width, int height)
    {
        gridWidth = width;
        table = new float[width * height, ActionCount];
    }

    public void Reset()
    {
        System.Array.Clear(table, 0, table.Length);
    }

    public int PositionToState(int x, int y)
    {
        return (y * gridWidth) + x;
    }

    public Vector2Int StateToPosition(int state)
    {
        return new Vector2Int(state % gridWidth, state / gridWidth);
    }

    public float GetQ(int state, int action)
    {
        return table[state, action];
    }

    public void SetQ(int state, int action, float value)
    {
        table[state, action] = value;
    }

    public float GetMaxQ(int state)
    {
        float max = float.MinValue;
        for (int a = 0; a < ActionCount; a++)
        {
            if (table[state, a] > max) max = table[state, a];
        }
        return max;
    }

    public int GetBestAction(int state)
    {
        float max = float.MinValue;
        int best = 0;
        
        for (int a = 0; a < ActionCount; a++)
        {
            if (table[state, a] > max)
            {
                max = table[state, a];
                best = a;
            }
        }
        return best;
    }

    public static Vector2Int ActionToDirection(int action)
    {
        switch (action)
        {
            case 0: return Vector2Int.up;
            case 1: return Vector2Int.right;
            case 2: return Vector2Int.down;
            case 3: return Vector2Int.left;
            default: return Vector2Int.zero;
        }
    }
}