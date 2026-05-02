using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// ระบบถังขยะใน Inventory
///
/// วิธีใช้:
///   • Right-click ที่ Inventory Slot → popup ถามยืนยัน
///   • กด "ทิ้ง" → ลบสิ่งของออก
///   • กด "ยกเลิก" หรือ ESC → ปิด popup
///
/// Setup ใน Inspector:
///   1. สร้าง UI Panel ชื่อ "TrashConfirmPanel" ใน Canvas
///      ├── TextMeshPro "ConfirmText" — "ทิ้ง [Onion] x99 ?"
///      ├── Button "ConfirmBtn"  — "ทิ้ง"
///      └── Button "CancelBtn"  — "ยกเลิก"
///   2. ติด InventoryTrash script ไว้ที่ GameObject ใดก็ได้ใน Scene
///   3. ลาก refs ใน Inspector
/// </summary>
public class InventoryTrash : MonoBehaviour
{
    public static InventoryTrash Instance { get; private set; }

    [Header("Confirm Popup")]
    public GameObject confirmPanel;
    public TextMeshProUGUI confirmText;
    public Button confirmBtn;
    public Button cancelBtn;

    // ─── Runtime ──────────────────────────────────────────────────────
    Action _pendingConfirm;
    bool   _isOpen;

    // ──────────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;

        if (confirmPanel) confirmPanel.SetActive(false);
        if (confirmBtn)   confirmBtn.onClick.AddListener(OnConfirm);
        if (cancelBtn)    cancelBtn.onClick.AddListener(OnCancel);
    }

    void Update()
    {
        if (_isOpen && Input.GetKeyDown(KeyCode.Escape))
            OnCancel();
    }

    // ─── API ──────────────────────────────────────────────────────────

    /// <summary>
    /// เรียกจาก InventorySlot / HotbarSlot เมื่อ right-click
    /// onConfirm = lambda ที่ล้าง slot นั้นจริงๆ
    /// </summary>
    public void RequestDelete(ItemSO item, int amount, bool isHotbar, Action onConfirm)
    {
        if (item == null) return;

        _pendingConfirm = onConfirm;
        _isOpen = true;

        if (confirmText)
            confirmText.text = $"ทิ้ง  {item.itemName}  x{amount}  ?";

        if (confirmPanel) confirmPanel.SetActive(true);
    }

    // ─── Buttons ──────────────────────────────────────────────────────

    void OnConfirm()
    {
        _pendingConfirm?.Invoke();
        _pendingConfirm = null;
        Close();
    }

    void OnCancel()
    {
        _pendingConfirm = null;
        Close();
    }

    void Close()
    {
        _isOpen = false;
        if (confirmPanel) confirmPanel.SetActive(false);
    }
}
