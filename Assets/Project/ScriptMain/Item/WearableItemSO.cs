using UnityEngine;

[CreateAssetMenu(menuName = "Items/Wearable")]
public class WearableItemSO : ItemSO
{
    [Header("Wearable")]
    public EquipSlot slot;
    public float defenseBonus;
    public float speedBonus;
    public float attackBonus;
    public GameObject visualPrefab;

    [Header("Visual Offset (ปรับตำแหน่ง/หมุน/ขนาดให้พอดีกับโมเดลโดยไม่ต้องแก้ prefab)")]
    public Vector3 visualPositionOffset = Vector3.zero;
    public Vector3 visualRotationOffset = Vector3.zero;
    public Vector3 visualScale = Vector3.one;

    public EquipSlot Slot => slot;

    // Speed ใช้งานจริงแล้ว — ผ่าน EquipmentData.GetTotalSpeedBonus() ที่ PlayerController
    // เรียกสดทุกเฟรมแทน ไม่ต้อง apply ที่นี่ก็ได้ผลเหมือนกัน (ดู PlayerController.HandleMovement())
    //
    // Defense/Attack: ยังไม่มีระบบต่อสู้/คำนวณดาเมจในเกมตอนนี้ให้ค่านี้ไปมีผล — พอมีระบบนั้นเมื่อไหร่
    // ค่อยมา apply ที่นี่ (เช่นเพิ่ม StatType.Defense/Attack ใน StatManager แล้ว user.RegenStat(...) ตอน equip)
    public void OnEquip(StatManager user)
    {
    }

    public void OnUnequip(StatManager user)
    {
    }
}
