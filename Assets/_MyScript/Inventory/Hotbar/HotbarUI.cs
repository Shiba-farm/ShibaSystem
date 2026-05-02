using UnityEngine;
using UnityEngine.UI;

public class HotbarUI : MonoBehaviour
{
    public HotbarSlot[] slots;
    public int selectedIndex = 0;

    // [เพิ่มใหม่] ตัวแปรสำหรับล็อกอินพุต (ห้ามเปลี่ยนของ)
    public bool IsInputLocked { get; set; }

    public static HotbarUI Instance { get; private set; }

    void Awake() => Instance = this;

    void Update()
    {
        HandleInput();
        HighlightSelectedSlot();
    }

    void HandleInput()
    {
        // [เพิ่มใหม่] ถ้าโดนล็อกอยู่ ให้หยุดการทำงานทันที (เปลี่ยนของไม่ได้)
        if (IsInputLocked) return;

        if (slots == null || slots.Length == 0) return;

        // 1..9
        for (int i = 0; i < Mathf.Min(9, slots.Length); i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                selectedIndex = i;
        }

        // 0 = ช่องที่ 10 (index 9) ถ้ามี
        if (slots.Length >= 10 && Input.GetKeyDown(KeyCode.Alpha0))
            selectedIndex = 9;

        // สลับด้วยสกอร์เมาส์
        float scroll = Input.mouseScrollDelta.y;
        if (scroll != 0f)
        {
            selectedIndex = (selectedIndex - Mathf.RoundToInt(scroll) + slots.Length) % slots.Length;
        }

        // กัน out of range
        selectedIndex = Mathf.Clamp(selectedIndex, 0, slots.Length - 1);
    }

    void HighlightSelectedSlot()
    {
        if (slots == null) return;

        for (int i = 0; i < slots.Length; i++)
        {
            var s = slots[i];
            if (!s) continue;

            var img = s.GetComponent<Image>();
            if (!img) continue;

            img.color = (i == selectedIndex) ? Color.red : Color.white;
        }
    }

    public ItemSO GetSelectedItem() => slots != null && slots.Length > 0 ? slots[selectedIndex].item : null;
    public HotbarSlot GetSelectedSlot() => slots != null && slots.Length > 0 ? slots[selectedIndex] : null;

    public bool AddItemToFirstEmptySlot(ItemSO item) => AddItemToFirstEmptySlot(item, 1);

    public bool AddItemToFirstEmptySlot(ItemSO item, int amount)
    {
        if (slots == null || item == null || amount <= 0) return false;

        if (item.isStackable)
        {
            foreach (var s in slots)
            {
                if (s && s.item == item && s.amount > 0)
                {
                    int max = Mathf.Max(1, item.maxStack);
                    int canAdd = Mathf.Min(amount, max - s.amount);
                    if (canAdd > 0)
                    {
                        s.amount += canAdd;
                        s.UpdateUI();
                        amount -= canAdd;
                        if (amount <= 0) return true;
                    }
                }
            }
        }

        foreach (var s in slots)
        {
            if (s && s.item == null)
            {
                s.SetItem(item, amount);
                return true;
            }
        }
        return false;
    }
}