using System.Collections.Generic;
using UnityEngine;
using MyGame.Dungeon;

[System.Serializable]
public class GameSaveData
{
    public string saveVersion = "1.0";
    public string savedAt;               // DateTime.UtcNow.ToString()
    public int    slotIndex;
    public string worldName;

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
    public string currentScene;

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

    // PlayerDungeonState (Phase B — personal per-player dungeon instancing)
    // Each player has their own dungeon progression, independent of every
    // other player. dungeon is null until this player has entered the
    // dungeon at least once.
    public bool dungeonInDungeon;
    public DungeonSaveData dungeon;

    // Where to return this player when they exit/die in the dungeon.
    public bool hasDungeonReturnPosition;
    public float dungeonReturnPosX, dungeonReturnPosY, dungeonReturnPosZ;
    public float dungeonReturnRotY;

    // ── Menu System (Inventory redesign) ───────────────────────────────────
    // EquipmentData — ของที่สวมอยู่ในแต่ละช่อง
    public List<EquipmentSlotSaveData> equipment = new();

    // QuestManager — เก็บเฉพาะเควสที่เริ่ม/จบแล้วเท่านั้น (sparse) รองรับเควส
    // หลักร้อยโดยไม่ทำให้ save ไฟล์บวม
    public List<QuestSaveData> quests = new();

    // RelationshipManager — เก็บเฉพาะ NPC ที่เคยพบแล้วเท่านั้น (sparse)
    public List<RelationshipSaveData> relationships = new();

    // SkillManager — เก็บเฉพาะสกิลที่มี progress แล้วเท่านั้น (sparse)
    public List<SkillSaveData> skills = new();
    public int skillPoints;

    // AchievementManager — เก็บเฉพาะของที่ค้นพบแล้วเท่านั้น (sparse)
    public List<int> discoveredCollectibleIds = new();
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

[System.Serializable]
public class QuestSaveData
{
    public int questId;
    public QuestStatus status;
    public int progress;
}

[System.Serializable]
public class RelationshipSaveData
{
    public int npcId;
    public int heartLevel;
    public int heartExp;
    public int lastGiftDay;
    public int giftsGivenToday;
    public int discoveredLikedMask;
    public int discoveredLovedMask;
}

[System.Serializable]
public class SkillSaveData
{
    public int skillId;
    public int level;
    public bool unlocked;
}
