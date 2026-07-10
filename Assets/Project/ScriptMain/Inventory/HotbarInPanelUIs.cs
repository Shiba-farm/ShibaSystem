/// <summary>
/// แสดง Hotbar slots (inventoryID = 1) ภายใน Inventory Panel
/// ให้ drag-drop ระหว่าง Bag (inventoryID=0) ↔ Hotbar (inventoryID=1) ได้
///
/// วิธีตั้งค่าใน Unity Inspector:
///   - สร้าง GameObject ใน Inventory Panel ชื่อ "HotbarSection"
///   - ใส่ HorizontalLayoutGroup + ContentSizeFitter
///   - Attach script นี้
///   - inventoryID         = 1  (hotbar data ID)
///   - connectionSignal    = HotbarInventoryDataSignal (signal เดียวกับ HUD hotbar)
///   - slotPrefab          = InventorySlot prefab
///   - interactionMode     = DragDrop
///   - onlyShowUnEmptySlots = false
/// </summary>
public class HotbarInPanelUIs : InventoryMainUIs
{
    // base class (InventoryMainUIs) จัดการ data binding, slot population,
    // refresh และ drag-drop ให้ครบแล้ว — ไม่ต้องเพิ่ม logic ที่นี่
    //
    // ต่างจาก HotbarUIController ตรงที่:
    // - ไม่รับ keyboard/scroll input
    // - ไม่ทำ SetSelected highlight (นั่นคือ HUD hotbar)
    // - เปิด/ปิดพร้อม Inventory Panel
}
