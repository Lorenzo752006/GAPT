using UnityEngine;

public class GoblinActions : MonoBehaviour
{
    [Header("Mood Colors")]
    public Color attackColor = new Color(1f, 0.3f, 0.3f);
    public Color danceColor = new Color(0.3f, 1f, 0.3f);
    public Color ponderColor = new Color(0.3f, 0.6f, 1f);
    public Color32 defaultColor = new Color32(49, 77, 121, 255);

    private Camera ActiveCamera => Camera.main;

    void Start()
    {
        SetupCamera();
    }

    void SetupCamera()
    {
        var cam = ActiveCamera;
        if (cam == null)
        {
            Debug.LogWarning("GoblinActions: No camera found in this scene.");
            return;
        }
        cam.clearFlags = CameraClearFlags.SolidColor;
    }

    public void PerformAttack()
    {
        Debug.Log("Goblin: *Screeches and swings!*");
        var cam = ActiveCamera;
        if (cam != null) cam.backgroundColor = attackColor;
    }

    public void PerformDance()
    {
        Debug.Log("Goblin: *Doing a jig!*");
        var cam = ActiveCamera;
        if (cam != null) cam.backgroundColor = danceColor;
    }

    public void PerformPonder()
    {
        Debug.Log("Goblin: Hmm... very curious...");
        var cam = ActiveCamera;
        if (cam != null) cam.backgroundColor = ponderColor;
    }

    public void DoNothing()
    {
        Debug.Log("Goblin: Just standing there, looking nervous.");
        var cam = ActiveCamera;
        if (cam != null) cam.backgroundColor = defaultColor;
    }
}