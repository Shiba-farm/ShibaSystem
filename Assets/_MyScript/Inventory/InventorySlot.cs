using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class InventorySlot : MonoBehaviour, IPointerClickHandler
{
    [Header("UI refs")]
    public Image iconImage;
    public TextMeshProUGUI amountText;

    [Header("Runtime")]
    public ItemSO item;
    public int amount;

    public bool IsEmpty => item == null || amount <= 0;

    void Start() => UpdateUI();

    // ─── Public API ───────────────────────────────────────────────────

    public void SetItem(ItemSO newItem) => SetItem(newItem, 1);

    public void SetItem(ItemSO newItem, int newAmount)
    {
        item   = newItem;
        amount = Mathf.Max(1, newAmount);
        UpdateUI();
    }

    public void Clear()
    {
        item   = null;
        amount = 0;
        UpdateUI();
    }

    public void IncreaseAmount(int value)
    {
        if (value <= 0 || item == null) return;
        amount += value;
        UpdateUI();
    }

    public void DecreaseAmount(int value)
    {
        if (value <= 0 || item == null) return;
        amount -= value;
        if (amount <= 0) Clear();
        else UpdateUI();
    }

    public void UpdateUI()
    {
        if (iconImage)
        {
            iconImage.sprite  = item?.icon;
            iconImage.enabled = item != null;
        }

        if (amountText)
        {
            if (item == null || amount <= 0) amountText.text = "";
            else if (item.category == ItemCategory.Tools) amountText.text = "∞";
            else amountText.text = amount.ToString();
        }
    }

    // ─── Click (ไว้สำหรับระบบเดิมถ้าจำเป็น) ──────────────────────────
    public void OnPointerClick(PointerEventData eventData) { }

    // ─── Wrappers ─────────────────────────────────────────────────────
    public void ClearSlot()        => Clear();
    public void UpdateAmountText() => UpdateUI();
}
