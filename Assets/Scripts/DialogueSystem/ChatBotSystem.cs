using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using System.Collections.Generic;
using TMPro;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using UnityEngine.Events;
using System.Linq;
using System.IO;
using System;
using UnityEngine.SceneManagement;

public class ChatBotSystem_Test : MonoBehaviour 
{
    [Header("Ollama Settings")]
    private string ollamaUrl = "http://localhost:11434/api/chat";
    public string modelToUse = "llama3:latest";

    [Header("Scene Persistence")]
    [Tooltip("The chatbot will persist across these scenes. It will be destroyed when entering any scene NOT in this list.")]
    public string[] persistInScenes = { "Scene1", "Scene2" };

    [Header("Scene History Settings")]
    public int defaultMaxHistory = 10;
    public SceneHistoryOverride[] historyOverrides;

    [System.Serializable]
    public class SceneHistoryOverride
    {
        public string sceneName;
        public int maxHistory;
    }

    [Header("Conversation")]
    public TMP_InputField playerInputField;
    public TMP_Text ChatBotOutput;
    public int maxHistory = 10;
    private bool isThinking = false;
    public List<Message> conversationHistory = new List<Message>();

    [System.Serializable]
    public class ChatBotUnityAction
    {
        public string validAction;
        public UnityEvent OnAction;
    }
    public ChatBotUnityAction[] VALID_ACTIONS;
    private string actionsList;

    private Process _ollamaProcess;

    // --- Singleton ---
    private static ChatBotSystem_Test _instance;

    // --- Data Classes ---
    [System.Serializable]
    public class Message 
    {
        public string role;
        public string content;
        public Message(string r, string c) { role = r; content = c; }
    }

    [System.Serializable]
    public class OllamaChatResponse 
    {
        public string model;   
        public Message message; 
        public bool done;      
    }

    [System.Serializable]
    public class OllamaRequest 
    {
        public string model;
        public List<Message> messages;
        public bool stream = false;
        public string format;

        public OllamaRequest(List<Message> history, string modelUsed)
        {
            messages = (history != null) ? new List<Message>(history) : new List<Message>();
            model = modelUsed;
            format = "json";
        }
    }

    [System.Serializable]
    public class ChatbotReturn 
    {
        public string dialogue;
        public string action;
    }

    // --- Unity Lifecycle ---
    void Awake()
    {
        // --- Singleton + DontDestroyOnLoad ---
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;

        conversationHistory.Clear();

        if (VALID_ACTIONS != null && VALID_ACTIONS.Length > 0)
            actionsList = string.Join(", ", VALID_ACTIONS.Select(a => a.validAction));
        else
            actionsList = "NONE";

        string systemPrompt = @"
        You are a goblin.
        You must choose an action based on the user's message.
        Rules:
        - Use 'ATTACK' ONLY if the user directly threatens your life or physically harms you.
        - Use 'DANCE' when the user is playful, flirty, happy, or asks you to dance or celebrate.
        - Use 'PONDER' ONLY if the user asks a question, expresses confusion, or presents a complex idea. 
        - Use 'NONE' ONLY for simple statements of fact, greetings, or silence where no response is required.

        Conflict Resolution:
        - If a message is both neutral and a question, you MUST use 'PONDER'.
        - If a message contains no questions and no emotional cues, you MUST use 'NONE'.
        - Do NOT use 'PONDER' for simple greetings like 'Hello'.

        Always respond with ONLY a JSON object. No extra text.
        Use exactly these keys: ""dialogue"" and ""action"" (PONDER, DANCE, NONE, ATTACK).

        Example:
        {""dialogue"": ""U-um... should I dance now...? "", ""action"": ""DANCE""}";

        conversationHistory.Add(new Message("system", systemPrompt));
    }



    void SetMaxHistoryForScene(string sceneName)
    {
        // Check if there's an override for this scene
        if (historyOverrides != null)
        {
            foreach (var overrideData in historyOverrides)
            {
                if (overrideData.sceneName == sceneName)
                {
                    maxHistory = overrideData.maxHistory;
                    Debug.Log($"ChatBotSystem: Set maxHistory to {maxHistory} for scene '{sceneName}' (override).");
                    return;
                }
            }
        }
        
        // Use default if no override
        maxHistory = defaultMaxHistory;
        Debug.Log($"ChatBotSystem: Set maxHistory to {maxHistory} for scene '{sceneName}' (default).");
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        bool shouldPersist = persistInScenes != null &&
                            persistInScenes.Any(s => s == scene.name);

        if (!shouldPersist)
        {
            Debug.Log($"ChatBotSystem: Scene '{scene.name}' is not in the persist list. Shutting down.");
            SceneManager.sceneLoaded -= OnSceneLoaded;
            StopOllama();
            _instance = null;
            Destroy(gameObject);
        }
        else
        {
            Debug.Log($"ChatBotSystem: Persisting into scene '{scene.name}'. Re-scanning for UI references...");
            SetMaxHistoryForScene(scene.name);
            ClearConversationHistory();
            RefreshUIReferences();
            ReconnectInputFieldEvents();
        }
    }

    public void ClearConversationHistory()
    {
        // Keep ONLY the system prompt, remove everything else
        if (conversationHistory.Count > 0)
        {
            // Find the system prompt
            var systemPrompt = conversationHistory.FirstOrDefault(m => m.role == "system");
            
            // Clear everything
            conversationHistory.Clear();
            
            // Re-add system prompt if it existed
            if (systemPrompt != null)
            {
                conversationHistory.Add(systemPrompt);
                Debug.Log("ChatBotSystem: History cleared (system prompt preserved).");
            }
            else
            {
                Debug.Log("ChatBotSystem: History cleared (no system prompt found).");
            }
        }
        else
        {
            Debug.Log("ChatBotSystem: History was already empty.");
        }
    }

    void RefreshUIReferences()
    {

        // Try to find input, but don't care if it's missing
        GameObject inputObj = GameObject.FindWithTag("ChatBotInput");
        if (inputObj != null)
        {
            playerInputField = inputObj.GetComponent<TMP_InputField>();
            Debug.Log("ChatBotSystem: Found player input field.");
        }
        else
        {
            playerInputField = FindAnyObjectByType<TMP_InputField>();
            if (playerInputField != null)
                Debug.Log("ChatBotSystem: Found player input field.");
            // SILENTLY IGNORE if not found - no warning
        }

        // Try to find output, but don't care if it's missing
        GameObject outputObj = GameObject.FindWithTag("ChatBotOutput");
        if (outputObj != null)
        {
            ChatBotOutput = outputObj.GetComponent<TMP_Text>();
            Debug.Log("ChatBotSystem: Found chatbot output text.");
        }
        else
        {
            ChatBotOutput = FindObjectsByType<TMP_Text>(FindObjectsSortMode.None)
                .FirstOrDefault(t => t.GetComponentInParent<TMP_InputField>() == null);
            if (ChatBotOutput != null)
                Debug.Log("ChatBotSystem: Found chatbot output text.");
            // SILENTLY IGNORE if not found - no warning
        }
    }

    void Start()
    {
        StartCoroutine(UniversalBootSequence());
    }

    IEnumerator UniversalBootSequence()
    {
        Debug.Log("System: Launching Ollama Service...");
        LaunchOllamaProcess();
        yield return StartCoroutine(WaitForOllama());

        Debug.Log("System: Warming up GPU VRAM...");
        
        string preloadJson = "{\"model\":\"" + modelToUse + "\"}";
        
        using (UnityWebRequest request = new UnityWebRequest(ollamaUrl.Replace("/chat", "/generate"), "POST")) 
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(preloadJson);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = 120;

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success) 
            {
                Debug.Log("Preload Complete! Model is hot in GPU. Ready for instant player input.");
            } 
            else 
            {
                Debug.LogError("Preload failed. Check if Ollama is running properly.");
            }
        }
    }

    IEnumerator WaitForOllama()
    {
        while (true)
        {
            UnityWebRequest req = UnityWebRequest.Get("http://localhost:11434");
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
                break;

            yield return new WaitForSeconds(0.5f);
        }
    }

    private static bool ollamaAlreadyStarted = false;
    
    void LaunchOllamaProcess()
    {
        // FIRST: Check if we already have a valid process
        if (_ollamaProcess != null && !_ollamaProcess.HasExited)
        {
            Debug.Log($"Ollama already running with PID: {_ollamaProcess.Id}");
            ollamaAlreadyStarted = true;
            return;
        }

        // SECOND: Check for existing processes
        var ollamaProcesses = Process.GetProcessesByName("ollama");
        var llamaProcesses = Process.GetProcessesByName("llama-server");
        
        if (ollamaProcesses.Length > 0)
        {
            Debug.Log($"Found existing Ollama process PID: {ollamaProcesses[0].Id}");
            _ollamaProcess = ollamaProcesses[0]; // STORE the process!
            ollamaAlreadyStarted = true;
            return;
        }
        
        if (llamaProcesses.Length > 0)
        {
            Debug.Log($"Found llama-server running. Ollama is running.");
            ollamaAlreadyStarted = true;
            // We can't get the parent ollama.exe, but that's OK
            return;
        }

        if (ollamaAlreadyStarted)
        {
            Debug.Log("Ollama already started by this Unity session.");
            return;
        }

        // THIRD: Launch new process
        try
        {
            ProcessStartInfo psi = new ProcessStartInfo();
            #if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
                psi.FileName = "ollama.exe";
            #else
                psi.FileName = "ollama";
            #endif

            psi.Arguments = "serve";
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            _ollamaProcess = Process.Start(psi);

            if (_ollamaProcess != null)
            {
                Debug.Log($"Ollama launched. PID: {_ollamaProcess.Id}");
                ollamaAlreadyStarted = true;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Universal Launch Failed: " + e.Message);
        }
    }

    public void StopOllama()
    {
        UnloadModelFromGPU();

        // Kill EVERYTHING Ollama-related
        string[] processNames = { "ollama", "llama-server" };
        
        foreach (string name in processNames)
        {
            var processes = Process.GetProcessesByName(name);
            foreach (var p in processes)
            {
                try 
                { 
                    if (!p.HasExited)
                    {
                        p.Kill(); 
                        p.WaitForExit(1000);
                        Debug.Log($"Killed {name}.exe PID: {p.Id}");
                    }
                    p.Dispose();
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"Could not kill {name}.exe: {e.Message}");
                }
            }
        }

        // Clear our reference
        if (_ollamaProcess != null)
        {
            try 
            { 
                if (!_ollamaProcess.HasExited)
                    _ollamaProcess.Kill();
                _ollamaProcess.Dispose();
            }
            catch { }
            _ollamaProcess = null;
        }

        ollamaAlreadyStarted = false;
        Debug.Log("All Ollama processes stopped.");
    }

    void UnloadModelFromGPU()
    {
        string unloadJson = "{\"model\":\"" + modelToUse + "\", \"keep_alive\": 0}";
        UnityWebRequest request = new UnityWebRequest(ollamaUrl.Replace("/chat", "/generate"), "POST");
        
        byte[] bodyRaw = Encoding.UTF8.GetBytes(unloadJson);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        request.SendWebRequest();
    }

    private void OnApplicationQuit()
    {
        StopOllama();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (_instance == this)
        {
            _instance = null;
        }
    }

    // --- Player Interaction ---
    public void SendPlayerMessage()
    {
        if (playerInputField == null)
        {
            Debug.LogWarning("ChatBotSystem: SendPlayerMessage called but playerInputField is missing in this scene.");
            return;
        }

        string playerInput = playerInputField.text;
        if (isThinking || string.IsNullOrWhiteSpace(playerInput)) return;

        string wrappedInput = playerInput + $"\n(Respond ONLY with JSON: {{\"dialogue\": \"...\", \"action\": \"{actionsList}\"}})";
        AddToHistory("user", wrappedInput);
        playerInputField.text = "";

        ClearConsole();
        Debug.Log($"History ({conversationHistory.Count}):");
        foreach (var msg in conversationHistory)
            Debug.Log($"Role: {msg.role} | Content: {msg.content}");

        StartCoroutine(AskWithRetry(3));
    }

    // --- Retry Wrapper ---
    IEnumerator AskWithRetry(int maxRetries)
    {
        isThinking = true;

        for (int i = 0; i < maxRetries; i++)
        {
            OllamaRequest request = new OllamaRequest(conversationHistory, modelToUse);
            bool success = false;

            yield return StartCoroutine(AskOllama(request, (result) => success = result));

            if (success)
            {
                isThinking = false;
                yield break;
            }

            Debug.LogWarning($"Bad response, asking model to correct... ({i + 1}/{maxRetries})");
            conversationHistory.Add(new Message("user",
                "Your last response was not valid JSON. You MUST respond ONLY with this exact format: " +
                "{\"dialogue\": \"your response here\", \"action\": \"" + actionsList + "\"}. " +
                "Nothing else. No explanations. Just the JSON object."
            ));
        }

        Debug.LogError("All retries failed. Using fallback.");
        ChatbotReturn fallback = new ChatbotReturn { dialogue = "Grr!", action = "NONE" };
        AddToHistory("assistant", JsonConvert.SerializeObject(fallback));
        ExecuteChatBotAction(fallback.action);
        isThinking = false;
    }

    // --- Communication ---
    public IEnumerator AskOllama(OllamaRequest requestData, System.Action<bool> onDone) 
    {
        string jsonPayload = JsonConvert.SerializeObject(requestData);
        print(jsonPayload);

        using (UnityWebRequest request = new UnityWebRequest(ollamaUrl, "POST")) 
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = 120;

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success) 
            {
                Debug.LogError("Error: " + request.error);
                onDone(false);
            } 
            else 
            {
                bool parsed = ProcessNPCResponse(request.downloadHandler.text);
                onDone(parsed);
            }
        }
    }

    // --- Response Handling ---
    bool ProcessNPCResponse(string json)
    {
        try
        {
            JObject responseObj = JObject.Parse(json);

            if (responseObj["done_reason"]?.ToString() == "load")
            {
                Debug.Log("Ollama confirmed model is loaded.");
                return true; 
            }

            string content = responseObj["message"]?["content"]?.ToString();

            if (string.IsNullOrEmpty(content))
            {
                Debug.LogWarning("Empty message content from Ollama.");
                return false;
            }

            content = System.Text.RegularExpressions.Regex.Replace(
                content, @"```(?:json)?\s*|\s*```", ""
            ).Trim();

            ChatbotReturn ChatBotAct = JsonConvert.DeserializeObject<ChatbotReturn>(content);


            if (ChatBotAct == null || string.IsNullOrEmpty(ChatBotAct.dialogue))
            {
                Debug.LogWarning("Missing dialogue in ChatBotAction: " + content);
                return false;
            }

            bool actionExists = VALID_ACTIONS.Any(a => a.validAction.Equals(ChatBotAct.action, System.StringComparison.OrdinalIgnoreCase));

            if (string.IsNullOrEmpty(ChatBotAct.action) || !actionExists)
            {
                Debug.LogWarning($"Invalid or missing action '{ChatBotAct.action}', defaulting to NONE.");
                ChatBotAct.action = "NONE";
            }

            AddToHistory("assistant", content);
            if (ChatBotOutput != null)
                ChatBotOutput.text = ChatBotAct.dialogue;
            else
                Debug.LogWarning("ChatBotSystem: ChatBotOutput is missing in this scene — dialogue won't be displayed.");
            ExecuteChatBotAction(ChatBotAct.action);
            return true;
        }
        catch (JsonReaderException jex)
        {
            Debug.LogError("JSON Parsing error: " + jex.Message + "\n" + json);
            return false;
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Unexpected error: " + ex.Message + "\n" + json);
            return false;
        }
    }

    // --- History Management ---
    void AddToHistory(string role, string content)  
    {
        conversationHistory.Add(new Message(role, content));

        while (conversationHistory.Count > maxHistory)
        {
            Debug.Log("Max history reached. Removing oldest message.");
            conversationHistory.RemoveAt(1);
        }
    }

    // --- Example Action Executor ---
    void ExecuteChatBotAction(string actionName)
    {
        var current = VALID_ACTIONS.FirstOrDefault(a => 
            a.validAction.Equals(actionName, System.StringComparison.OrdinalIgnoreCase));

        current?.OnAction?.Invoke(); 
    }

    // --- Utility ---
    void ClearConsole()
    {
        var assembly = Assembly.GetAssembly(typeof(UnityEditor.Editor));
        var type = assembly.GetType("UnityEditor.LogEntries");
        var method = type.GetMethod("Clear");
        method.Invoke(new object(), null);
    }

    void ReconnectInputFieldEvents()
    {
        // Find the input field
        if (playerInputField == null)
        {
            playerInputField = FindAnyObjectByType<TMP_InputField>();
        }
        
        if (playerInputField != null)
        {
            // Clear existing listeners to avoid duplicates
            playerInputField.onEndEdit.RemoveAllListeners();
            
            // Add the listener back
            playerInputField.onEndEdit.AddListener(delegate { SendPlayerMessage(); });
            
            Debug.Log("ChatBotSystem: Reconnected OnEndEdit event to SendPlayerMessage.");
        }
        else
        {
            Debug.LogWarning("ChatBotSystem: Could not find input field to reconnect events.");
        }
    }
}