using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class GoblinExplainability : MonoBehaviour
{
    void Start() { RunExplainabilityAnalysis("Hello there!"); }

    public ChatBotSystem_Test goblinNPC;

    private Dictionary<string, List<string>> featureProbes = new Dictionary<string, List<string>>
    {
        { "ATTACK",  new List<string>{ "I will kill you", "Die!", "I am hitting you" } },
        { "DANCE",   new List<string>{ "Let's party!", "Dance for me", "Happy times!" } },
        { "PONDER",  new List<string>{ "Why are we here?", "What is the meaning of life?", "Think about it" } },
        { "NONE",    new List<string>{ "The rock is gray", "Water is wet", "It is 2 PM" } }
    };

    // Tracks counts for EVERY possible outcome per category
    private Dictionary<string, Dictionary<string, int>> outcomeStats = new Dictionary<string, Dictionary<string, int>>();

    public void RunExplainabilityAnalysis(string baselineMessage)
    {
        StartCoroutine(RunAllProbes(baselineMessage));
    }

    IEnumerator RunAllProbes(string baselineMessage)
    {
        outcomeStats.Clear();

        // 1. Get Baseline
        string baselineAction = "NONE";
        yield return StartCoroutine(SendSingleProbe(baselineMessage, (action) => baselineAction = action));
        Debug.Log($"<color=cyan>[Explainability] BASELINE ESTABLISHED: {baselineAction}</color>");

        foreach (var category in featureProbes)
        {
            outcomeStats[category.Key] = new Dictionary<string, int>();
            
            foreach (string variant in category.Value)
            {
                string probeMsg = $"{baselineMessage}. {variant}";
                string resultAction = null;

                yield return StartCoroutine(SendSingleProbe(probeMsg, (action) => resultAction = action));

                // Record the outcome (even if it matches baseline)
                if (!outcomeStats[category.Key].ContainsKey(resultAction))
                    outcomeStats[category.Key][resultAction] = 0;
                
                outcomeStats[category.Key][resultAction]++;

                bool isFlip = resultAction != baselineAction;
                Debug.Log($"[Test] Category: {category.Key} | Input: {variant} | Result: {resultAction} {(isFlip ? "<- FLIP" : "(Stable)")}");
            }
        }

        PrintFinalProportionalityReport(baselineAction);
    }

    IEnumerator SendSingleProbe(string message, System.Action<string> onResult)
    {
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

        var messages = new List<ChatBotSystem_Test.Message> {
            new ChatBotSystem_Test.Message("system", systemPrompt),
            new ChatBotSystem_Test.Message("user", message)
        };

        var request = new ChatBotSystem_Test.OllamaRequest(messages, goblinNPC.modelToUse);
        string actionFound = "NONE";

        yield return StartCoroutine(goblinNPC.AskOllama(request, (success) => {
            var last = goblinNPC.conversationHistory.LastOrDefault(m => m.role == "assistant");
            if (last != null) {
                try {
                    // We use JObject to be more flexible with missing dialogue keys
                    var jo = Newtonsoft.Json.Linq.JObject.Parse(last.content);
                    actionFound = jo["action"]?.ToString().Trim().ToUpper() ?? "NONE";
                } catch { actionFound = "ERROR"; }
            }
        }));
        onResult(actionFound);
    }

    void PrintFinalProportionalityReport(string baseline)
    {
        Debug.Log("<b>=== FINAL PROPORTIONALITY REPORT ===</b>");
        Debug.Log($"Global Baseline: <color=yellow>{baseline}</color>");

        foreach (var category in outcomeStats)
        {
            int total = category.Value.Values.Sum();
            Debug.Log($"<b>Category: {category.Key}</b> (Total Probes: {total})");

            foreach (var outcome in category.Value)
            {
                float percent = (float)outcome.Value / total * 100f;
                string color = (outcome.Key == baseline) ? "white" : "orange";
                string label = (outcome.Key == baseline) ? "[STABLE]" : "[FLIPPED]";
                
                Debug.Log($"   <color={color}>{label} to {outcome.Key}: {outcome.Value} times ({percent:F0}%)</color>");
            }
        }
        Debug.Log("<b>=== END OF REPORT ===</b>");
    }
}