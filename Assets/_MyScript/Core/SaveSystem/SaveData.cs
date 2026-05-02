using System;

[Serializable]
public class SoilTileData
{
    public float posX;
    public float posY;
    public float posZ;

    public bool isTilled;
    public bool isWatered;

    public string cropName;
    public int stageIndex;
    public float stageTimer;
}

[Serializable]
public class InventorySlotData
{
    public string itemName;
    public int amount;
}

[Serializable]
public class HotbarSlotData
{
    public string itemName;
    public int amount;
}

[Serializable]
public class SaveData
{
    // Player
    public float playerX;
    public float playerY;
    public float playerZ;
    public float playerRotY;

    // Time / Calendar
    public int year;
    public int month;
    public int day;
    public int hour;
    public int minute;

    // Status
    public float currentEnergy;

    // Economy
    public int money;
    public int currentDebt;
    public int missedPayments;
    public int monthsPassed;

    // Inventory / Hotbar
    public InventorySlotData[] inventorySlots;
    public HotbarSlotData[] hotbarSlots;

    // Farming (Soil + Crops)
    public SoilTileData[] soilTiles;

    // Crafting & Farm Helpers
    public FarmHelperData[] farmHelpers;
    public string[] learnedRecipes;
    public int consecutiveMisses;

    // Market / Economy
    public MarketItemData[] marketPrices;
    public int daysSinceRestock;
}

[Serializable]
public class FarmHelperData
{
    public string helperName;
    public float posX;
    public float posY;
    public float posZ;
    public float rotY;
    public int daysUsed;
    public string uniqueId;
}
