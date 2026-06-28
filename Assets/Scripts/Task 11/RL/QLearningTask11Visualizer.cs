using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Draws the learned path and goal cell in the Game view.
/// </summary>
public class QLearningTask11Visualizer : MonoBehaviour
{
    [Header("Display")]
    [SerializeField] private bool visible = true;
    [SerializeField] private bool showHeatmap = false;
    [SerializeField] private float pathWidth = 0.055f;
    [SerializeField] private float heatmapArrowWidth = 0.035f;
    [SerializeField] private int circleSegments = 40;

    private static readonly Color PathColor = new Color(0f, 0.9f, 1f, 0.9f);
    private static readonly Color DimPathColor = new Color(0f, 0.9f, 1f, 0.45f);
    private static readonly Color GoalFillColor = new Color(1f, 0.85f, 0f, 0.35f);
    private static readonly Color GoalOutlineColor = new Color(1f, 0.85f, 0f, 0.95f);
    private static readonly Color HeatmapArrowColor = new Color(1f, 0.95f, 0f, 0.85f);

    private QLearningEnemyController source;
    private Transform visualsRoot;
    private Transform heatmapRoot;
    private Material lineMaterial;
    private Sprite goalSprite;

    private LineRenderer pathLine;
    private LineRenderer currentWaypointRing;
    private LineRenderer goalOutline;
    private SpriteRenderer goalHighlight;
    private readonly List<SpriteRenderer> heatmapCells = new List<SpriteRenderer>();
    private readonly List<LineRenderer> heatmapArrows = new List<LineRenderer>();

    public void Initialize(QLearningEnemyController owner)
    {
        source = owner;
        EnsureVisuals();
        SetVisible(visible);
    }

    public void SetVisible(bool value)
    {
        visible = value;
        if (visualsRoot != null)
            visualsRoot.gameObject.SetActive(visible);
    }

    public void SetHeatmapVisible(bool value)
    {
        showHeatmap = value;
        if (heatmapRoot != null)
            heatmapRoot.gameObject.SetActive(showHeatmap);
    }

    public void ToggleHeatmap()
    {
        SetHeatmapVisible(!showHeatmap);
    }

    private void LateUpdate()
    {
        if (!visible || source == null)
            return;

        EnsureVisuals();
        UpdatePathVisual();
        UpdateGoalVisual();
        UpdateHeatmapVisual();
    }

    private void EnsureVisuals()
    {
        if (visualsRoot != null)
            return;

        lineMaterial = new Material(Shader.Find("Sprites/Default"));
        goalSprite = CreateGoalSprite();

        visualsRoot = new GameObject($"{gameObject.name}_QLVisuals").transform;
        visualsRoot.SetParent(transform, false);

        heatmapRoot = new GameObject("Learned Q Heatmap").transform;
        heatmapRoot.SetParent(visualsRoot, false);
        heatmapRoot.gameObject.SetActive(showHeatmap);

        pathLine = CreateLine("Learned Path", 26, pathWidth, PathColor, false);
        currentWaypointRing = CreateLine("Current Waypoint", 28, pathWidth, PathColor, true);
        goalOutline = CreateLine("Goal Cell Outline", 29, pathWidth, GoalOutlineColor, true);

        GameObject goalObject = new GameObject("Goal Cell Highlight");
        goalObject.transform.SetParent(visualsRoot, false);
        goalHighlight = goalObject.AddComponent<SpriteRenderer>();
        goalHighlight.sprite = goalSprite;
        goalHighlight.color = GoalFillColor;
        goalHighlight.sortingOrder = 25;
    }

    private void UpdatePathVisual()
    {
        IReadOnlyList<Vector3> path = source.CompletePath;
        bool canDraw = path != null && path.Count > 0;
        SetLineActive(pathLine, canDraw);
        SetLineActive(currentWaypointRing, canDraw);

        if (!canDraw)
            return;

        pathLine.positionCount = path.Count;
        for (int i = 0; i < path.Count; i++)
        {
            pathLine.SetPosition(i, WithVisualZ(path[i]));
        }

        ApplyLineColor(pathLine, PathColor);

        int currentIndex = Mathf.Clamp(source.CurrentWaypointIndex, 0, path.Count - 1);
        DrawCircle(currentWaypointRing, WithVisualZ(path[currentIndex]), 0.2f, DimPathColor);
    }

    private void UpdateGoalVisual()
    {
        QLearningTrainer trainer = QLearningTrainer.Instance;
        GridManager grid = GridManager.Instance;
        bool canDraw = trainer != null && grid != null;

        SetLineActive(goalOutline, canDraw);
        if (goalHighlight != null && goalHighlight.gameObject.activeSelf != canDraw)
            goalHighlight.gameObject.SetActive(canDraw);

        if (!canDraw)
            return;

        Vector2Int goal = trainer.CurrentGoalPosition;
        Vector3 goalWorld = WithVisualZ(grid.GridToWorld(goal.x, goal.y));
        float cellSize = grid.CellSize;

        goalHighlight.transform.position = goalWorld;
        goalHighlight.transform.rotation = Quaternion.identity;
        goalHighlight.transform.localScale = Vector3.one * (cellSize * 0.9f);

        DrawSquare(goalOutline, goalWorld, cellSize * 0.9f, GoalOutlineColor);
    }

    private void UpdateHeatmapVisual()
    {
        QLearningTrainer trainer = QLearningTrainer.Instance;
        GridManager grid = GridManager.Instance;
        bool canDraw = showHeatmap && trainer != null && trainer.IsReady && trainer.Agent != null && grid != null;

        if (heatmapRoot != null && heatmapRoot.gameObject.activeSelf != canDraw)
            heatmapRoot.gameObject.SetActive(canDraw);

        if (!canDraw)
            return;

        QTable table = trainer.Agent.Table;
        float globalMax = GetMaxLearnedValue(trainer, grid, table);
        int cellIndex = 0;
        int arrowIndex = 0;

        for (int x = 0; x < grid.Width; x++)
        {
            for (int y = 0; y < grid.Height; y++)
            {
                if (grid.grid[x, y] != CellType.Floor)
                    continue;

                Vector3 worldPosition = WithVisualZ(grid.GridToWorld(x, y));
                int state = trainer.Agent.GetCurrentStateInfo(x, y);
                float maxQ = table.GetMaxQ(state);
                int bestAction = table.GetBestAction(state);
                float intensity = Mathf.Clamp01(maxQ / globalMax);

                SpriteRenderer cell = GetHeatmapCell(cellIndex++);
                cell.gameObject.SetActive(true);
                cell.transform.position = worldPosition + Vector3.forward * 0.02f;
                cell.transform.localScale = Vector3.one * (grid.CellSize * 0.82f);
                cell.color = new Color(0f, intensity, 1f - intensity, 0.28f);

                if (maxQ > 0f)
                {
                    Vector2Int direction = QTable.ActionToDirection(bestAction);
                    Vector3 arrowEnd = worldPosition + new Vector3(direction.x, direction.y, 0f) * grid.CellSize * 0.35f;
                    LineRenderer arrow = GetHeatmapArrow(arrowIndex++);
                    arrow.gameObject.SetActive(true);
                    DrawArrow(arrow, worldPosition, WithVisualZ(arrowEnd), grid.CellSize * 0.16f, HeatmapArrowColor);
                }
            }
        }

        DisableUnusedHeatmapObjects(cellIndex, arrowIndex);
    }

    private float GetMaxLearnedValue(QLearningTrainer trainer, GridManager grid, QTable table)
    {
        float globalMax = 0.001f;
        for (int x = 0; x < grid.Width; x++)
        {
            for (int y = 0; y < grid.Height; y++)
            {
                if (grid.grid[x, y] != CellType.Floor)
                    continue;

                float maxQ = table.GetMaxQ(trainer.Agent.GetCurrentStateInfo(x, y));
                if (maxQ > globalMax)
                    globalMax = maxQ;
            }
        }

        return globalMax;
    }

    private LineRenderer CreateLine(string lineName, int sortingOrder, float width, Color color, bool loop)
    {
        GameObject lineObject = new GameObject(lineName);
        lineObject.transform.SetParent(visualsRoot, false);

        LineRenderer line = lineObject.AddComponent<LineRenderer>();
        line.material = lineMaterial;
        line.useWorldSpace = true;
        line.loop = loop;
        line.widthMultiplier = width;
        line.numCapVertices = 4;
        line.numCornerVertices = 4;
        line.sortingOrder = sortingOrder;
        ApplyLineColor(line, color);
        return line;
    }

    private SpriteRenderer GetHeatmapCell(int index)
    {
        while (heatmapCells.Count <= index)
        {
            GameObject cellObject = new GameObject($"Heatmap Cell {heatmapCells.Count + 1}");
            cellObject.transform.SetParent(heatmapRoot, false);

            SpriteRenderer cell = cellObject.AddComponent<SpriteRenderer>();
            cell.sprite = goalSprite;
            cell.sortingOrder = 18;
            heatmapCells.Add(cell);
        }

        return heatmapCells[index];
    }

    private LineRenderer GetHeatmapArrow(int index)
    {
        while (heatmapArrows.Count <= index)
        {
            LineRenderer arrow = CreateLine($"Heatmap Direction {heatmapArrows.Count + 1}", 19, heatmapArrowWidth, HeatmapArrowColor, false);
            arrow.transform.SetParent(heatmapRoot, false);
            heatmapArrows.Add(arrow);
        }

        return heatmapArrows[index];
    }

    private void DisableUnusedHeatmapObjects(int usedCells, int usedArrows)
    {
        for (int i = usedCells; i < heatmapCells.Count; i++)
            heatmapCells[i].gameObject.SetActive(false);

        for (int i = usedArrows; i < heatmapArrows.Count; i++)
            heatmapArrows[i].gameObject.SetActive(false);
    }

    private Sprite CreateGoalSprite()
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
    }

    private void DrawCircle(LineRenderer line, Vector3 center, float radius, Color color)
    {
        int segments = Mathf.Max(12, circleSegments);
        line.positionCount = segments;

        for (int i = 0; i < segments; i++)
        {
            float angle = (i / (float)segments) * Mathf.PI * 2f;
            Vector3 point = center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
            line.SetPosition(i, point);
        }

        ApplyLineColor(line, color);
    }

    private void DrawSquare(LineRenderer line, Vector3 center, float size, Color color)
    {
        float half = size * 0.5f;
        line.positionCount = 4;
        line.SetPosition(0, center + new Vector3(-half, -half, 0f));
        line.SetPosition(1, center + new Vector3(-half, half, 0f));
        line.SetPosition(2, center + new Vector3(half, half, 0f));
        line.SetPosition(3, center + new Vector3(half, -half, 0f));
        ApplyLineColor(line, color);
    }

    private void DrawLine(LineRenderer line, Vector3 from, Vector3 to, Color color)
    {
        line.positionCount = 2;
        line.SetPosition(0, from);
        line.SetPosition(1, to);
        ApplyLineColor(line, color);
    }

    private void DrawArrow(LineRenderer line, Vector3 from, Vector3 to, float headLength, Color color)
    {
        Vector3 direction = (to - from).normalized;
        if (direction == Vector3.zero)
            direction = Vector3.up;

        Vector3 leftHead = Quaternion.Euler(0f, 0f, 145f) * direction * headLength;
        Vector3 rightHead = Quaternion.Euler(0f, 0f, -145f) * direction * headLength;

        line.positionCount = 5;
        line.SetPosition(0, from);
        line.SetPosition(1, to);
        line.SetPosition(2, to + leftHead);
        line.SetPosition(3, to);
        line.SetPosition(4, to + rightHead);
        ApplyLineColor(line, color);
    }

    private void ApplyLineColor(LineRenderer line, Color color)
    {
        line.startColor = color;
        line.endColor = color;
    }

    private void SetLineActive(LineRenderer line, bool active)
    {
        if (line != null && line.gameObject.activeSelf != active)
            line.gameObject.SetActive(active);
    }

    private Vector3 WithVisualZ(Vector3 position)
    {
        position.z = -0.05f;
        return position;
    }
}
