using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

[RequireComponent(typeof(CanvasGroup))]
public class InventoryItems : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI amountText;
    public ItemSO item;
    public int amount;

    [HideInInspector]

    public InventorySlotUIs sourceSlot;

    public RectTransform rectTransform;
    public CanvasGroup canvasGroup;
    public Transform originalParent;
    public int originalAmount;
    public Vector2 originalPosition;
    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        // amountText.text = amount.ToString();
    }
    public bool wasDroppedSuccessfully;

    public void InitializeItem(ItemSO newItem, int newAmount)
    {
        item = newItem;
        amount = newAmount;
        RefreshUI();
    }

    public void RefreshUI()
    {
        if (item == null || amount <= 0)
        {
            iconImage.enabled = false;
            amountText.text = "";
        }
        else
        {
            iconImage.sprite = item.icon;
            iconImage.enabled = true;
            amountText.text = amount.ToString();
        }
    }

    public void SetOriginalAmount(int value) => originalAmount = value;
    public int GetOriginalAmount() => originalAmount;
    public bool IsPartialStack() => amount < originalAmount;
}