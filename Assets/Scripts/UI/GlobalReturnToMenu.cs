using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

public class GlobalReturnToMenu : MonoBehaviour
{
    [Header("Scene Settings")]
    [SerializeField] private string menuSceneName = "TaskMenu";

    [Header("Return Button Settings")]
    [SerializeField] private bool showReturnButton = true;
    [SerializeField] private bool allowEscapeKeyReturn = true;
    [SerializeField] public TMP_FontAsset customFont;

    private static GlobalReturnToMenu instance;
    
    private Canvas returnCanvas;
    private Button returnButton;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        CreateReturnButton();
        SceneManager.sceneLoaded += OnSceneLoaded;

        UpdateButtonVisibility(SceneManager.GetActiveScene().name);
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            instance = null;
        }
    }

    private void Update()
    {
        if (!allowEscapeKeyReturn)
        {
            return;
        }

        if (Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            LoadMenuScene();
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        CreateEventSystemIfMissing();
        UpdateButtonVisibility(scene.name);
    }

    private void CreateReturnButton()
    {
        GameObject canvasObject = new GameObject("Global Return Menu Canvas");
        canvasObject.transform.SetParent(transform);

        returnCanvas = canvasObject.AddComponent<Canvas>();
        returnCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        returnCanvas.sortingOrder = 999;

        CanvasScaler canvasScaler = canvasObject.AddComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1920, 1080);

        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject buttonObject = new GameObject("Return To Menu Button");
        buttonObject.transform.SetParent(canvasObject.transform);

        RectTransform buttonRect = buttonObject.AddComponent<RectTransform>();

        // Bottom-left anchor
        buttonRect.anchorMin = new Vector2(0, 0);
        buttonRect.anchorMax = new Vector2(0, 0);
        buttonRect.pivot = new Vector2(0, 0);

        // 20 px from left, 20 px from bottom
        buttonRect.anchoredPosition = new Vector2(20, 20);
        buttonRect.sizeDelta = new Vector2(170, 50);

        Image buttonImage = buttonObject.AddComponent<Image>();
        buttonImage.color = new Color32(0, 0, 0, 90);

        returnButton = buttonObject.AddComponent<Button>();
        returnButton.onClick.AddListener(LoadMenuScene);

        GameObject textObject = new GameObject("Text");
        textObject.transform.SetParent(buttonObject.transform);

        RectTransform textRect = textObject.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI buttonText = textObject.AddComponent<TextMeshProUGUI>();
        buttonText.text = "Task Select";
        buttonText.fontSize = 28;
        buttonText.alignment = TextAlignmentOptions.Center;
        ColorUtility.TryParseHtmlString("#958CD6", out Color customColor);
        buttonText.color = customColor;
        
        if (customFont != null)
        {
            buttonText.font = customFont;
        }

        CreateEventSystemIfMissing();
    }

    private void CreateEventSystemIfMissing()
    {
        EventSystem existingEventSystem = FindFirstObjectByType<EventSystem>();

        if (existingEventSystem != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<InputSystemUIInputModule>();
    }

    private void UpdateButtonVisibility(string currentSceneName)
    {
        if (returnCanvas == null)
        {
            return;
        }

        bool isInMenuScene = currentSceneName == menuSceneName;
        returnCanvas.gameObject.SetActive(showReturnButton && !isInMenuScene);
    }

    public void LoadMenuScene()
    {
        if (SceneManager.GetActiveScene().name == menuSceneName)
        {
            return;
        }

        SceneManager.LoadScene(menuSceneName);
    }
}