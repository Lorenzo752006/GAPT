using UnityEngine;
using UnityEngine.SceneManagement; 

public class SceneChangerTask12basic : MonoBehaviour
{
    public void MoveToScene(string sceneName)
    {
        // from task12basic to task12complex
        SceneManager.LoadScene("Task12Complex");
    }
}