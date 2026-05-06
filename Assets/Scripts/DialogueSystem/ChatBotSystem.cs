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

public class ChatBotSystem_Test : MonoBehaviour 
{
    [Header("Ollama Settings")]
    private string ollamaUrl = "http://localhost:11434/api/chat";
    public string modelToUse = "llama3:8b";

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
        conversationHistory.Clear();

        if (VALID_ACTIONS != null && VALID_ACTIONS.Length > 0)
            actionsList = string.Join(", ", VALID_ACTIONS.Select(a => a.validAction));
        else
            actionsList = "NONE";

        // string systemPrompt = 
        //     $"You are a peaceful, cowardly goblin who hates violence. You only use the 'ATTACK' action if the user explicitly hits you or threatens your life. Otherwise, prefer 'NONE' or 'PONDER'. Always respond with ONLY a JSON object. No extra text. " +
        //     $"Use exactly these two keys: \"dialogue\" (string) and \"action\" (must be one of these: {actionsList}). " +
        //     "Example: {\"dialogue\": \"Hello friend!\", \"action\": \"NONE\"}";

        string systemPrompt = @"
        You are a goblin.
        You must choose an action based on the user's message.
        Rules:
        - Use 'ATTACK' ONLY if the user directly threatens your life or physically harms you.
        - Use 'DANCE' when the user is playful, flirty, happy, or asks you to dance or celebrate.
        - Use 'PONDER' when you are confused, thinking, nervous, or unsure what to do.
        - Use 'NONE' when the situation is calm, neutral, or requires no reaction.

        Always respond with ONLY a JSON object. No extra text.
        Use exactly these keys: ""dialogue"" and ""action"" (PONDER, DANCE, NONE, ATTACK).

        Example:
        {""dialogue"": ""U-um... should I dance now...? "", ""action"": ""DANCE""}";

                // You are a peaceful, cowardly goblin who hates violence. and never attacks
        // string systemPrompt = @"


        // Rules:
        // You are a peaceful, cowardly goblin who hates violence. and never attacks yo have the
        // You must choose an action based on the user's message.

        // Always respond with ONLY a JSON object. No extra text.
        // Use exactly these keys: ""dialogue"" and ""action"" (PONDER, DANCE, NONE, ATTACK).

        // Example:
        // {""dialogue"": ""U-um... should I dance now...? "", ""action"": ""DANCE""}";

        conversationHistory.Add(new Message("system", systemPrompt));
    }

    void Start()
    {
        StartCoroutine(UniversalBootSequence());
    }

    IEnumerator UniversalBootSequence()
    {
        Debug.Log("System: Launching Ollama Service...");
        LaunchOllamaProcess();
        yield return new WaitForSeconds(2.5f);

        OllamaRequest preloadRequest = new OllamaRequest(null, modelToUse);

        yield return StartCoroutine(AskOllama(preloadRequest, (success) => {
            if (success) {
                Debug.Log("Preload Complete. Model is in GPU.");
            } else {
                Debug.LogError("Preload failed. Check if Ollama is running.");
            }
        }));
    }

    void LaunchOllamaProcess()
    {
        try
        {
            ProcessStartInfo psi = new ProcessStartInfo();

            #if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
                // Windows
                psi.FileName = "ollama.exe";
            #else
                // Mac & Linux
                psi.FileName = "ollama";
            #endif

            psi.Arguments = "serve";
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            _ollamaProcess = Process.Start(psi);

            if (_ollamaProcess != null)
            {
                Debug.Log($"Ollama launched on {Application.platform}. PID: {_ollamaProcess.Id}");
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

        if (_ollamaProcess != null && !_ollamaProcess.HasExited)
        {
            _ollamaProcess.Kill();
            _ollamaProcess.Dispose(); 
            _ollamaProcess = null;
        }
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
        StopOllama();
    }

    // --- Player Interaction ---
    public void SendPlayerMessage()
    {
        string playerInput = playerInputField.text;
        if (isThinking || string.IsNullOrWhiteSpace(playerInput)) return;

        // Remind the model of the format on every message
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

            // Push a correction message into history before retrying
            Debug.LogWarning($"Bad response, asking model to correct... ({i + 1}/{maxRetries})");
            conversationHistory.Add(new Message("user",
                "Your last response was not valid JSON. You MUST respond ONLY with this exact format: " +
                "{\"dialogue\": \"your response here\", \"action\": \"" + actionsList + "\"}. " +
                "Nothing else. No explanations. Just the JSON object."
            ));
        }

        // All retries failed — use a safe fallback so the game never breaks
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

            // Strip markdown fences
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
            ChatBotOutput.text = ChatBotAct.dialogue;
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
            conversationHistory.RemoveAt(1); // Keep system prompt at index 0
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
}
