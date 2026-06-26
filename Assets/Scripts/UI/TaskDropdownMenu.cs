using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TaskSceneDropdownMenu : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Dropdown taskDropdown;
    [SerializeField] private Button enterButton;

    [Header("Scene Names")]
    [SerializeField] private List<TaskScene> taskScenes = new List<TaskScene>();

    [System.Serializable]
    public class TaskScene
    {
        public string displayName;
        public string sceneName;
    }

    private void Start()
    {
        SetupDropdown();

        if (enterButton != null)
        {
            enterButton.onClick.AddListener(LoadSelectedTaskScene);
        }
        else
        {
            Debug.LogError("Enter Button is not assigned.");
        }
    }

    private void SetupDropdown()
    {
        if (taskDropdown == null)
        {
            Debug.LogError("Task Dropdown is not assigned.");
            return;
        }

        taskDropdown.ClearOptions();

        List<string> dropdownOptions = new List<string>();

        foreach (TaskScene taskScene in taskScenes)
        {
            dropdownOptions.Add(taskScene.displayName);
        }

        taskDropdown.AddOptions(dropdownOptions);
        taskDropdown.value = 0;
        taskDropdown.RefreshShownValue();
    }

    public void LoadSelectedTaskScene()
    {
        if (taskDropdown == null)
        {
            Debug.LogError("Task Dropdown is not assigned.");
            return;
        }

        if (taskScenes.Count == 0)
        {
            Debug.LogError("No task scenes have been added.");
            return;
        }

        int selectedIndex = taskDropdown.value;

        if (selectedIndex < 0 || selectedIndex >= taskScenes.Count)
        {
            Debug.LogError("Invalid dropdown selection.");
            return;
        }

        string sceneToLoad = taskScenes[selectedIndex].sceneName;

        if (string.IsNullOrWhiteSpace(sceneToLoad))
        {
            Debug.LogError("Scene name is empty.");
            return;
        }

        SceneManager.LoadScene(sceneToLoad);
    }
}