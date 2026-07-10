using UnityEngine;

/// <summary>
/// แท็บ Inventory ของหน้าต่างเมนูรวม — ประกอบจากชิ้นที่ "เป็นอิสระจากกัน" ทุกชิ้น:
///   - InventoryMainUIs (กริดไอเทมที่มีอยู่แล้วในโปรเจกต์ ใช้ซ้ำ ไม่เขียนใหม่)
///   - EquipmentSlotsPanelUI (ช่องอุปกรณ์)
///   - PlayerPreviewUI (โมเดล 3D)
///   - MenuStatBarBinder + StatBarUI (HP / Energy)
/// ตัว InventoryTabView เองแค่ทำหน้าที่ตาม IMenuTabView ไม่ต้องรู้รายละเอียดข้างใน
/// ของแต่ละชิ้น — แต่ละชิ้น self-manage ผ่าน OnEnable/OnDisable ของตัวเอง
/// </summary>
public class InventoryTabView : MonoBehaviour, IMenuTabView
{
    public MenuTabId TabId => MenuTabId.Inventory;
    public bool IsInitialized { get; private set; }

    public void InitializeTab() => IsInitialized = true;

    public void OnTabShown() { /* ลูกๆ bind ข้อมูลเองผ่าน OnEnable ของแต่ละ component */ }

    public void OnTabHidden() { /* ลูกๆ unbind เองผ่าน OnDisable ของแต่ละ component */ }
}
