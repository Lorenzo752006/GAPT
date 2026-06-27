using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChangerTask3Complex : MonoBehaviour
{
public void MoveToScene(string sceneName)
    {
        // from task3complex to task3basic
        SceneManager.LoadScene("Task3Basic");
    }
}
