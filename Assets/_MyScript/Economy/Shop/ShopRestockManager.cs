using UnityEngine;

/// <summary>
/// จัดการ Restock ร้านค้าทั้งหมดตาม Calendar
/// - นับวัน → ถ้าครบ restockIntervalDays → เติมสต๊อก
/// </summary>
public class ShopRestockManager : MonoBehaviour
{
    public static ShopRestockManager Instance { get; private set; }

    [Header("All Shop Catalogs")]
    [Tooltip("ลาก ShopDefinition ทั้งหมดมาใส่ — จะ restock อัตโนมัติ")]
    public ShopDefinition[] allShops;

    [Header("Runtime")]
    [SerializeField] private int daysSinceLastRestock;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Initialize stock ครั้งแรก
        if (allShops != null)
            foreach (var shop in allShops)
                if (shop != null) shop.InitializeStock();
    }

    void OnEnable()
    {
        var cal = FindObjectOfType<CalendarSystem>();
        if (cal != null) cal.OnDayEnded += OnNewDay;
    }

    void OnDisable()
    {
        var cal = FindObjectOfType<CalendarSystem>();
        if (cal != null) cal.OnDayEnded -= OnNewDay;
    }

    void OnNewDay(Date d)
    {
        daysSinceLastRestock++;

        if (allShops == null) return;

        foreach (var shop in allShops)
        {
            if (shop == null) continue;
            if (shop.restockIntervalDays <= 0) continue; // ไม่จำกัดสต๊อก

            if (daysSinceLastRestock >= shop.restockIntervalDays)
            {
                shop.RestockAll();
                Debug.Log($"[Shop] {shop.shopName} เติมสต๊อกแล้ว!");
            }
        }

        // Reset counter ถ้าครบ cycle
        // ใช้ max interval เพื่อ reset
        int maxInterval = 1;
        foreach (var shop in allShops)
            if (shop != null && shop.restockIntervalDays > maxInterval)
                maxInterval = shop.restockIntervalDays;

        if (daysSinceLastRestock >= maxInterval)
            daysSinceLastRestock = 0;
    }

    // Save / Load
    public int GetDaysSinceRestock() => daysSinceLastRestock;
    public void SetDaysSinceRestock(int val) => daysSinceLastRestock = val;
}
