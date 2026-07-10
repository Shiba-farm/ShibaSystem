using UnityEngine;

public interface IEquippable
{
    EquipSlot Slot { get; }          // Helmet, Ring, Shield, Boots, ...
    void OnEquip(StatManager user);  // apply stat buffs
    void OnUnequip(StatManager user);
}

// ── Equip Slots ──────────────────────────────────────────────────────────────
// ผู้เล่นขอให้ออกแบบเผื่ออนาคต: เพิ่มช่องอุปกรณ์ใหม่ได้โดย "เพิ่มชื่อต่อท้าย" ที่นี่
// ที่เดียว (ห้ามแทรกกลางหรือสลับลำดับ — ค่า int ของ enum ถูกเซฟไว้ใน ItemSO.asset
// และ NetworkEquipment แล้ว) จากนั้นเพิ่มแถวใน EquipmentSlotConfigSO asset เพื่อให้
// UI สร้างช่องใหม่ขึ้นมาเอง — ไม่ต้องแก้โค้ด UI แม้แต่บรรทัดเดียว
public enum EquipSlot { Helmet, Ring, Shield, Boots }