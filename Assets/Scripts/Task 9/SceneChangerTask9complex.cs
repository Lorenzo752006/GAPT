using UnityEngine;
using UnityEngine.SceneManagement; 

public class SceneChangerTask9complex : MonoBehaviour
{
    public void MoveToScene(string sceneName)
    {
        // from task9complex to task9basic
        SceneManager.LoadScene("Task9Basic");
    }
}