using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum ItemCategory { Base, Tools, Food, Structures, Resources, Seed, FarmHelper, Wearables }
public enum ToolAction { None, Hoe, Water, Axe, Mine, Fish, Weapon }

public enum SellCategory { Farming, Fishing, Ore, Other }
public enum HoldType
{
    None = 0,           // resources, seeds — nothing shown in hand
    OneHand = 1,        // knife, axe, hoe
    TwoHand = 2,        // pickaxe, scythe, fishing rod
    TwoHandLift = 4     // heavy box, crate — lifted above head
}

public class ItemSO : ScriptableObject
{

    [Header("Info")]
    public string itemName;
    public int itemID;
    public Sprite icon;

    [Header("Stack")]
    public bool isStackable = true;
    [Min(1)] public int maxStack = 99;

    [Header("3D Visuals")]
    public GameObject equipmentPrefab;
    public GameObject worldItemPrefab;
    public HoldType holdType = HoldType.OneHand;

    [Header("Sell")]
    public bool sellable = true;
    public int sellPrice = 10;
    public SellCategory sellCategory = SellCategory.Other;
    [Header("Gameplay")]
    public ItemCategory category = ItemCategory.Tools;

    // public Vector3 holdPositionOffset = Vector3.zero;
    // public Vector3 holdRotationOffset = Vector3.zero;
    // public Vector3 holdScale = Vector3.one;


    // In ItemSO:
    [Header("Hold Offset")]
    public List<HoldPosition> holdPositions = new();
    public HoldState defaultHoldState = HoldState.Idle;

    public HoldPosition GetHoldPosition(HoldState state)
    {
        return holdPositions.Find(h => h.state == state)
            ?? holdPositions.Find(h => h.state == HoldState.Idle)
            ?? holdPositions.FirstOrDefault();
    }


#if UNITY_EDITOR
    private void OnValidate()
    {
        ItemHoldPreview preview = null;

        var prefabStage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
        if (prefabStage != null)
        {
            // search inside the prefab stage root
            preview = prefabStage.prefabContentsRoot.GetComponentInChildren<ItemHoldPreview>();
        }
        else
        {
            // fall back to scene search
            preview = FindFirstObjectByType<ItemHoldPreview>();
        }

        if (preview == null || preview.previewItem != this) return;
        preview.ApplyOffsets();
    }
#endif
}

[System.Serializable]
public class HoldPosition
{
    public HoldState state;
    public Vector3 positionOffset;
    public Vector3 rotationOffset;
}