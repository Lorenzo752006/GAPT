using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ItemGenerator : MonoBehaviour
{
    public TMP_Text itemNameText;
    public TMP_Text itemStatsText;
    public TMP_Text itemLoreText;
    public Image itemImage;

    public Sprite basicSwordSprite;
    public Sprite aiSwordSprite;

    void Start()
    {
        itemNameText.text = "Item Name";
        itemStatsText.text = "Stats";
        itemLoreText.text = "Lore";
        itemImage.sprite = null;
        itemImage.color = new Color(1, 1, 1, 0); // hides image at start
    }

    public void GenerateBasicItem()
    {
        itemNameText.text = "Iron Sword";
        itemStatsText.text = "Damage: +5\nDurability: 80";
        itemLoreText.text = "A simple sword used by dungeon guards.";

        itemImage.sprite = basicSwordSprite;
        itemImage.color = Color.white; // shows image
    }

    public void GenerateComplexItem()
    {
        itemNameText.text = "Shadowfang Blade";
        itemStatsText.text = "Damage: +15\nCrit: +20%\nRarity: Epic";
        itemLoreText.text = "Forged in darkness, this AI-generated blade feeds on fear.";

        itemImage.sprite = aiSwordSprite;
        itemImage.color = Color.white; // shows image
    }
}