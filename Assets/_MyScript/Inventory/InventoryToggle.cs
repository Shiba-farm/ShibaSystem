using UnityEngine;

/// <summary>
/// [หมายเหตุ] Script นี้ซ้ำกับ InventoryUI.cs ซึ่งมี Update() ที่รับ กด I อยู่แล้ว
/// ถ้าใช้ InventoryUI อยู่แล้ว ให้ลบ Component นี้ออกจาก Scene ได้เลย
///
/// Script นี้จะ delegate ให้ InventoryUI.Toggle() แทน เพื่อป้องกัน toggle ซ้ำ
/// </summary>
public class InventoryToggle : MonoBehaviour
{
    [Tooltip("ถ้าใช้ InventoryUI อยู่แล้ว → ปิด Component นี้ได้เลย")]
    public bool enableManualToggle = false;

    public GameObject inventoryPanel; // (optional) ใช้แค่ถ้า InventoryUI ไม่มีใน Scene

    void Start()
    {
        // ถ้ามี InventoryUI อยู่ใน Scene ให้ disable ตัวเองทันที (ป้องกัน toggle ซ้ำ)
        if (InventoryMainUI.Instance != null)
        {
            if (enableManualToggle)
                Debug.LogWarning("[InventoryToggle] ตรวจพบ InventoryUI — ปิด InventoryToggle เพื่อป้องกัน toggle ซ้ำ");
            enabled = false;
            return;
        }

        // Fallback: ไม่มี InventoryUI → ซ่อน panel เอง
        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);
    }

    void Update()
    {
        // เผื่อไม่มี InventoryUI — ควบคุม panel ตรง ๆ
        if (Input.GetKeyDown(KeyCode.I))
        {
            if (InventoryMainUI.Instance != null)
                InventoryMainUI.Instance.Toggle();
            else if (inventoryPanel != null)
                inventoryPanel.SetActive(!inventoryPanel.activeSelf);
        }
    }
}
