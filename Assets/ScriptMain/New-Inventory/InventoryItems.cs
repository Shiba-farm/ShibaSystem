using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

[RequireComponent(typeof(CanvasGroup))]
public class InventoryItems : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("UI References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI amountText;
    public ItemSO item;
    public int amount;
    
    public InventorySlotUIs sourceSlot;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Transform originalParent;
    private int originalAmount;
    private Vector2 originalPosition;
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

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        amountText.text = amount.ToString();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Calculate amount (Terraria Style: Right Click = 1)
        int amountToMove = (eventData.button == PointerEventData.InputButton.Right) ? amount / 2 : amount;
        if(amount == 1) amountToMove = 1; // If we only have 1, we can only move 1 regardless of click type
        originalAmount = amount;
        amount = amountToMove;
        wasDroppedSuccessfully = false;
        RefreshUI();

        // Visual feedback: If we split the stack, the source slot stays visible with less
        // If we move all, the source slot looks empty.

        // UI Layering: Move to a 'Drag Layer' so it's above all other UI
        Debug.Log($"Begin Dragging Item: {item.itemName} x{amountToMove} from Slot {sourceSlot.slotIndex}");
        sourceSlot.OnItemDraggedAway(amountToMove);
        originalParent = transform.parent;
        originalPosition = rectTransform.anchoredPosition;
        transform.SetParent(transform.root); // Move to topmost canvas

        canvasGroup.blocksRaycasts = false; // Essential for IDropHandler to work
        canvasGroup.alpha = 0.7f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Simple position follow
        rectTransform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;

        // If we weren't dropped on a valid IDropHandler slot, return home
        if (!wasDroppedSuccessfully)
        {
            ReturnToSource();
        }
    }

    public void ReturnToSource()
    {
        amount = originalAmount;
        sourceSlot.amount = originalAmount;
        sourceSlot.ClearSlotVisuals();
        transform.SetParent(originalParent);
        rectTransform.anchoredPosition = Vector2.zero;
        sourceSlot.LinkItemUI(this);
        RefreshUI();
        Debug.Log($"Returning item to original slot: {item?.itemName ?? "None"} x{amount} back to Slot {sourceSlot.slotIndex}");
    }

    public bool IsPartialStack()
    {
        return amount < originalAmount;
    }
}