using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System;

public enum SlotInteractionMode { DragDrop, ClickOnly, None }

public class InventorySlotUIs : MonoBehaviour, IDropHandler
{
    public Action<InventorySlotUIs, PointerEventData> OnClickedCallback;
    [Header("UI details")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Sprite backgroundSprite;
    [SerializeField] private GameObject backgroundImageForAmountText;
    [SerializeField] private Sprite backgroundSpriteForAmountText;

    [Header("UI References")]
    [SerializeField] private InventoryItems itemIconPrefab;

    [Header("Identification")]
    public int slotIndex;
    public int inventoryID;
    public int inventoryIndex;
    [Header("Runtime Data")]
    public ItemSO currentItem;
    public int amount;
    [Header("Interaction")]
    public SlotInteractionMode interactionMode = SlotInteractionMode.DragDrop;
    private InventoryItems currentItemUI;

    private void Awake()
    {
        currentItemUI = GetComponentInChildren<InventoryItems>();
        if (currentItemUI != null)
        {
            currentItemUI.sourceSlot = this;
            amount = currentItemUI.amount;
        }
    }

    private void Start()
    {
        backgroundImage.sprite = backgroundSprite;
        backgroundImageForAmountText.GetComponent<Image>().sprite = backgroundSpriteForAmountText;
    }

    public void RefreshSlot(ItemSO newItem, int newAmount)
    {
        currentItem = newItem;
        amount = newAmount;

        if (newItem == null || newAmount <= 0)
        {
            ClearSlotVisuals();
            return;
        }

        UpdateUI();
    }

    public void ClearSlotVisuals()
    {
        if (currentItemUI != null)
        {
            InventoryItems dragScript = currentItemUI.GetComponent<InventoryItems>();
            if (dragScript != null && !dragScript.wasDroppedSuccessfully)
            {
                Destroy(currentItemUI.gameObject);
                currentItemUI = null;
            }
            currentItemUI = null;
        }
    }

    public void UpdateUI()
    {
        if (currentItem == null) return;

        if (currentItemUI == null)
        {
            currentItemUI = Instantiate(itemIconPrefab, transform);
            currentItemUI.sourceSlot = this;
            currentItemUI.InitializeItem(currentItem, amount);

            SetupInteraction(currentItemUI);
        }

        currentItemUI.item = currentItem;
        currentItemUI.amount = amount;
        currentItemUI.RefreshUI();
    }

    private void SetupInteraction(InventoryItems itemUI)
    {
        switch (interactionMode)
        {
            case SlotInteractionMode.DragDrop:
                itemUI.gameObject.AddComponent<DraggableItem>();
                break;

            case SlotInteractionMode.ClickOnly:
                var clickable = itemUI.gameObject.AddComponent<ClickableItem>();
                break;

            case SlotInteractionMode.None:
                break;
        }
    }

    public void OnItemClicked(InventoryItems itemUI, PointerEventData eventData)
    {
        OnClickedCallback?.Invoke(this, eventData);
    }

    public void OnItemDraggedAway(int amountTaken)
    {
        amount -= amountTaken;

        if (amount <= 0)
        {
            currentItemUI = null;
            currentItem = null;
            amount = 0;
        }
        else
        {
            currentItemUI = null;
            UpdateUI();
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        GameObject droppedObject = eventData.pointerDrag;
        if (droppedObject == null) return;

        InventoryItems draggedItem = droppedObject.GetComponent<InventoryItems>();

        if (draggedItem != null)
        {
            int fromSlot = draggedItem.sourceSlot.slotIndex;
            int toSlot = slotIndex;
            if (fromSlot == toSlot && draggedItem.sourceSlot.inventoryID == inventoryID)
            {
                draggedItem.wasDroppedSuccessfully = false;
                return;
            }
            bool isPartialStack = draggedItem.IsPartialStack();
            bool isDifferentItem = currentItem != null && currentItem.itemID != draggedItem.item.itemID;

            if (isPartialStack && isDifferentItem)
            {
                draggedItem.wasDroppedSuccessfully = false;
                return;
            }

            var from = new SlotAddress(draggedItem.sourceSlot.inventoryID, draggedItem.sourceSlot.slotIndex);
            var to = new SlotAddress(inventoryID, slotIndex);

            InventoryUIRegistry.RequestItemTransfer(from, to, draggedItem.amount);

            Destroy(draggedItem.gameObject);
        }
    }

    public void LinkItemUI(InventoryItems itemUI)
    {
        currentItemUI = itemUI;
    }
}

public struct SlotAddress
{
    public int InventoryID;
    public int SlotIndex;

    public SlotAddress(int inventoryID, int slotIndex)
    {
        InventoryID = inventoryID;
        SlotIndex = slotIndex;
    }
}