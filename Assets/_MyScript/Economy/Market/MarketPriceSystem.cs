using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ระบบราคาตลาด (Supply & Demand)
/// - ราคาซื้อ/ขายเปลี่ยนแปลงตามจำนวนที่ผู้เล่นขายเข้าตลาด
/// - ขายของมากชนิดเดียว → ราคาตก / ขายของหายาก → ราคาสูง
/// - ราคาค่อย ๆ ฟื้นตัว (recover) กลับเข้าหาราคาฐานทุกวัน
/// - รองรับ Seasonal Bonus (ฤดูกาลต่าง ๆ ราคาเพิ่ม/ลด)
/// </summary>
public class MarketPriceSystem : MonoBehaviour
{
    public static MarketPriceSystem Instance { get; private set; }

    [Header("Config")]
    [Tooltip("ราคาลดสูงสุดกี่% เมื่อ oversupply (0.3 = ลดได้สูงสุด 70%)")]
    [Range(0.1f, 0.9f)]
    public float minPriceMultiplier = 0.3f;

    [Tooltip("ราคาเพิ่มสูงสุดกี่เท่าเมื่อ demand สูง")]
    [Range(1f, 3f)]
    public float maxPriceMultiplier = 2.0f;

    [Tooltip("จำนวนที่ขายก่อนราคาเริ่มลด (threshold)")]
    [Min(5)]
    public int supplyThreshold = 10;

    [Tooltip("ตัวหาร — ยิ่งสูง ราคายิ่งลดช้า")]
    [Min(1f)]
    public float supplyDivisor = 20f;

    [Tooltip("อัตราฟื้นตัวต่อวัน (0.1 = ฟื้นวันละ 10%)")]
    [Range(0.01f, 0.5f)]
    public float dailyRecoveryRate = 0.1f;

    [Header("Runtime (read-only)")]
    [SerializeField] private List<MarketItemState> marketStates = new List<MarketItemState>();

    /// <summary>ราคาตลาดเปลี่ยน</summary>
    public event Action OnMarketUpdated;

    // ================================================================
    // Lifecycle
    // ================================================================

    void Awake()
    {
        if (Instance != null && Instance != this) { Debug.LogWarning($"[MarketPriceSystem] พบ Instance ซ้ำบน '{gameObject.name}' — ลบ Component"); Destroy(this); return; }
        Instance = this;
    }

    void OnEnable()
    {
        var cal = FindObjectOfType<CalendarSystem>();
        // if (cal != null) cal.OnDayEnded += OnNewDay;
    }

    void OnDisable()
    {
        var cal = FindObjectOfType<CalendarSystem>();
        // if (cal != null) cal.OnDayEnded -= OnNewDay;
    }

    // ================================================================
    // Get / Set Market State
    // ================================================================

    MarketItemState GetState(string itemName)
    {
        foreach (var s in marketStates)
            if (s.itemName == itemName) return s;
        return null;
    }

    MarketItemState GetOrCreateState(string itemName)
    {
        var s = GetState(itemName);
        if (s == null)
        {
            s = new MarketItemState { itemName = itemName, totalSold = 0, priceMultiplier = 1f };
            marketStates.Add(s);
        }
        return s;
    }

    // ================================================================
    // Price Calculation
    // ================================================================

    /// <summary>
    /// คำนวณราคาขาย (Sell Price) ตามตลาด
    /// basePrice × marketMultiplier × sellMultiplier
    /// </summary>
    public int GetSellPrice(ItemSO item, float shopMultiplier = 1f)
    {
        if (item == null || !item.sellable) return 0;
        float mult = GetPriceMultiplier(item.itemName);
        return Mathf.Max(1, Mathf.RoundToInt(item.sellPrice * mult * shopMultiplier));
    }

    /// <summary>
    /// คำนวณราคาซื้อ (Buy Price) ตามตลาด
    /// basePrice ใช้ราคาจาก ShopDefinition.Entry.price
    /// ราคาซื้อจะ inverse กับ sell — ของหายาก (ขายน้อย) → ซื้อถูก, ของล้นตลาด → ซื้อแพง
    /// (ในเกมนี้ buy price ยังคงใช้ fixed price จาก shop แต่เพิ่ม demand bonus ได้)
    /// </summary>
    public int GetBuyPrice(string itemName, int basePrice)
    {
        // Buy price ไม่ลดตาม supply — แต่ถ้า demand สูง (ของหาย) อาจแพงขึ้นได้
        // สำหรับ V1 ใช้ราคาเดิมก่อน เพิ่มในอนาคต
        return basePrice;
    }

    /// <summary>
    /// ตัวคูณราคาปัจจุบันของไอเท็ม (1.0 = ปกติ, <1 = ราคาตก, >1 = ราคาสูง)
    /// </summary>
    public float GetPriceMultiplier(string itemName)
    {
        var state = GetState(itemName);
        if (state == null) return 1f;
        return Mathf.Clamp(state.priceMultiplier, minPriceMultiplier, maxPriceMultiplier);
    }

    /// <summary>
    /// แสดง trend (ลูกศร) — ราคากำลังขึ้นหรือลง
    /// </summary>
    public PriceTrend GetTrend(string itemName)
    {
        var state = GetState(itemName);
        if (state == null) return PriceTrend.Stable;

        if (state.priceMultiplier < 0.85f) return PriceTrend.Down;
        if (state.priceMultiplier > 1.15f) return PriceTrend.Up;
        return PriceTrend.Stable;
    }

    // ================================================================
    // Recording Sales — เรียกเมื่อผู้เล่นขาย
    // ================================================================

    /// <summary>
    /// บันทึกว่าผู้เล่นขายไอเท็มนี้ → ราคาลดลงตาม supply
    /// </summary>
    public void RecordSale(string itemName, int amount)
    {
        var state = GetOrCreateState(itemName);
        state.totalSold += amount;
        state.soldToday += amount;

        // คำนวณ multiplier ใหม่
        RecalculateMultiplier(state);

        Debug.Log($"[Market] ขาย {itemName} x{amount}, totalSold={state.totalSold}, mult={state.priceMultiplier:F2}");
        OnMarketUpdated?.Invoke();
    }

    void RecalculateMultiplier(MarketItemState state)
    {
        if (state.totalSold <= supplyThreshold)
        {
            state.priceMultiplier = 1f;
            return;
        }

        // ราคาลดตาม: 1 - (oversupply / divisor)
        float oversupply = state.totalSold - supplyThreshold;
        float reduction = oversupply / supplyDivisor;
        state.priceMultiplier = Mathf.Clamp(1f - reduction, minPriceMultiplier, maxPriceMultiplier);
    }

    // ================================================================
    // Daily Recovery — ราคาค่อย ๆ ฟื้นตัว
    // ================================================================

    void OnNewDay(Date d)
    {
        bool changed = false;

        for (int i = marketStates.Count - 1; i >= 0; i--)
        {
            var state = marketStates[i];

            // Reset daily counter
            state.soldToday = 0;

            // ฟื้นตัว — ค่อย ๆ กลับไป 1.0
            if (Mathf.Abs(state.priceMultiplier - 1f) > 0.01f)
            {
                float diff = 1f - state.priceMultiplier;
                state.priceMultiplier += diff * dailyRecoveryRate;

                // ลด totalSold ให้ sync กับ multiplier ใหม่
                if (state.totalSold > 0)
                    state.totalSold = Mathf.Max(0, state.totalSold - Mathf.CeilToInt(supplyThreshold * dailyRecoveryRate));

                changed = true;
            }

            // ถ้าฟื้นจน multiplier ≈ 1 และ totalSold = 0 → ลบออก
            if (Mathf.Abs(state.priceMultiplier - 1f) < 0.02f && state.totalSold <= 0)
            {
                marketStates.RemoveAt(i);
                changed = true;
            }
        }

        if (changed) OnMarketUpdated?.Invoke();
    }

    // ================================================================
    // Save / Load
    // ================================================================

    public MarketItemData[] GetSaveData()
    {
        var data = new MarketItemData[marketStates.Count];
        for (int i = 0; i < marketStates.Count; i++)
        {
            var s = marketStates[i];
            data[i] = new MarketItemData
            {
                itemName = s.itemName,
                totalSold = s.totalSold,
                priceMultiplier = s.priceMultiplier
            };
        }
        return data;
    }

    public void ApplySaveData(MarketItemData[] data)
    {
        marketStates.Clear();
        if (data == null) return;

        foreach (var d in data)
        {
            marketStates.Add(new MarketItemState
            {
                itemName = d.itemName,
                totalSold = d.totalSold,
                priceMultiplier = d.priceMultiplier,
                soldToday = 0
            });
        }
    }

    // ================================================================
    // Debug
    // ================================================================

    [ContextMenu("Debug/Print All Market Prices")]
    void DebugPrintPrices()
    {
        foreach (var s in marketStates)
            Debug.Log($"[Market] {s.itemName}: mult={s.priceMultiplier:F2}, totalSold={s.totalSold}");
    }

    [ContextMenu("Debug/Reset All Prices")]
    void DebugResetPrices()
    {
        marketStates.Clear();
        OnMarketUpdated?.Invoke();
        Debug.Log("[Market] Reset all prices to 1.0");
    }
}

/// <summary>สถานะตลาดของไอเท็มแต่ละชนิด (runtime)</summary>
[Serializable]
public class MarketItemState
{
    public string itemName;
    public int totalSold;     // จำนวนรวมที่ขายเข้าตลาด
    public int soldToday;     // จำนวนที่ขายวันนี้
    public float priceMultiplier = 1f;
}

/// <summary>สถานะตลาดสำหรับ Save</summary>
[Serializable]
public class MarketItemData
{
    public string itemName;
    public int totalSold;
    public float priceMultiplier;
}

/// <summary>แนวโน้มราคา</summary>
public enum PriceTrend
{
    Up,     // ราคาสูง (demand > supply)
    Stable, // ราคาปกติ
    Down    // ราคาตก (oversupply)
}
