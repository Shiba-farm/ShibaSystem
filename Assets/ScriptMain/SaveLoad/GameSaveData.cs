using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GameSaveData
{
    public string saveVersion = "1.0";
    public string savedAt;               // DateTime.UtcNow.ToString()
    public int    slotIndex;

    public WorldSaveData world = new();
    public List<PlayerSaveData> players = new();

    public PlayerSaveData GetOrCreatePlayer(ulong clientId)
    {
        string id = clientId.ToString(); // swap for Steam ID later
        var existing = players.Find(p => p.playerId == id);
        if (existing != null) return existing;

        var fresh = new PlayerSaveData { playerId = id };
        players.Add(fresh);
        return fresh;
    }

    // used during LOAD — returns null if player has no save data yet
    public PlayerSaveData FindPlayer(ulong clientId)
    {
        string id = clientId.ToString();
        return players.Find(p => p.playerId == id);
    }
}

[System.Serializable]
public class WorldSaveData
{
    // WorldTimeManager
    public int currentYear;
    public int currentMonth;
    public int currentDay;

    // CurrencyManager (if shared gold) or move to PlayerSaveData if per-player
    public long sharedGold;

    // DebtManager
    public int currentDebt;
    // public int remainingDueThisMonth;
    public int monthlyMinimumDue;
    public int paidThisMonth;
    public int tradeValuePaidThisMonth;


    // World items lying on the ground (optional, lower priority)
    public List<WorldItemSaveData> droppedItems = new();

    // Crops (important!)
    public List<CropSaveData> crops = new();
}

[System.Serializable]
public class WorldItemSaveData
{
    public int itemID;
    public int amount;
    public float posX, posY, posZ;
}

[System.Serializable]
public class CropSaveData
{
    public int cropItemID;        // which crop type (from ItemSO ID)
    public int growthStage;       // 0 = just planted, max = harvestable
    public bool isWatered;
    public float posX, posY, posZ;
}

[System.Serializable]
public class PlayerSaveData
{
    // Identity
    public string playerId;           // stable ID (Steam ID later, GUID for now)
    public string playerName;

    // PlayerController — position
    public float posX, posY, posZ;
    public float rotY;                // facing direction is enough

    // StatManager — mirrors your NetworkStat / StatType
    public List<StatSaveData> stats = new();
    public int level;

    // InventoryNetworkManager — main inventory (inventoryID = 0)
    public List<ItemSlotSaveData> inventory = new();

    // HotbarUIController — hotbar inventory (inventoryID = 1, or whatever yours is)
    public List<ItemSlotSaveData> hotbar = new();

    // PlayerHeldItem — just the selected slot index
    public int heldSlotIndex;
}

[System.Serializable]
public class StatSaveData
{
    public StatType type;             // Health / Stamina / Energy — your existing enum
    public float currentValue;
    public float maxValue;
}

[System.Serializable]
public class ItemSlotSaveData
{
    public int slotIndex;
    public int itemID;                // 0 = empty, matches your NetworkItems.ItemID
    public int amount;                // matches your NetworkItems.Amount
}