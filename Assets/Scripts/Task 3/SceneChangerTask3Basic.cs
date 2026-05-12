using UnityEngine;
using UnityEngine.SceneManagement;


public class SceneChangerTask3Basic : MonoBehaviour
{
    
public void MoveToScene(string sceneName)
    {
        // from task3basic to task3complex
        SceneManager.LoadScene("Task3Complex");
    }
}