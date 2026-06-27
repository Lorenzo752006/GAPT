using UnityEngine;
using UnityEngine.SceneManagement;

public class ResetButtonScene : MonoBehaviour
{
    public void ResetCurrentScene()
    {
        // Gets the active scene's name and reloads it
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
