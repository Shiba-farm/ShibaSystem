using System.Collections.Generic;
using UnityEngine;

public enum GameDifficulty { Easy, Normal, Hard }

[System.Serializable]
public class DifficultySettings
{
    public GameDifficulty difficulty;
    public float monthlyPaymentPercent;  // 0.10 = 10%, 0.20 = 20%
}
public enum GameState { Playing, InMenu, Pause }
public class GameDataManager : MonoBehaviour
{
    public static GameDataManager Instance { get; private set; }
    public GameState CurrentState { get; private set; }
    public ItemDatabases itemDatabases;
    public CraftRecipeDatabase craftRecipeDatabase;
    public GameDifficulty CurrentDifficulty { get; private set; }

    public int CurrentDayNumber => WorldTimeManager.Instance != null
    ? WorldTimeManager.Instance.CurrentDay
    : 1;
    public Dictionary<ItemCategory, CategorySellRecord> NightSellSummary { get; private set; }
    public void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        itemDatabases.Initialize();
        craftRecipeDatabase.Initialize();
        ResetNightSummary();
    }

    public void InitializeWorld(GameDifficulty difficulty)
    {
        CurrentDifficulty = difficulty;
    }

    public void SetGameState(GameState gameState)
    {
        CurrentState = gameState;
    }
    public void SaveGame()
    {
        // Implement your save logic here
        Debug.Log("Game saved!");
    }

    public void ResetNightSummary()
    {
        NightSellSummary = new Dictionary<ItemCategory, CategorySellRecord>();
        foreach (ItemCategory cat in System.Enum.GetValues(typeof(ItemCategory)))
        {
            NightSellSummary[cat] = new CategorySellRecord { Category = cat };
        }
    }

    public void RecordSale(int itemId, int amount, int goldEarned)
    {
        ItemSO itemSO = itemDatabases.GetItemByID(itemId);
        if (itemSO == null) return;

        ItemCategory category = itemSO.category;
        var record = NightSellSummary[category];

        record.TotalGold += goldEarned;

        var existing = record.Items.Find(e => e.ItemID == itemId);
        if (existing != null)
        {
            existing.Amount += amount;
            existing.GoldEarned += goldEarned;
        }
        else
        {
            record.Items.Add(new SoldItemEntry
            {
                ItemID = itemId,
                ItemName = itemSO.itemName,
                Amount = amount,
                GoldEarned = goldEarned
            });
        }
    }

    public int GetNightTotalGold()
    {
        int total = 0;
        foreach (var record in NightSellSummary.Values)
            total += record.TotalGold;
        return total;
    }

    public void AdvanceDay()
    {
        CurrencyManager.Instance.AddCurrencyServerRpc(GetNightTotalGold());
        ResetNightSummary();
        WorldTimeManager.Instance.SkipToMorningServerRpc();
    }

}

[System.Serializable]
public class CategorySellRecord
{
    public ItemCategory Category;
    public int TotalGold;
    public List<SoldItemEntry> Items = new List<SoldItemEntry>();
}

[System.Serializable]
public class SoldItemEntry
{
    public int ItemID;
    public string ItemName;
    public int Amount;
    public int GoldEarned;
}
