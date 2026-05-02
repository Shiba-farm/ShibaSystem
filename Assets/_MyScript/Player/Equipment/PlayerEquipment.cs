using UnityEngine;

public class PlayerEquipment : MonoBehaviour
{
    [Header("Setup")]
    [Tooltip("ลาก ToolSocket ที่สร้างไว้ในมือขวามาใส่ตรงนี้")]
    public Transform toolSocket;

    private ItemSO currentItem;
    private GameObject currentModel;

    private void Update()
    {
        // เช็คว่า Hotbar มีอยู่จริงไหม
        if (HotbarUI.Instance == null) return;

        // ดึงไอเทมที่กำลังเลือกอยู่
        ItemSO selectedItem = HotbarUI.Instance.GetSelectedItem();

        // ถ้าไอเทมเปลี่ยนไปจากเดิม ให้เปลี่ยนโมเดล
        if (selectedItem != currentItem)
        {
            EquipItem(selectedItem);
        }
    }

    public void EquipItem(ItemSO newItem)
    {
        currentItem = newItem;

        // 1. ลบโมเดลเก่าทิ้ง (ถ้ามี)
        if (currentModel != null)
        {
            Destroy(currentModel);
            currentModel = null;
        }

        // 2. ถ้าไม่มีไอเทม หรือไอเทมนั้นไม่มีโมเดล 3D ก็จบแค่นี้ (มือเปล่า)
        if (newItem == null || newItem.equipmentPrefab == null)
        {
            return;
        }

        // 3. สร้างโมเดลใหม่ขึ้นมาแปะที่มือ
        currentModel = Instantiate(newItem.equipmentPrefab, toolSocket);

        // รีเซ็ตตำแหน่งให้ตรงกับ Socket เป๊ะๆ
        currentModel.transform.localPosition = Vector3.zero;
        currentModel.transform.localRotation = Quaternion.identity;
        currentModel.transform.localScale = Vector3.one;

        // (Optional) ถ้าโมเดลที่ Import มามันหมุนผิดด้าน คุณอาจจะต้องมาแก้ Rotation เพิ่มเติมตรงนี้
        // หรือแก้ที่ตัว ToolSocket ใน Scene จะง่ายกว่า
    }
}