using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ติดตามไอเท็มที่ขายในแต่ละวัน แยกตาม SellCategory
/// SellBox เรียก RecordSale() ทุกครั้งที่ขาย
/// DayEndSystem เรียก GetRecordsByCategory() และ ResetDaily()
/// </summary>
public class DailyEconomyTracker : MonoBehaviour
{
    public static DailyEconomyTracker Instance { get; private set; }

    // ─── Record ───────────────────────────────────────────────────────
    [System.Serializable]
    public class SoldItemRecord
    {
        public string      itemName;
        public Sprite      icon;
        public SellCategory category;
        public int         amount;
        public int         totalPrice;
    }

    // ─── Runtime ──────────────────────────────────────────────────────
    readonly List<SoldItemRecord> _records = new List<SoldItemRecord>();
    public int TotalEarnedToday { get; private set; }

    // ──────────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    // ─── API ──────────────────────────────────────────────────────────

    /// <summary>เรียกจาก SellBox ทุกครั้งที่ขาย</summary>
    public void RecordSale(ItemSO item, int amount, int totalPrice)
    {
        if (item == null) return;

        // Merge ถ้าขายไอเท็มเดิมซ้ำ
        var existing = _records.Find(r => r.itemName == item.itemName);
        if (existing != null)
        {
            existing.amount     += amount;
            existing.totalPrice += totalPrice;
        }
        else
        {
            _records.Add(new SoldItemRecord
            {
                itemName   = item.itemName,
                icon       = item.icon,
                category   = item.sellCategory,
                amount     = amount,
                totalPrice = totalPrice,
            });
        }

        TotalEarnedToday += totalPrice;
    }

    /// <summary>คืน list ไอเท็มใน category นั้น (ว่างถ้าไม่มีการขาย)</summary>
    public List<SoldItemRecord> GetRecordsByCategory(SellCategory cat)
        => _records.FindAll(r => r.category == cat);

    /// <summary>ยอดรวมของ category</summary>
    public int GetCategoryTotal(SellCategory cat)
    {
        int sum = 0;
        foreach (var r in _records)
            if (r.category == cat) sum += r.totalPrice;
        return sum;
    }

    /// <summary>รีเซ็ตสำหรับวันใหม่</summary>
    public void ResetDaily()
    {
        _records.Clear();
        TotalEarnedToday = 0;
    }
}
