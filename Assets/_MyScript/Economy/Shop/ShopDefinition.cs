using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "ShibaFarm/Shop Definition")]
public class ShopDefinition : ScriptableObject
{
    [Header("Shop Info")]
    [Tooltip("ชื่อร้าน (แสดงบน header)")]
    public string shopName = "Shop";

    [Tooltip("ชื่อ NPC พ่อค้า (optional)")]
    public string merchantName;

    [Tooltip("Portrait NPC (optional)")]
    public Sprite merchantPortrait;

    [Header("Restock Config")]
    [Tooltip("ร้านเติมสต๊อกทุกกี่วัน (0 = ไม่จำกัดสต๊อก)")]
    [Min(0)]
    public int restockIntervalDays = 3;

    [Serializable]
    public class Entry
    {
        public ItemSO item;
        [Min(0)] public int price = 10;
        [Min(1)] public int maxPerClick = 10;

        [Header("Category")]
        public ShopCategory category = ShopCategory.Others;

        [Header("Stock")]
        [Tooltip("สต๊อกเริ่มต้น / สต๊อกเมื่อ restock (-1 = ไม่จำกัด)")]
        public int maxStock = -1;

        [Tooltip("มีเฉพาะบางวัน? (ว่างเปล่า = มีทุกวัน)")]
        public DayAvailability availability;

        [HideInInspector]
        public int currentStock; // runtime — จำนวนที่เหลืออยู่
    }

    [Serializable]
    public class DayAvailability
    {
        [Tooltip("มีเฉพาะวันไหนของสัปดาห์ (0=Mon..6=Sun), ว่าง = ทุกวัน")]
        public int[] availableDays;

        public bool IsAvailableToday(int dayOfWeek)
        {
            if (availableDays == null || availableDays.Length == 0) return true;
            foreach (var d in availableDays)
                if (d == dayOfWeek) return true;
            return false;
        }
    }

    public List<Entry> items = new List<Entry>();

    /// <summary>Reset สต๊อกทั้งร้าน</summary>
    public void RestockAll()
    {
        foreach (var e in items)
        {
            if (e.maxStock > 0)
                e.currentStock = e.maxStock;
        }
    }

    /// <summary>เรียกตอน Awake / Game Start ครั้งแรก</summary>
    public void InitializeStock()
    {
        foreach (var e in items)
        {
            if (e.maxStock > 0)
                e.currentStock = e.maxStock;
            else
                e.currentStock = -1; // ไม่จำกัด
        }
    }
}
