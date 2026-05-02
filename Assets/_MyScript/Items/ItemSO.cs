using UnityEngine;

public enum ItemCategory { Base, Tools, Food, Structures, Resources, Seed, FarmHelper, Wearables }
public enum ToolAction { None, Hoe, Water, Axe }

public enum SellCategory { Farming, Fishing, Ore, Other }

[CreateAssetMenu(menuName = "Items/Item")]
public class ItemSO : ScriptableObject
{

    [Header("Info")]
    public string itemName;
    public int itemID;
    public Sprite icon;

    [Header("3D Visuals")]
    public GameObject equipmentPrefab;

    [Header("Effects (VFX & SFX)")]
    public GameObject actionVFX;
    public AudioClip actionSFX;
    public float sfxDuration = 0f;
    [Range(0.8f, 1.2f)] public float pitchRandomMultiplier = 1f;

    [Header("Energy")]
    public float energyCost = 0f;

    [Header("Stack")]
    public bool isStackable = true;
    [Min(1)] public int maxStack = 99;

    [Header("Gameplay")]
    public ItemCategory category = ItemCategory.Tools;
    public ToolAction toolAction = ToolAction.None;
    public CropSO seedCrop;

    [Header("Crafting")]
    [Tooltip("ถ้าเป็น FarmHelper → อ้างถึง FarmHelperSO")]
    public FarmHelperSO farmHelperData;

    [Header("Sell")]
    public bool sellable = true;
    public int sellPrice = 10;

    [Tooltip("หมวดหมู่ที่จะแสดงใน Day Summary (Farming/Fishing/Ore/Other)")]
    public SellCategory sellCategory = SellCategory.Other;
}