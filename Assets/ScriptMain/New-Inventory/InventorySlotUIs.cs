using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class InventorySlotUIs : MonoBehaviour, IDropHandler
{
    [Header("UI References")]
    [SerializeField] private InventoryItems itemIconPrefab;

    [Header("Identification")]
    public int slotIndex;
    public int inventoryID;

    [Header("Runtime Data")]
    public ItemSO currentItem;
    public int amount;
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
        }

        currentItemUI.item = currentItem;
        currentItemUI.amount = amount;
        currentItemUI.RefreshUI();
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

        if (draggedItem != null && InventoryMainUIs.Instance.activeData != null)
        {
            int fromSlot = draggedItem.sourceSlot.slotIndex;
            int toSlot = slotIndex;
            if (fromSlot == toSlot)
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
            
            InventoryMainUIs.Instance.activeData.RequestPutItemServerRpc(fromSlot, toSlot, draggedItem.amount);

            Destroy(draggedItem.gameObject);
        }
    }

    public void LinkItemUI(InventoryItems itemUI)
    {
        currentItemUI = itemUI;
    }
}