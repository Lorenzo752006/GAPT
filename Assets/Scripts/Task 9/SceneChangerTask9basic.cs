using UnityEngine;
using UnityEngine.SceneManagement; 

public class SceneChangerTask9basic : MonoBehaviour
{
    public void MoveToScene(string sceneName)
    {
        // from task9basic to task9complex
        SceneManager.LoadScene("Task9Complex");
    }
}