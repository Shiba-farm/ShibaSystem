using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// รายการช่องอุปกรณ์ที่จะแสดงในหน้า Inventory (เช่น Helmet, Ring, Shield, Boots
/// ตาม mockup) — เป็น ScriptableObject เพื่อให้ "เพิ่ม/ลบ/เรียงช่องใหม่" ทำได้จาก
/// Inspector ล้วน ๆ โดยไม่ต้องแก้ EquipmentSlotsPanelUI หรือ prefab เลย
/// ขั้นตอนเพิ่มช่องใหม่ในอนาคต:
///   1) เพิ่มชื่อต่อท้าย enum EquipSlot (ดูคอมเมนต์ใน IEquippable.cs)
///   2) เพิ่มแถวใหม่ใน asset นี้ (Slot, DisplayName, EmptyIcon)
///   เท่านั้น — UI จะสร้างช่องใหม่ให้เองตอน runtime
/// </summary>
[CreateAssetMenu(menuName = "ShibaFarm/Menu/Equipment Slot Config")]
public class EquipmentSlotConfigSO : ScriptableObject
{
    [System.Serializable]
    public class SlotEntry
    {
        public EquipSlot slot;
        public string displayName;
        public Sprite emptyIcon;
    }

    public List<SlotEntry> slots = new();
}
