/// <summary>
/// Contract ที่แยก "ตรรกะแสดงผลอุปกรณ์บนโมเดล" ออกจาก UI (Dependency Inversion) —
/// PlayerPreviewUI เรียกผ่าน interface นี้เท่านั้น ไม่รู้จัก rig หรือ rig ของเกมจริงเลย
/// ทำให้ทีม Art/Character เปลี่ยนวิธี implement (เช่นสวมโมเดลจริงแทนเปิด/ปิด
/// child object) โดยไม่กระทบ UI เลย
/// </summary>
public interface IEquipmentVisualApplier
{
    void ApplyVisual(EquipSlot slot, ItemSO equippedItem);
}
