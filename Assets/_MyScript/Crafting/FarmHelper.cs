using UnityEngine;

/// <summary>
/// MonoBehaviour ที่ติดบน Prefab ตัวช่วยฟาร์มที่วางไว้ในโลก
/// FarmHelperManager จัดการ spawn/destroy — ตัวนี้เก็บข้อมูล runtime
/// </summary>
public class FarmHelper : MonoBehaviour
{
    [Header("Data (ตั้งค่าจาก FarmHelperManager)")]
    public FarmHelperSO helperData;

    [Header("Runtime")]
    [Tooltip("จำนวนวันที่ใช้แล้ว (สำหรับคำนวณ durability)")]
    public int daysUsed;

    [Tooltip("ID ไม่ซ้ำ — ใช้สำหรับ Save/Load")]
    public string uniqueId;

    /// <summary>ตำแหน่ง Grid (สำหรับ Save)</summary>
    public Vector3 PlacedPosition => transform.position;

    /// <summary>ความทนทานเหลือกี่วัน (-1 = ไม่เสื่อม)</summary>
    public int RemainingDurability
    {
        get
        {
            if (helperData == null || helperData.durabilityDays < 0) return -1;
            return Mathf.Max(0, helperData.durabilityDays - daysUsed);
        }
    }

    /// <summary>ตัวช่วยพังแล้วหรือยัง</summary>
    public bool IsBroken => helperData != null
                         && helperData.durabilityDays > 0
                         && daysUsed >= helperData.durabilityDays;

    /// <summary>เพิ่มวันที่ใช้ (เรียกทุกวัน)</summary>
    public void AddDay()
    {
        daysUsed++;
    }

    /// <summary>ซ่อม (reset วันที่ใช้)</summary>
    public void Repair()
    {
        daysUsed = 0;
    }

    // ================================================
    // Effect Application — เรียกจาก FarmHelperManager
    // ================================================

    /// <summary>
    /// ใช้ผลกับ SoilTile ที่อยู่ในรัศมี
    /// </summary>
    public void ApplyEffectToTile(SoilTile tile)
    {
        if (helperData == null || IsBroken) return;

        switch (helperData.effectType)
        {
            case HelperEffectType.AutoWater:
                // รดน้ำอัตโนมัติ
                if (!tile.isWatered)
                {
                    tile.Water();
                    Debug.Log($"[FarmHelper] {helperData.helperName} รดน้ำ tile ที่ {tile.transform.position}");
                }
                break;

            case HelperEffectType.Fertilizer:
                // เพิ่มความเร็วโต — จะถูกใช้ใน SoilTile.Update() ผ่าน GetGrowthMultiplier()
                break;
        }
    }

    /// <summary>
    /// ตรวจสอบว่า SoilTile อยู่ในรัศมีทำงานไหม
    /// </summary>
    public bool IsInRange(Vector3 tilePos)
    {
        if (helperData == null) return false;
        float dist = Vector3.Distance(
            new Vector3(transform.position.x, 0, transform.position.z),
            new Vector3(tilePos.x, 0, tilePos.z)
        );
        // 1 tile ≈ 1 unit
        return dist <= helperData.effectRadius + 0.5f;
    }

    // ================================================
    // ถูกทำลายโดยลูกน้องเจ้าหนี้
    // ================================================

    /// <summary>
    /// เล่น VFX ทำลาย แล้วลบออก
    /// </summary>
    public void DestroyByHenchman()
    {
        Debug.Log($"[Punishment] ลูกน้องเจ้าหนี้ทำลาย {helperData.helperName} ที่ {PlacedPosition}!");

        // TODO: เล่น particle effect ทำลาย + SFX
        // Instantiate(destroyVFX, transform.position, Quaternion.identity);

        // ลบจาก Manager ก่อน destroy
        if (FarmHelperManager.Instance != null)
            FarmHelperManager.Instance.RemoveHelper(this);

        Destroy(gameObject);
    }
}
