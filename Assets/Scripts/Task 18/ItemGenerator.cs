using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using UnityEngine.UI;

// Task 18 - Generative AI content pipeline.
//
//   BASIC   (GenerateBasicItem): hardcoded name/stats/lore + a pre-made sprite.
//           The placeholder baseline.
//
//   COMPLEX (GenerateComplexItem): generates the item live via web APIs -
//           an AI image from Pollinations (image.pollinations.ai) applied to the
//           UI, and AI stats + lore from Pollinations text (text.pollinations.ai).
//           Pollinations is free and needs no API key. Endpoints are configurable.
//
// Wire the two methods to two UI buttons (Basic / Complex).
public class ItemGenerator : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text itemNameText;
    public TMP_Text itemStatsText;
    public TMP_Text itemLoreText;
    public Image itemImage;

    [Header("Basic (placeholder) art")]
    public Sprite basicSwordSprite;
    public Sprite aiSwordSprite; // kept for fallback if a request fails

    [Header("Complex - generation prompts")]
    [TextArea] public string imagePrompt =
        "a single fantasy sword item icon, dark dungeon loot, centered on black background, pixel art";
    [TextArea] public string textPrompt =
        "Invent one fantasy dungeon weapon. Reply ONLY with compact JSON, no markdown, " +
        "exactly: {\"name\":\"...\",\"stats\":\"...\",\"lore\":\"...\"}. " +
        "stats is a short multi-line string, lore is one or two sentences.";

    [Header("Complex - endpoints (Pollinations.ai)")]
    public string imageEndpoint = "https://image.pollinations.ai/prompt/";
    public string textEndpoint = "https://text.pollinations.ai/";
    public int imageSize = 384;

    private bool isBusy;

    [Serializable]
    private class GeneratedItem
    {
        public string name;
        public string stats;
        public string lore;
    }

    private void Start()
    {
        itemNameText.text = "Item Name";
        itemStatsText.text = "Stats";
        itemLoreText.text = "Lore";
        if (itemImage != null)
        {
            itemImage.sprite = null;
            itemImage.color = new Color(1, 1, 1, 0); // hidden until something is shown
        }
    }

    // ----------------------------------------------------------------- BASIC
    public void GenerateBasicItem()
    {
        itemNameText.text = "Iron Sword";
        itemStatsText.text = "Damage: +5\nDurability: 80";
        itemLoreText.text = "A simple sword used by dungeon guards.";

        if (itemImage != null)
        {
            itemImage.sprite = basicSwordSprite;
            itemImage.color = Color.white;
        }
    }

    // --------------------------------------------------------------- COMPLEX
    public void GenerateComplexItem()
    {
        if (isBusy) return;
        StartCoroutine(GenerateRoutine());
    }

    private IEnumerator GenerateRoutine()
    {
        isBusy = true;
        itemNameText.text = "Generating...";
        itemStatsText.text = "";
        itemLoreText.text = "Contacting generative APIs...";

        // Fire both requests; run them one after another to keep it simple/robust.
        yield return StartCoroutine(RequestText());
        yield return StartCoroutine(RequestImage());

        isBusy = false;
    }

    private IEnumerator RequestText()
    {
        string url = textEndpoint + UnityWebRequest.EscapeURL(textPrompt);
        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                itemNameText.text = "Mystery Item";
                itemStatsText.text = "(stats unavailable)";
                itemLoreText.text = "Text generation failed: " + req.error;
                yield break;
            }

            ApplyGeneratedText(req.downloadHandler.text);
        }
    }

    private void ApplyGeneratedText(string raw)
    {
        // Models sometimes wrap JSON in prose or ``` fences - extract the {...} span.
        GeneratedItem item = null;
        int open = raw.IndexOf('{');
        int close = raw.LastIndexOf('}');
        if (open >= 0 && close > open)
        {
            string json = raw.Substring(open, close - open + 1);
            try { item = JsonUtility.FromJson<GeneratedItem>(json); }
            catch { item = null; }
        }

        if (item != null && !string.IsNullOrWhiteSpace(item.name))
        {
            itemNameText.text = item.name;
            itemStatsText.text = string.IsNullOrWhiteSpace(item.stats) ? "(no stats)" : item.stats;
            itemLoreText.text = string.IsNullOrWhiteSpace(item.lore) ? "(no lore)" : item.lore;
        }
        else
        {
            // Fallback: not valid JSON, just show the raw generated text as lore.
            itemNameText.text = "AI-Generated Item";
            itemStatsText.text = "Damage: +?? \nRarity: Unknown";
            itemLoreText.text = raw.Trim();
        }
    }

    private IEnumerator RequestImage()
    {
        if (itemImage == null) yield break;

        string url = imageEndpoint + UnityWebRequest.EscapeURL(imagePrompt)
                     + $"?width={imageSize}&height={imageSize}&nologo=true&seed={UnityEngine.Random.Range(1, 999999)}";

        using (UnityWebRequest req = UnityWebRequestTexture.GetTexture(url))
        {
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                // Fall back to the pre-made AI sprite so the panel still shows something.
                itemImage.sprite = aiSwordSprite;
                itemImage.color = Color.white;
                yield break;
            }

            Texture2D tex = DownloadHandlerTexture.GetContent(req);
            Sprite sprite = Sprite.Create(
                tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f));

            itemImage.sprite = sprite;
            itemImage.color = Color.white;
        }
    }
}
