using System;
using UnityEngine;

/// <summary>
/// The Q-Table: a massive spreadsheet where rows represent states (grid positions)
/// and columns represent actions the agent can take.
/// 
/// Each cell stores a Q-value: the expected future reward of taking that action
/// from that state. Higher values mean the agent has learned that action is good.
/// 
/// State encoding: state = y * gridWidth + x  (flattened 2D grid index)
/// Actions: Up, Down, Left, Right (4 cardinal directions on the grid)
/// </summary>
public class QTable
{
    /// <summary>
    /// The four cardinal actions the agent can take on the grid.
    /// </summary>
    public enum Action
    {
        Up = 0,
        Down = 1,
        Left = 2,
        Right = 3
    }

    public const int ActionCount = 4;

    /// <summary>
    /// The Q-value table. Indexed as [stateIndex, actionIndex].
    /// </summary>
    private float[,] table;

    /// <summary>
    /// Grid dimensions used for state encoding.
    /// </summary>
    public int GridWidth { get; private set; }
    public int GridHeight { get; private set; }
    public int StateCount { get; private set; }

    /// <summary>
    /// Initializes the Q-Table with all values set to zero.
    /// Rows = total grid cells (width * height), Columns = 4 actions.
    /// </summary>
    public QTable(int gridWidth, int gridHeight)
    {
        GridWidth = gridWidth;
        GridHeight = gridHeight;
        StateCount = gridWidth * gridHeight;
        table = new float[StateCount, ActionCount];
    }

    /// <summary>
    /// Converts a grid position to a flat state index.
    /// </summary>
    public int PositionToState(int x, int y)
    {
        return y * GridWidth + x;
    }

    /// <summary>
    /// Converts a flat state index back to grid coordinates.
    /// </summary>
    public Vector2Int StateToPosition(int state)
    {
        int x = state % GridWidth;
        int y = state / GridWidth;
        return new Vector2Int(x, y);
    }

    /// <summary>
    /// Gets the Q-value for a specific state-action pair.
    /// </summary>
    public float GetQ(int state, int action)
    {
        return table[state, action];
    }

    /// <summary>
    /// Sets the Q-value for a specific state-action pair.
    /// </summary>
    public void SetQ(int state, int action, float value)
    {
        table[state, action] = value;
    }

    /// <summary>
    /// Gets the Q-value for a grid position and action.
    /// </summary>
    public float GetQ(int x, int y, Action action)
    {
        return table[PositionToState(x, y), (int)action];
    }

    /// <summary>
    /// Returns the action with the highest Q-value for the given state.
    /// If multiple actions are tied, returns the first one found.
    /// </summary>
    public int GetBestAction(int state)
    {
        int best = 0;
        float bestValue = table[state, 0];
        for (int a = 1; a < ActionCount; a++)
        {
            if (table[state, a] > bestValue)
            {
                bestValue = table[state, a];
                best = a;
            }
        }
        return best;
    }

    /// <summary>
    /// Returns the maximum Q-value across all actions for the given state.
    /// Used in the Bellman equation to estimate future reward.
    /// </summary>
    public float GetMaxQ(int state)
    {
        float max = table[state, 0];
        for (int a = 1; a < ActionCount; a++)
        {
            if (table[state, a] > max)
                max = table[state, a];
        }
        return max;
    }

    /// <summary>
    /// Returns the direction vector for the given action.
    /// </summary>
    public static Vector2Int ActionToDirection(int action)
    {
        switch ((Action)action)
        {
            case Action.Up:    return Vector2Int.up;
            case Action.Down:  return Vector2Int.down;
            case Action.Left:  return Vector2Int.left;
            case Action.Right: return Vector2Int.right;
            default:           return Vector2Int.zero;
        }
    }

    /// <summary>
    /// Resets all Q-values to zero.
    /// </summary>
    public void Reset()
    {
        Array.Clear(table, 0, table.Length);
    }

    /// <summary>
    /// Returns a formatted string of the Q-values for a specific state (for debugging).
    /// </summary>
    public string DebugState(int state)
    {
        Vector2Int pos = StateToPosition(state);
        return $"State ({pos.x},{pos.y}): " +
               $"Up={table[state, 0]:F2} " +
               $"Down={table[state, 1]:F2} " +
               $"Left={table[state, 2]:F2} " +
               $"Right={table[state, 3]:F2}";
    }
}
