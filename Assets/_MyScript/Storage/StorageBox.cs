using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// กล่องเก็บของในโลก — ผู้เล่นต้องคราฟก่อนแล้ว Place ลงใน Scene
///
/// Setup:
///   1. สร้าง Prefab "StorageBox"
///      ├── MeshRenderer (model กล่อง)
///      ├── BoxCollider (Is Trigger = true)  ← zone ตรวจจับ player
///      └── StorageBox (script นี้)
///   2. ติด PromptPanel (WorldSpace Canvas หรือ ScreenSpace ก็ได้)
///   3. StorageBoxPlacer จะ Instantiate Prefab นี้ตอน Place
/// </summary>
public class StorageBox : MonoBehaviour
{
    [Header("Interaction")]
    public KeyCode interactKey = KeyCode.E;

    [Header("Storage")]
    [Min(4)] public int slotCount = 20;

    [Header("Prompt UI")]
    public GameObject promptPanel;
    public TextMeshProUGUI promptLabel;

    // ─── Slot Data ────────────────────────────────────────────────────
    [System.Serializable]
    public class Slot
    {
        public ItemSO item;
        public int    amount;
        public bool   IsEmpty => item == null || amount <= 0;
    }

    public List<Slot> slots = new List<Slot>();

    // ─── Runtime ──────────────────────────────────────────────────────
    bool _playerInRange;

    // ──────────────────────────────────────────────────────────────────
    void Start()
    {
        // เติม slot ที่ยังขาด
        while (slots.Count < slotCount)
            slots.Add(new Slot());

        if (promptPanel) promptPanel.SetActive(false);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        _playerInRange = true;
        ShowPrompt($"กด [{interactKey}] เพื่อเปิดกล่องเก็บของ");
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        _playerInRange = false;
        HidePrompt();

        // ปิด UI ถ้า player เดินออก
        if (StorageUI.Instance != null && StorageUI.Instance.CurrentBox == this)
            StorageUI.Instance.Close();
    }

    void Update()
    {
        if (!_playerInRange) return;
        if (!Input.GetKeyDown(interactKey)) return;

        if (StorageUI.Instance != null && StorageUI.Instance.IsOpen
            && StorageUI.Instance.CurrentBox == this)
            StorageUI.Instance.Close();
        else
            StorageUI.Instance?.Open(this);
    }

    // ─── API ──────────────────────────────────────────────────────────

    /// <summary>เพิ่มไอเท็มเข้ากล่อง คืน true ถ้าใส่ได้หมด</summary>
    public bool AddItem(ItemSO item, int amount)
    {
        if (item == null) return false;

        // Stack ซ้ำก่อน
        if (item.isStackable)
        {
            foreach (var s in slots)
            {
                if (s.item != item) continue;
                int space = item.maxStack - s.amount;
                if (space <= 0) continue;
                int add = Mathf.Min(space, amount);
                s.amount += add;
                amount   -= add;
                if (amount <= 0) return true;
            }
        }

        // Slot ว่าง
        foreach (var s in slots)
        {
            if (!s.IsEmpty) continue;
            s.item   = item;
            s.amount = Mathf.Min(amount, item.maxStack);
            amount  -= s.amount;
            if (amount <= 0) return true;
        }

        return amount <= 0;
    }

    /// <summary>เคลียร์ slot ที่ index</summary>
    public void ClearSlot(int index)
    {
        if (index < 0 || index >= slots.Count) return;
        slots[index].item   = null;
        slots[index].amount = 0;
    }

    /// <summary>กล่องยังมีที่ว่างไหม</summary>
    public bool HasSpace() => slots.Exists(s => s.IsEmpty);

    // ─── Prompt Helpers ───────────────────────────────────────────────

    void ShowPrompt(string msg)
    {
        if (promptLabel) promptLabel.text = msg;
        if (promptPanel) promptPanel.SetActive(true);
    }

    void HidePrompt()
    {
        if (promptPanel) promptPanel.SetActive(false);
    }
}
