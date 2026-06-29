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

    private string[] prefixes = { "Shadow", "Void", "Frost", "Storm", "Inferno", "Crystal", "Ancient" };
    private string[] weaponTypes = { "Blade", "Axe", "Dagger", "Spear", "Staff", "Sword" };
    private string[] rarities = { "Rare", "Epic", "Legendary" };

    private string[] loreParts =
    {
        "Forged in a forgotten dungeon, this weapon carries unstable magical energy.",
        "Generated from ancient dungeon data, this item adapts to its wielder.",
        "Created by corrupted AI magic, this weapon changes its strength over time.",
        "Discovered inside a cursed chest, this item was never crafted by human hands.",
        "Formed from digital runes, this weapon feeds on the fear of nearby enemies."
    };

    void Start()
    {
        GenerateBasicItem();
    }

    public void GenerateBasicItem()
    {
        itemNameText.text = "Iron Sword";
        itemStatsText.text = "Damage: +5\nDurability: 80";
        itemLoreText.text = "A simple sword used by dungeon guards.";

        itemImage.sprite = basicSwordSprite;
        itemImage.color = Color.white;
    }

    public void GenerateComplexItem()
    {
        string itemName = prefixes[Random.Range(0, prefixes.Length)] + weaponTypes[Random.Range(0, weaponTypes.Length)];

        int damage = Random.Range(10, 26);
        int crit = Random.Range(5, 31);
        int durability = Random.Range(60, 101);
        string rarity = rarities[Random.Range(0, rarities.Length)];
        string lore = loreParts[Random.Range(0, loreParts.Length)];

        itemNameText.text = itemName;

        itemStatsText.text =
            "Damage: +" + damage +
            "\nCrit: +" + crit + "%" +
            "\nDurability: " + durability +
            "\nRarity: " + rarity;

        itemLoreText.text = lore;

        itemImage.sprite = aiSwordSprite;
        itemImage.color = Color.white;
    }
}