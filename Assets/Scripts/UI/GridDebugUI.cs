using UnityEngine;

public class GridDebugUI : MonoBehaviour
{
    [SerializeField] private GridPlayerController player;

    private void OnGUI()
    {
        if (player == null) return;

        Vector2Int pos = player.GetGridPosition();

        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 20;
        style.normal.textColor = Color.white;

        GUI.Label(new Rect(10, 10, 400, 30), $"Grid Position: ({pos.x}, {pos.y})", style);
        GUI.Label(new Rect(10, 40, 400, 30), $"Move: WASD / Arrow Keys", style);
    }
}