using System;
using System.Collections;
using UnityEngine;
using Sirenix.OdinInspector;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    [Header("Signal")]
    [SerializeField] private WorldTimeSignal timeSignal;

    [FoldoutGroup("Core Refs"), Required, SerializeField]
    private Transform player;

    [FoldoutGroup("Core Refs"), Required, SerializeField]
    private InventoryMainUI inventoryUI;

    [FoldoutGroup("Core Refs"), Required, SerializeField]
    private HotbarUI hotbarUI;

    [FoldoutGroup("Core Refs"), Required, SerializeField]
    private PlayerEnergy playerEnergy;

    [FoldoutGroup("Core Refs"), Required, SerializeField]
    private CalendarSystem calendar;

    [FoldoutGroup("Core Refs"), Required, SerializeField]
    private TimeOfDaySystem timeOfDay;

    [FoldoutGroup("Core Refs"), Required, SerializeField]
    private ItemDatabase itemDatabase;

    [FoldoutGroup("Core Refs"), SerializeField]
    private PlayerWallet playerWallet;

    [FoldoutGroup("Core Refs"), SerializeField]
    private DebtCollectorManager debtManager;

    [FoldoutGroup("Farming"), Required, SerializeField]
    [InlineEditor]
    private CropSO[] allCrops;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[GameManager] พบ Instance ซ้ำบน '{gameObject.name}' — ลบ Component นี้ออก");
            Destroy(this);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        StartCoroutine(InitRoutine());
    }

    private IEnumerator InitRoutine()
    {
        yield return null;

        if (SaveSystem.LoadOnStart && SaveSystem.SaveExists())
            LoadGame();
        else
            StartNewGameDefaults();

        if (player != null)
            Debug.Log($"[GameManager] After init pos = {player.position}, rotY = {player.eulerAngles.y}");
    }

    private void StartNewGameDefaults()
    {
        Debug.Log("[GameManager] Start new game");
    }

    // === ������顴�ҡ Inspector �µç ===
    [FoldoutGroup("Debug Buttons")]
    [Button(ButtonSizes.Medium)]
    private void DebugSave() => SaveGame();

    [FoldoutGroup("Debug Buttons")]
    [Button(ButtonSizes.Medium)]
    private void DebugLoad() => LoadGame();

    // ================= SAVE =================
    public void SaveGame()
    {
        var data = new SaveData();

        Vector3 pos = player.position;
        data.playerX = pos.x;
        data.playerY = pos.y;
        data.playerZ = pos.z;
        data.playerRotY = player.eulerAngles.y;

        // var d = calendar.date;
        // data.year = d.year;
        // data.month = d.month;
        // data.day = d.day;
        data.hour = timeSignal.CurrentTime.Hour;
        data.minute = timeSignal.CurrentTime.Minute;

        data.currentEnergy = playerEnergy.CurrentEnergy;

        // Economy
        if (playerWallet != null)
            data.money = playerWallet.Money;
        if (debtManager != null)
        {
            data.currentDebt = debtManager.CurrentDebt;
            data.missedPayments = debtManager.MissedPayments;
            data.monthsPassed = debtManager.MonthsPassed;
        }

        // Inventory
        data.inventorySlots = new InventorySlotData[inventoryUI.slots.Length];
        for (int i = 0; i < inventoryUI.slots.Length; i++)
        {
            var slot = inventoryUI.slots[i];
            data.inventorySlots[i] = new InventorySlotData()
            {
                itemName = slot.item ? slot.item.itemName : "",
                amount = slot.amount
            };
        }

        // Hotbar
        data.hotbarSlots = new HotbarSlotData[hotbarUI.slots.Length];
        for (int i = 0; i < hotbarUI.slots.Length; i++)
        {
            var slot = hotbarUI.slots[i];
            data.hotbarSlots[i] = new HotbarSlotData()
            {
                itemName = slot.item ? slot.item.itemName : "",
                amount = slot.amount
            };
        }

        // Soil
        SoilTile[] tiles = FindObjectsOfType<SoilTile>();
        Array.Sort(tiles,
            (a, b) => a.transform.position.sqrMagnitude
                .CompareTo(b.transform.position.sqrMagnitude));

        data.soilTiles = new SoilTileData[tiles.Length];
        for (int i = 0; i < tiles.Length; i++)
            data.soilTiles[i] = tiles[i].GetSaveData();

        // FarmHelpers
        if (FarmHelperManager.Instance != null)
            data.farmHelpers = FarmHelperManager.Instance.GetSaveData();

        // Crafting — สูตรที่เรียนรู้แล้ว
        if (CraftingManager.Instance != null)
            // data.learnedRecipes = CraftingManager.Instance.GetLearnedRecipes();

        // Debt Punishment — consecutive misses
        if (DebtPunishmentSystem.Instance != null)
            data.consecutiveMisses = DebtPunishmentSystem.Instance.GetConsecutiveMisses();

        // Market Prices
        if (MarketPriceSystem.Instance != null)
            data.marketPrices = MarketPriceSystem.Instance.GetSaveData();

        SaveSystem.Save(data);
        Debug.Log("[GameManager] Saved game with farm data.");
    }

    // ================= LOAD =================
    public void LoadGame()
    {
        var data = SaveSystem.Load();
        if (data == null)
        {
            StartNewGameDefaults();
            return;
        }

        player.position = new Vector3(data.playerX, data.playerY, data.playerZ);
        var e = player.eulerAngles;
        e.y = data.playerRotY;
        player.eulerAngles = e;

        // calendar.SetDate(data.year, data.month, data.day);
        timeSignal.UpdateTime(data.hour, data.minute, data.day, data.month, data.year); 
        float t01 = (data.hour + data.minute / 60f) / 24f;
        // calendar.SetTime01(t01, true);

        playerEnergy.SetEnergy(data.currentEnergy);

        // Economy
        if (playerWallet != null)
            playerWallet.SetMoney(data.money);
        if (debtManager != null)
        {
            debtManager.SetCurrentDebt(data.currentDebt);
            debtManager.SetMissedPayments(data.missedPayments);
            debtManager.SetMonthsPassed(data.monthsPassed);
        }

        // Inventory
        for (int i = 0; i < inventoryUI.slots.Length; i++)
        {
            if (i >= data.inventorySlots.Length)
            {
                inventoryUI.slots[i].Clear();
                continue;
            }

            var sd = data.inventorySlots[i];
            if (string.IsNullOrEmpty(sd.itemName))
                inventoryUI.slots[i].Clear();
            else
                inventoryUI.slots[i].SetItem(itemDatabase.GetItemByName(sd.itemName), sd.amount);
        }

        // Hotbar
        for (int i = 0; i < hotbarUI.slots.Length; i++)
        {
            if (i >= data.hotbarSlots.Length)
            {
                hotbarUI.slots[i].Clear();
                continue;
            }

            var sd = data.hotbarSlots[i];
            if (string.IsNullOrEmpty(sd.itemName))
                hotbarUI.slots[i].Clear();
            else
                hotbarUI.slots[i].SetItem(itemDatabase.GetItemByName(sd.itemName), sd.amount);
        }

        // Soil
        SoilTile[] tiles = FindObjectsOfType<SoilTile>();
        Array.Sort(tiles,
            (a, b) => a.transform.position.sqrMagnitude
                .CompareTo(b.transform.position.sqrMagnitude));

        if (data.soilTiles != null)
        {
            int count = Mathf.Min(tiles.Length, data.soilTiles.Length);
            for (int i = 0; i < count; i++)
                tiles[i].ApplySaveData(data.soilTiles[i], allCrops);
        }

        // FarmHelpers — รวบรวม FarmHelperSO จาก ItemDatabase
        if (FarmHelperManager.Instance != null && data.farmHelpers != null)
        {
            var allHelperSOs = new System.Collections.Generic.List<FarmHelperSO>();
            if (itemDatabase != null)
            {
                // foreach (var item in itemDatabase.items)
                //     if (item != null && item.farmHelperData != null)
                //         allHelperSOs.Add(item.farmHelperData);
            }
            FarmHelperManager.Instance.ApplySaveData(data.farmHelpers, allHelperSOs.ToArray());
        }

        // Crafting — โหลดสูตรที่เรียนรู้แล้ว
        if (CraftingManager.Instance != null && data.learnedRecipes != null)
            // CraftingManager.Instance.SetLearnedRecipes(data.learnedRecipes);

        // Debt Punishment — โหลด consecutive misses
        if (DebtPunishmentSystem.Instance != null)
            DebtPunishmentSystem.Instance.SetConsecutiveMisses(data.consecutiveMisses);

        // Market Prices
        if (MarketPriceSystem.Instance != null && data.marketPrices != null)
            MarketPriceSystem.Instance.ApplySaveData(data.marketPrices);

        Debug.Log("[GameManager] Farm loaded");
    }
}
