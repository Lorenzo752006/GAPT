using UnityEngine;
using UnityEngine.SceneManagement; 

public class SceneChangerTask12complex : MonoBehaviour
{
    public void MoveToScene(string sceneName)
    {
        // from task12complex to task12basic
        SceneManager.LoadScene("Task12Basic");
    }
}