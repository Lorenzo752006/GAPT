using UnityEngine;

public class GoblinActions : MonoBehaviour
{
    [Header("Main Camera")] 
    public Camera mainCamera;

    [Header("Mood Colors")]
    public Color attackColor = new Color(1f, 0.3f, 0.3f);    // Soft Red
    public Color danceColor = new Color(0.3f, 1f, 0.3f);     // Soft Green
    public Color ponderColor = new Color(0.3f, 0.6f, 1f);    // Soft Blue
    public Color32 defaultColor = new Color32(49, 77, 121, 255);

    void Start()
    {
        // Automatically find the camera if not assigned
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        // Ensure the camera clears to a solid color at the start
        mainCamera.clearFlags = CameraClearFlags.SolidColor;
    }

    public void PerformAttack()
    {
        Debug.Log("Goblin: *Screeches and swings!*");
        mainCamera.backgroundColor = attackColor;
    }

    public void PerformDance()
    {
        Debug.Log("Goblin: *Doing a jig!*");
        mainCamera.backgroundColor = danceColor;
    }

    public void PerformPonder()
    {
        Debug.Log("Goblin: Hmm... very curious...");
        mainCamera.backgroundColor = ponderColor;
    }

    public void DoNothing()
    {
        Debug.Log("Goblin: Just standing there, looking nervous.");
        mainCamera.backgroundColor = defaultColor;
    }
}