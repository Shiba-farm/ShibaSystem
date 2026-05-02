using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HotbarSlot : MonoBehaviour
{
    [Header("UI refs")]
    public Image iconImage;              // �ͤ͹����ͧ Hotbar
    public TextMeshProUGUI amountText;   // ����Ţ (�������� �������ҧ��)

    [Header("Runtime")]
    public ItemSO item;
    public int amount;

    public bool HasStack => item != null && amount > 0;

    void Start()
    {
        // Refresh UI ตอนเริ่มเกมเพื่อให้ Tool แสดง ∞ ทันที
        UpdateUI();
    }

    // ---------- API Ẻ��� (��������ʤ�Ի��������¡��) ----------
    public void SetItem(ItemSO newItem) => SetShortcut(newItem);
    public void SetItem(ItemSO newItem, int newAmount) => SetStack(newItem, newAmount);
    // ---------------------------------------------------------------

    // ---------- API �й� ----------
    // �ҧ�� "���쵤ѵ" (����ͤ͹���ҧ���� ����ͨӹǹ)
    public void SetShortcut(ItemSO newItem)
    {
        item = newItem;
        amount = 0;
        UpdateUI();
    }

    // �ҧ�� "�ͧ��ԧ" (��ͨӹǹ)
    public void SetStack(ItemSO newItem, int newAmount)
    {
        item = newItem;
        amount = Mathf.Max(0, newAmount);
        UpdateUI();
    }
    // -------------------------------

    public void Clear()
    {
        item = null;
        amount = 0;
        UpdateUI();
    }

    public void UpdateUI()
    {
        // Icon
        if (iconImage)
        {
            if (item != null) { iconImage.sprite = item.icon; iconImage.enabled = true; }
            else { iconImage.sprite = null; iconImage.enabled = false; }
        }

        // Amount text — Tool แสดง ∞ แทนตัวเลข
        if (amountText)
        {
            if (item != null && item.category == ItemCategory.Tools)
                amountText.text = "∞";
            else if (amount > 0)
                amountText.text = amount.ToString();
            else
                amountText.text = "";
        }
    }
}
