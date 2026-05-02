using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI กล่องเก็บของ — แสดง 2 ฝั่ง
///   ซ้าย  : slot ในกล่อง         คลิก → ดึงออกมาใส่ Inventory
///   ขวา   : Inventory + Hotbar  คลิก → ใส่เข้ากล่อง
///
/// Setup ใน Canvas:
///   StoragePanel
///     ├── TitleText (TMP)           — "กล่องเก็บของ"
///     ├── CloseButton (Button)
///     ├── StorageGrid (GridLayout)  ← slot กล่อง
///     └── InventoryGrid (GridLayout)← slot player
///
/// Prefab สำหรับ Grid Slot (ใช้ร่วมกันได้):
///   StorageSlotPrefab
///     ├── Icon   (Image)
///     ├── Amount (TMP)
///     └── Button (component บน root)
/// </summary>
public class StorageUI : MonoBehaviour
{
    public static StorageUI Instance { get; private set; }

    // ─── Refs ─────────────────────────────────────────────────────────
    [Header("Panel")]
    public GameObject storagePanel;
    public TextMeshProUGUI titleText;
    public Button closeButton;

    [Header("Storage Side (ซ้าย — slot ในกล่อง)")]
    public Transform storageGrid;
    public GameObject storageSlotPrefab;

    [Header("Inventory Side (ขวา — ของ player)")]
    public Transform inventoryGrid;
    public GameObject inventorySlotPrefab;

    [Header("Feedback")]
    public TextMeshProUGUI feedbackLabel;
    public float feedbackDuration = 2f;

    // ─── Runtime ──────────────────────────────────────────────────────
    public StorageBox CurrentBox { get; private set; }
    public bool       IsOpen     { get; private set; }

    readonly List<GameObject> _storageObjs  = new List<GameObject>();
    readonly List<GameObject> _inventoryObjs = new List<GameObject>();
    float _feedbackTimer;

    // ──────────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;

        if (storagePanel) storagePanel.SetActive(false);
        if (feedbackLabel) feedbackLabel.text = "";
        if (closeButton) closeButton.onClick.AddListener(Close);
    }

    void Update()
    {
        if (_feedbackTimer > 0)
        {
            _feedbackTimer -= Time.deltaTime;
            if (_feedbackTimer <= 0 && feedbackLabel) feedbackLabel.text = "";
        }

        if (IsOpen && Input.GetKeyDown(KeyCode.Escape))
            Close();
    }

    // ─── Open / Close ─────────────────────────────────────────────────

    public void Open(StorageBox box)
    {
        CurrentBox = box;
        IsOpen     = true;

        Cursor.visible   = true;
        Cursor.lockState = CursorLockMode.None;

        if (titleText) titleText.text = "กล่องเก็บของ";
        if (storagePanel) storagePanel.SetActive(true);

        Refresh();
    }

    public void Close()
    {
        IsOpen     = false;
        CurrentBox = null;

        if (storagePanel) storagePanel.SetActive(false);

        if (!InventoryMainUI.IsOpen)
        {
            Cursor.visible   = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    // ─── Refresh ──────────────────────────────────────────────────────

    void Refresh()
    {
        RefreshStorageGrid();
        RefreshInventoryGrid();
    }

    // ── Storage Side ──────────────────────────────────────────────────

    void RefreshStorageGrid()
    {
        foreach (var g in _storageObjs) if (g) Destroy(g);
        _storageObjs.Clear();

        if (CurrentBox == null || storageGrid == null || storageSlotPrefab == null) return;

        for (int i = 0; i < CurrentBox.slots.Count; i++)
        {
            int captureIdx = i;
            var data = CurrentBox.slots[i];

            var obj = Instantiate(storageSlotPrefab, storageGrid);
            _storageObjs.Add(obj);

            var icon   = obj.transform.Find("Icon")?.GetComponent<Image>();
            var amount = obj.transform.Find("Amount")?.GetComponent<TextMeshProUGUI>();

            if (!data.IsEmpty)
            {
                if (icon)   { icon.sprite = data.item.icon; icon.enabled = true; }
                if (amount) amount.text = data.item.category == ItemCategory.Tools
                                            ? "∞" : data.amount.ToString();
            }
            else
            {
                if (icon)   icon.enabled = false;
                if (amount) amount.text  = "";
            }

            // คลิก → ดึงออกใส่ Inventory
            var btn = obj.GetComponent<Button>();
            btn?.onClick.AddListener(() =>
            {
                if (CurrentBox == null) return;
                var slot = CurrentBox.slots[captureIdx];
                if (slot.IsEmpty) return;

                if (InventoryMainUI.Instance != null)
                    InventoryMainUI.Instance.AddItemToInventory(slot.item, slot.amount);

                CurrentBox.ClearSlot(captureIdx);
                ShowFeedback($"นำ {slot.item.itemName} x{slot.amount} ออกจากกล่อง");
                Refresh();
            });
        }
    }

    // ── Inventory Side ────────────────────────────────────────────────

    void RefreshInventoryGrid()
    {
        foreach (var g in _inventoryObjs) if (g) Destroy(g);
        _inventoryObjs.Clear();

        if (inventoryGrid == null || inventorySlotPrefab == null) return;

        // Inventory slots
        if (InventoryMainUI.Instance != null)
        {
            foreach (var slot in InventoryMainUI.Instance.slots)
            {
                if (slot == null || slot.IsEmpty) continue;
                SpawnPlayerSlot(slot.item, slot.amount, fromHotbar: false);
            }
        }

        // Hotbar slots
        if (HotbarUI.Instance != null)
        {
            foreach (var slot in HotbarUI.Instance.slots)
            {
                if (slot == null || slot.item == null || slot.amount <= 0) continue;
                if (slot.item.category == ItemCategory.Tools) continue;
                SpawnPlayerSlot(slot.item, slot.amount, fromHotbar: true);
            }
        }
    }

    void SpawnPlayerSlot(ItemSO item, int amount, bool fromHotbar)
    {
        var obj = Instantiate(inventorySlotPrefab, inventoryGrid);
        _inventoryObjs.Add(obj);

        var icon   = obj.transform.Find("Icon")?.GetComponent<Image>();
        var lbl    = obj.transform.Find("Amount")?.GetComponent<TextMeshProUGUI>();

        if (icon && item.icon) { icon.sprite = item.icon; icon.enabled = true; }
        if (lbl) lbl.text = amount.ToString();

        var capItem     = item;
        var capAmount   = amount;
        var capHotbar   = fromHotbar;

        var btn = obj.GetComponent<Button>();
        btn?.onClick.AddListener(() =>
        {
            if (CurrentBox == null) return;

            if (!CurrentBox.HasSpace())
            {
                ShowFeedback("กล่องเต็มแล้ว!", Color.red);
                return;
            }

            bool ok = CurrentBox.AddItem(capItem, capAmount);
            if (ok)
            {
                if (capHotbar) RemoveFromHotbar(capItem, capAmount);
                else           RemoveFromInventory(capItem, capAmount);

                ShowFeedback($"ใส่ {capItem.itemName} x{capAmount} ในกล่อง");
                Refresh();
            }
            else
            {
                ShowFeedback("กล่องเต็ม ใส่ได้บางส่วน", Color.yellow);
                Refresh();
            }
        });
    }

    // ─── Remove Helpers ───────────────────────────────────────────────

    void RemoveFromInventory(ItemSO item, int amount)
    {
        if (InventoryMainUI.Instance == null) return;
        int rem = amount;
        foreach (var slot in InventoryMainUI.Instance.slots)
        {
            if (rem <= 0) break;
            if (slot == null || slot.item != item) continue;
            int take = Mathf.Min(rem, slot.amount);
            slot.DecreaseAmount(take);
            rem -= take;
        }
    }

    void RemoveFromHotbar(ItemSO item, int amount)
    {
        if (HotbarUI.Instance == null) return;
        int rem = amount;
        foreach (var slot in HotbarUI.Instance.slots)
        {
            if (rem <= 0) break;
            if (slot == null || slot.item != item) continue;
            int take = Mathf.Min(rem, slot.amount);
            int left = slot.amount - take;
            if (left <= 0) slot.Clear();
            else slot.SetStack(slot.item, left);
            rem -= take;
        }
    }

    // ─── Feedback ─────────────────────────────────────────────────────

    void ShowFeedback(string msg, Color? color = null)
    {
        if (!feedbackLabel) return;
        feedbackLabel.text  = msg;
        feedbackLabel.color = color ?? Color.white;
        _feedbackTimer      = feedbackDuration;
    }
}
