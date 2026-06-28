using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Draws Behaviour Tree state in the Game view during Play Mode.
/// </summary>
public class EnemyBTVisualizer : MonoBehaviour
{
    [Header("Display")]
    [SerializeField] private bool visible = true;
    [SerializeField] private float lineWidth = 0.045f;
    [SerializeField] private int circleSegments = 72;
    [SerializeField] private float labelScale = 0.9f;
    [SerializeField] private float labelHeight = 1.35f;

    private static readonly Color ChaseColor = new Color(1f, 0.62f, 0f, 0.85f);
    private static readonly Color FleeColor = new Color(1f, 0.12f, 0.08f, 0.9f);
    private static readonly Color PatrolColor = new Color(0f, 0.85f, 1f, 0.85f);
    private static readonly Color InvestigateColor = new Color(1f, 0.85f, 0f, 0.9f);
    private static readonly Color ClearSightColor = new Color(0.15f, 1f, 0.25f, 0.8f);
    private static readonly Color BlockedSightColor = new Color(1f, 0f, 1f, 0.8f);

    private EnemyBT source;
    private Transform visualsRoot;
    private Material lineMaterial;
    private TMP_FontAsset labelFont;

    private LineRenderer chaseRangeRing;
    private LineRenderer fleeRangeRing;
    private LineRenderer searchRangeRing;
    private LineRenderer sightLine;
    private LineRenderer pathLine;
    private LineRenderer playerLine;
    private LineRenderer lastKnownHorizontal;
    private LineRenderer lastKnownVertical;
    private LineRenderer currentNodeRing;
    private LineRenderer destinationRing;
    private LineRenderer stateHalo;

    private TextMeshPro stateLabel;
    private TextMeshPro sightLabel;
    private TextMeshPro pathLabel;
    private TextMeshPro searchLabel;

    public void Initialize(EnemyBT owner)
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

    private void LateUpdate()
    {
        if (!visible || source == null)
            return;

        EnsureVisuals();
        UpdateVisuals();
    }

    private void EnsureVisuals()
    {
        if (visualsRoot != null)
            return;

        lineMaterial = new Material(Shader.Find("Sprites/Default"));
        labelFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

        visualsRoot = new GameObject($"{gameObject.name}_BTVisuals").transform;
        visualsRoot.SetParent(transform, false);

        chaseRangeRing = CreateLine("Chase Range", 24, lineWidth, ChaseColor, true);
        fleeRangeRing = CreateLine("Flee Range", 22, lineWidth, FleeColor, true);
        searchRangeRing = CreateLine("Search Area", 23, lineWidth, InvestigateColor, true);
        sightLine = CreateLine("Line Of Sight", 26, lineWidth * 1.2f, ClearSightColor, false);
        pathLine = CreateLine("Active Path", 25, lineWidth * 1.4f, PatrolColor, false);
        playerLine = CreateLine("Player Link", 24, lineWidth, ChaseColor, false);
        lastKnownHorizontal = CreateLine("Last Known Horizontal", 27, lineWidth * 1.2f, InvestigateColor, false);
        lastKnownVertical = CreateLine("Last Known Vertical", 27, lineWidth * 1.2f, InvestigateColor, false);
        currentNodeRing = CreateLine("Current Path Node", 28, lineWidth, PatrolColor, true);
        destinationRing = CreateLine("Path Destination", 28, lineWidth, PatrolColor, true);
        stateHalo = CreateLine("State Halo", 29, lineWidth * 1.5f, PatrolColor, true);

        stateLabel = CreateLabel("State Label", 32);
        sightLabel = CreateLabel("Sight Label", 31);
        pathLabel = CreateLabel("Path Label", 31);
        searchLabel = CreateLabel("Search Label", 31);
    }

    private void UpdateVisuals()
    {
        Vector3 enemyPosition = WithVisualZ(transform.position);
        Color stateColor = source.CurrentStateColor;

        DrawCircle(chaseRangeRing, enemyPosition, source.ChaseRange, ChaseColor);
        DrawCircle(fleeRangeRing, enemyPosition, source.FleeRange, FleeColor);
        DrawCircle(stateHalo, enemyPosition, 0.38f, stateColor);

        UpdateStateLabel(enemyPosition, stateColor);
        UpdateSightVisual(enemyPosition);
        UpdateLastKnownVisual();
        UpdatePathVisual(stateColor);
        UpdateSearchVisual();
    }

    private void UpdateStateLabel(Vector3 enemyPosition, Color stateColor)
    {
        stateLabel.text = source.CurrentStateName;
        stateLabel.color = stateColor;
        PositionLabel(stateLabel, enemyPosition + Vector3.up * labelHeight);
    }

    private void UpdateSightVisual(Vector3 enemyPosition)
    {
        Transform player = source.Player;
        bool hasPlayer = player != null;
        bool inRange = hasPlayer && Vector2.Distance(transform.position, player.position) <= source.ChaseRange;
        SetLineActive(sightLine, inRange);
        SetTextActive(sightLabel, inRange);
        SetLineActive(playerLine, hasPlayer && (source.CurrentStateName == "Chase" || source.CurrentStateName == "Flee"));

        if (!hasPlayer)
            return;

        Vector3 playerPosition = WithVisualZ(player.position);
        Color sightColor = source.HasLineOfSight ? ClearSightColor : BlockedSightColor;

        if (inRange)
        {
            DrawLine(sightLine, enemyPosition, playerPosition, sightColor);
            sightLabel.text = source.HasLineOfSight ? "LOS clear" : "LOS blocked";
            sightLabel.color = sightColor;
            PositionLabel(sightLabel, Vector3.Lerp(enemyPosition, playerPosition, 0.5f) + Vector3.up * 0.35f);
        }

        if (source.CurrentStateName == "Chase")
            DrawLine(playerLine, enemyPosition, playerPosition, ChaseColor);
        else if (source.CurrentStateName == "Flee")
            DrawLine(playerLine, enemyPosition, playerPosition, FleeColor);
    }

    private void UpdateLastKnownVisual()
    {
        bool hasLastKnown = source.HasLastKnownLocation;
        SetLineActive(lastKnownHorizontal, hasLastKnown);
        SetLineActive(lastKnownVertical, hasLastKnown);

        if (!hasLastKnown)
            return;

        Vector3 center = WithVisualZ(source.LastKnownPlayerPosition);
        float size = 0.28f;
        DrawLine(lastKnownHorizontal, center + Vector3.left * size, center + Vector3.right * size, InvestigateColor);
        DrawLine(lastKnownVertical, center + Vector3.down * size, center + Vector3.up * size, InvestigateColor);
    }

    private void UpdatePathVisual(Color stateColor)
    {
        PathFollower follower = source.ActivePathFollower;
        bool hasPath = follower != null && follower.CurrentPath != null && follower.CurrentPath.Count > 0;
        GridManager grid = GridManager.Instance;
        bool canDraw = hasPath && grid != null;

        SetLineActive(pathLine, canDraw);
        SetLineActive(currentNodeRing, canDraw);
        SetLineActive(destinationRing, canDraw);
        SetTextActive(pathLabel, canDraw);

        if (!canDraw)
            return;

        List<Vector2Int> path = follower.CurrentPath;
        int startIndex = Mathf.Clamp(follower.CurrentIndex, 0, path.Count - 1);
        List<Vector3> points = new List<Vector3> { WithVisualZ(transform.position) };

        for (int i = startIndex; i < path.Count; i++)
        {
            points.Add(WithVisualZ(grid.GridToWorld(path[i].x, path[i].y)));
        }

        pathLine.positionCount = points.Count;
        pathLine.SetPositions(points.ToArray());
        ApplyLineColor(pathLine, stateColor);

        Vector3 currentNode = WithVisualZ(grid.GridToWorld(path[startIndex].x, path[startIndex].y));
        Vector2Int finalCell = path[path.Count - 1];
        Vector3 destination = WithVisualZ(grid.GridToWorld(finalCell.x, finalCell.y));

        DrawCircle(currentNodeRing, currentNode, 0.18f, stateColor);
        DrawCircle(destinationRing, destination, 0.28f, stateColor);

        pathLabel.text = $"{source.CurrentStateName} path";
        pathLabel.color = stateColor;
        PositionLabel(pathLabel, destination + Vector3.up * 0.45f);
    }

    private void UpdateSearchVisual()
    {
        bool showSearch = source.HasLastKnownLocation && source.CurrentStateName == "Investigate";
        SetLineActive(searchRangeRing, showSearch);
        SetTextActive(searchLabel, showSearch);

        if (!showSearch)
            return;

        Vector3 center = WithVisualZ(source.LastKnownPlayerPosition);
        DrawCircle(searchRangeRing, center, source.SearchRadius, InvestigateColor);

        searchLabel.text = "Search area";
        searchLabel.color = InvestigateColor;
        PositionLabel(searchLabel, center + Vector3.up * (source.SearchRadius + 0.35f));
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

    private TextMeshPro CreateLabel(string labelName, int sortingOrder)
    {
        GameObject labelObject = new GameObject(labelName);
        labelObject.transform.SetParent(visualsRoot, false);

        TextMeshPro label = labelObject.AddComponent<TextMeshPro>();
        if (labelFont != null)
            label.font = labelFont;

        label.alignment = TextAlignmentOptions.Center;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.fontSize = 4f;
        label.fontStyle = FontStyles.Bold;
        label.transform.localScale = Vector3.one * labelScale;
        label.rectTransform.sizeDelta = new Vector2(10f, 2.5f);

        MeshRenderer renderer = label.GetComponent<MeshRenderer>();
        renderer.sortingOrder = 200 + sortingOrder;
        return label;
    }

    private void PositionLabel(TextMeshPro label, Vector3 position)
    {
        label.transform.position = WithVisualZ(position);
        label.transform.rotation = Quaternion.identity;
        label.ForceMeshUpdate();
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

    private void DrawLine(LineRenderer line, Vector3 from, Vector3 to, Color color)
    {
        line.positionCount = 2;
        line.SetPosition(0, from);
        line.SetPosition(1, to);
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

    private void SetTextActive(TextMeshPro label, bool active)
    {
        if (label != null && label.gameObject.activeSelf != active)
            label.gameObject.SetActive(active);
    }

    private Vector3 WithVisualZ(Vector3 position)
    {
        position.z = -0.05f;
        return position;
    }
}
