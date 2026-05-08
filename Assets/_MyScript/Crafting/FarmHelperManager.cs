using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// จัดการตัวช่วยฟาร์มทั้งหมดที่วางไว้
/// - Spawn/Remove helpers
/// - ทุกเช้าเรียก Apply Effects (เช่น AutoWater)
/// - Save/Load helpers
/// - ให้ DebtPunishmentSystem เข้ามาทำลายได้
/// </summary>
public class FarmHelperManager : MonoBehaviour
{
    public static FarmHelperManager Instance { get; private set; }

    [Header("Refs")]
    [Tooltip("อ้างถึง SoilTile ทั้งหมด (หรือ FindAll ได้)")]
    public Transform farmArea;

    [Header("Config")]
    [Tooltip("จำนวนตัวช่วยสูงสุดที่วางได้")]
    public int maxHelpers = 20;

    [Header("Runtime")]
    [SerializeField] private List<FarmHelper> placedHelpers = new List<FarmHelper>();

    /// <summary>ตัวช่วยทั้งหมดที่วางอยู่</summary>
    public IReadOnlyList<FarmHelper> PlacedHelpers => placedHelpers;

    /// <summary>จำนวนตัวช่วยที่วางอยู่</summary>
    public int PlacedCount => placedHelpers.Count;

    // === Events ===
    /// <summary>เมื่อมีตัวช่วยถูกวาง/ลบ</summary>
    public event Action<FarmHelper> OnHelperPlaced;
    public event Action<FarmHelper> OnHelperRemoved;
    public event Action<FarmHelper> OnHelperDestroyed; // ถูกทำลายโดยลูกน้องเจ้าหนี้

    // ================================================================
    // Lifecycle
    // ================================================================

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[FarmHelperManager] พบ Instance ซ้ำบน '{gameObject.name}' — ลบ Component นี้ออก");
            Destroy(this);   // ลบแค่ Component ไม่ทำลาย GameObject ทั้งก้อน
            return;
        }
        Instance = this;
    }

    void OnEnable()
    {
        // Subscribe ทุกเช้า
        var cal = FindObjectOfType<CalendarSystem>();
        // if (cal != null) cal.OnDayEnded += OnNewDay;
    }

    void OnDisable()
    {
        var cal = FindObjectOfType<CalendarSystem>();
        // if (cal != null) cal.OnDayEnded -= OnNewDay;
    }

    // ================================================================
    // วางตัวช่วย (Place)
    // ================================================================

    /// <summary>
    /// วางตัวช่วยใหม่ลงในฟาร์ม
    /// </summary>
    /// <param name="helperSO">ข้อมูลตัวช่วย</param>
    /// <param name="worldPos">ตำแหน่งในโลก</param>
    /// <returns>FarmHelper ที่สร้างได้ (null = เต็ม)</returns>
    public FarmHelper PlaceHelper(FarmHelperSO helperSO, Vector3 worldPos)
    {
        if (helperSO == null || helperSO.placementPrefab == null)
        {
            Debug.LogWarning("[FarmHelper] ไม่มี placementPrefab!");
            return null;
        }

        if (placedHelpers.Count >= maxHelpers)
        {
            Debug.LogWarning($"[FarmHelper] วางเต็มแล้ว! ({maxHelpers})");
            return null;
        }

        // Spawn prefab
        GameObject obj = Instantiate(helperSO.placementPrefab, worldPos, Quaternion.identity);
        FarmHelper helper = obj.GetComponent<FarmHelper>();
        if (helper == null)
            helper = obj.AddComponent<FarmHelper>();

        helper.helperData = helperSO;
        helper.daysUsed = 0;
        helper.uniqueId = Guid.NewGuid().ToString("N").Substring(0, 8);

        placedHelpers.Add(helper);

        Debug.Log($"[FarmHelper] วาง {helperSO.helperName} ที่ {worldPos}, total={placedHelpers.Count}");
        OnHelperPlaced?.Invoke(helper);

        return helper;
    }

    /// <summary>ลบตัวช่วย (ไม่เล่น VFX — ใช้ตอนเก็บคืน)</summary>
    public void RemoveHelper(FarmHelper helper)
    {
        if (helper == null) return;
        placedHelpers.Remove(helper);
        OnHelperRemoved?.Invoke(helper);
    }

    // ================================================================
    // Daily Effect — ทุกเช้า Apply Effect ตัวช่วยทั้งหมด
    // ================================================================

    void OnNewDay(Date d)
    {
        SoilTile[] allTiles = FindObjectsOfType<SoilTile>();

        for (int i = placedHelpers.Count - 1; i >= 0; i--)
        {
            var helper = placedHelpers[i];
            if (helper == null) { placedHelpers.RemoveAt(i); continue; }

            // เพิ่มวัน
            helper.AddDay();

            // ถ้าพัง → ข้าม
            if (helper.IsBroken)
            {
                Debug.Log($"[FarmHelper] {helper.helperData.helperName} พังแล้ว! ต้องซ่อม");
                continue;
            }

            // Apply effect ไปทุก tile ที่อยู่ในรัศมี
            foreach (var tile in allTiles)
            {
                if (helper.IsInRange(tile.transform.position))
                    helper.ApplyEffectToTile(tile);
            }
        }
    }

    // ================================================================
    // Growth Multiplier — เช็คว่า tile ได้ bonus จาก Fertilizer ไหม
    // ================================================================

    /// <summary>
    /// คำนวณ growth multiplier สำหรับ SoilTile (เรียกจาก SoilTile.Update)
    /// ค่าเริ่มต้น = 1.0, Fertilizer เพิ่มตาม effectValue
    /// </summary>
    public float GetGrowthMultiplier(Vector3 tilePos)
    {
        float multiplier = 1f;
        foreach (var helper in placedHelpers)
        {
            if (helper == null || helper.IsBroken) continue;
            if (helper.helperData.effectType != HelperEffectType.Fertilizer) continue;
            if (helper.IsInRange(tilePos))
                multiplier += helper.helperData.effectValue;
        }
        return multiplier;
    }

    /// <summary>
    /// ตรวจสอบว่า SoilTile ได้รับการป้องกันจาก Scarecrow หรือไม่
    /// </summary>
    public bool IsProtectedByScarecrow(Vector3 tilePos)
    {
        foreach (var helper in placedHelpers)
        {
            if (helper == null || helper.IsBroken) continue;
            if (helper.helperData.effectType != HelperEffectType.Scarecrow) continue;
            if (helper.IsInRange(tilePos)) return true;
        }
        return false;
    }

    /// <summary>
    /// ตรวจสอบว่า SoilTile ได้รับการป้องกันจาก Fence (พายุ/แมลง) หรือไม่
    /// </summary>
    public bool IsProtectedByFence(Vector3 tilePos)
    {
        foreach (var helper in placedHelpers)
        {
            if (helper == null || helper.IsBroken) continue;
            if (helper.helperData.effectType != HelperEffectType.Fence) continue;
            if (helper.IsInRange(tilePos)) return true;
        }
        return false;
    }

    // ================================================================
    // Punishment — ลูกน้องเจ้าหนี้ทำลาย
    // ================================================================

    /// <summary>
    /// ทำลายตัวช่วยจำนวน count ตัว (เรียกจาก DebtPunishmentSystem)
    /// ทำลายตัวที่มี priority สูงก่อน
    /// </summary>
    /// <returns>จำนวนที่ทำลายจริง</returns>
    public int DestroyHelpers(int count)
    {
        if (placedHelpers.Count == 0) return 0;

        // sort by destructionPriority (สูง = ถูกทำลายก่อน)
        List<FarmHelper> sortedTargets = new List<FarmHelper>(placedHelpers);
        sortedTargets.Sort((a, b) =>
        {
            int pa = a.helperData != null ? a.helperData.destructionPriority : 0;
            int pb = b.helperData != null ? b.helperData.destructionPriority : 0;
            return pb.CompareTo(pa); // สูงก่อน
        });

        int destroyed = 0;
        for (int i = 0; i < Mathf.Min(count, sortedTargets.Count); i++)
        {
            var target = sortedTargets[i];
            if (target == null) continue;

            OnHelperDestroyed?.Invoke(target);
            target.DestroyByHenchman();
            destroyed++;
        }

        Debug.Log($"[Punishment] ทำลายตัวช่วย {destroyed}/{count} ตัว, เหลือ {placedHelpers.Count} ตัว");
        return destroyed;
    }

    /// <summary>ทำลายทั้งหมด</summary>
    public int DestroyAllHelpers()
    {
        return DestroyHelpers(placedHelpers.Count);
    }

    // ================================================================
    // Save / Load
    // ================================================================

    public FarmHelperData[] GetSaveData()
    {
        var data = new FarmHelperData[placedHelpers.Count];
        for (int i = 0; i < placedHelpers.Count; i++)
        {
            var h = placedHelpers[i];
            data[i] = new FarmHelperData
            {
                helperName = h.helperData != null ? h.helperData.helperName : "",
                posX = h.transform.position.x,
                posY = h.transform.position.y,
                posZ = h.transform.position.z,
                rotY = h.transform.eulerAngles.y,
                daysUsed = h.daysUsed,
                uniqueId = h.uniqueId
            };
        }
        return data;
    }

    public void ApplySaveData(FarmHelperData[] data, FarmHelperSO[] allHelperSOs)
    {
        // ลบตัวเก่าทั้งหมด
        foreach (var h in placedHelpers)
            if (h != null) Destroy(h.gameObject);
        placedHelpers.Clear();

        if (data == null) return;

        foreach (var d in data)
        {
            // หา SO ที่ตรงกัน
            FarmHelperSO so = null;
            foreach (var s in allHelperSOs)
            {
                if (s.helperName == d.helperName) { so = s; break; }
            }
            if (so == null || so.placementPrefab == null) continue;

            Vector3 pos = new Vector3(d.posX, d.posY, d.posZ);
            FarmHelper helper = PlaceHelper(so, pos);
            if (helper != null)
            {
                helper.daysUsed = d.daysUsed;
                helper.uniqueId = d.uniqueId;
                helper.transform.eulerAngles = new Vector3(0, d.rotY, 0);
            }
        }
    }
}
