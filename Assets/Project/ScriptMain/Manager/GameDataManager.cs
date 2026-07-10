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
    /// </summary>
    public int LastCompletedDayNumber { get; private set; } = 1;

    public void SetLastCompletedDay(int day) => LastCompletedDayNumber = day;

    // ── Scene-transition time bridge ──────────────────────────────────────────
    // WorldTimeManager lives on a scene-placed NetworkObject that gets despawned
    // on every LoadSceneMode.Single transition. SceneTransitionManager captures
    // the current time here (in DontDestroyOnLoad) before the scene swap so
    // WorldTimeManager.OnNetworkSpawn can restore it instead of resetting to Day 1.

    /// <summary>
    /// -1 means no pending restore. Set by SceneTransitionManager before LoadScene,
    /// consumed and cleared by WorldTimeManager.OnNetworkSpawn.
    /// </summary>
    public int PendingTransitionMinutes { get; private set; } = -1;

    /// <summary>Call on server only, immediately before LoadScene.</summary>
    public void CaptureTimeForTransition()
    {
        if (WorldTimeManager.Instance != null)
            PendingTransitionMinutes = WorldTimeManager.Instance.TotalGameMinutes;
    }

    /// <summary>Call from WorldTimeManager.OnNetworkSpawn after consuming the value.</summary>
    public void ClearTransitionTime() => PendingTransitionMinutes = -1;

    // ── Scene-transition debt bridge ──────────────────────────────────────────
    // DebtManager is also scene-placed, so all its NetworkVariables reset on
    // every LoadSceneMode.Single transition. We snapshot all six fields here
    // (DontDestroyOnLoad) so DebtManager.OnNetworkSpawn can restore them.
    // -1 on PendingDebt means no pending restore (fresh session).

    public int  PendingDebt                   { get; private set; } = -1;
    public int  PendingMonthlyMinimumDue      { get; private set; } = -1;
    public int  PendingPaidThisMonth          { get; private set; } = -1;
    public int  PendingCurrentTradeValue      { get; private set; } = -1;
    public bool PendingIsMonthSettled         { get; private set; } = false;
    public int  PendingTradeValuePaidThisMonth{ get; private set; } = -1;

    /// <summary>Call on server only, immediately before LoadScene.</summary>
    public void CaptureDebtForTransition()
    {
        if (DebtManager.Instance == null) return;
        PendingDebt                    = DebtManager.Instance.CurrentDebt;
        PendingMonthlyMinimumDue       = DebtManager.Instance.MonthlyMinimumDue;
        PendingPaidThisMonth           = DebtManager.Instance.PaidThisMonth;
        PendingCurrentTradeValue       = DebtManager.Instance.CurrentTradeValue;
        PendingIsMonthSettled          = DebtManager.Instance.IsMonthSettled;
        PendingTradeValuePaidThisMonth = DebtManager.Instance.TradePaidThisMonth;
    }

    /// <summary>Call from DebtManager.OnNetworkSpawn after consuming the values.</summary>
    public void ClearDebtTransition()
    {
        PendingDebt                    = -1;
        PendingMonthlyMinimumDue       = -1;
        PendingPaidThisMonth           = -1;
        PendingCurrentTradeValue       = -1;
        PendingIsMonthSettled          = false;
        PendingTradeValuePaidThisMonth = -1;
    }

    // ── Scene-transition gold bridge ──────────────────────────────────────────
    // CurrencyManager / CurrencyData are scene-placed; gold NetworkVariable resets
    // to 0 on every LoadSceneMode.Single swap. -1 = no pending restore.

    public long PendingGold { get; private set; } = -1L;

    /// <summary>Call on server only, immediately before LoadScene.</summary>
    public void CaptureGoldForTransition()
    {
        if (CurrencyManager.Instance != null)
            PendingGold = CurrencyManager.Instance.CurrentGold;
    }

    /// <summary>Call from CurrencyManager.OnNetworkSpawn after consuming the value.</summary>
    public void ClearGoldTransition() => PendingGold = -1L;

    // ── Scene-transition farm bridge ──────────────────────────────────────────
    // FarmingServerManager state lives in plain C# dictionaries that are lost when
    // the scene-placed NetworkObject despawns. We snapshot per scene name so that
    // visiting a non-farming scene (Bar, RoomShiba, etc.) never overwrites the
    // MainGame farm snapshot with empty data.

    private readonly Dictionary<string, FarmTransitionState> _pendingFarmStates
        = new Dictionary<string, FarmTransitionState>();

    /// <summary>
    /// Snapshots the current farm state tagged to <paramref name="sceneName"/>.
    /// Call on server only, immediately before LoadScene.
    /// </summary>
    public void CaptureFarmForTransition(string sceneName)
    {
        if (FarmingServerManager.Instance == null) return;
        _pendingFarmStates[sceneName] = FarmingServerManager.Instance.CaptureTransitionState();
    }

    /// <summary>
    /// Returns and removes the farm state for <paramref name="sceneName"/>, or null if none.
    /// Call from FarmingServerManager.OnNetworkSpawn to consume the snapshot.
    /// </summary>
    public FarmTransitionState GetAndClearFarmState(string sceneName)
    {
        if (_pendingFarmStates.TryGetValue(sceneName, out var state))
        {
            _pendingFarmStates.Remove(sceneName);
            return state;
        }
        return null;
    }
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
        // NOTE: ResetNightSummary() is intentionally NOT called here.
        // The Summary panel reads NightSellSummary on open, so we must not clear it
        // until AFTER the player has seen and dismissed the panel.
        // SummaryPanelUI.OnSleepClicked() calls ResetNightSummary() when the panel closes.
        WorldTimeManager.Instance.SkipToMorningServerRpc();
        FarmingServerManager.Instance?.AdvanceCropsForNewDay();
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
