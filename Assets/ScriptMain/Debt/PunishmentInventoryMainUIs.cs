using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class PunishmentInventoryMainUIs : InventoryMainUIs
{
    [SerializeField] private Color selectedColor = new Color(1f, 0.3f, 0.3f, 1f);
    private HashSet<InventorySlotUIs> selectedSlots = new();

    // Call this when inventory UI is built/refreshed
    public void Init()
    {
        // Just clear selection state, no dictionary needed
        foreach (var slot in selectedSlots)
        {
            if (slot != null)
                slot.SetBackgroundColor(slot.GetOriginalColor());
        }
        selectedSlots.Clear();
    }
    protected override void OnSlotClicked(InventorySlotUIs slot, PointerEventData eventData)
    {
        Debug.Log($"Click punishment on slot : {slot.inventoryIndex}");
        int currentPrice = slot.currentItem.sellPrice * slot.amount;

        if (selectedSlots.Contains(slot))
        {
            selectedSlots.Remove(slot);
            DebtManager.Instance.DeductTradeValue(currentPrice);
            slot.SetBackgroundColor(slot.GetOriginalColor());
        }
        else
        {
            selectedSlots.Add(slot);
            DebtManager.Instance.AddTradeValue(currentPrice);
            slot.SetBackgroundColor(selectedColor);
        }
    }

    public List<(ItemSO, int)> GetAndClearSelectedItems()
    {
        if (selectedSlots.Count == 0) return null;

        var result = new List<(ItemSO, int)>();
        var selectedIndices = new List<int>();

        foreach (var slot in selectedSlots)
        {
            if (slot == null || slot.currentItem == null) continue;

            Debug.Log($"Add deleted item to array : {slot.inventoryIndex} , {slot.currentItem.itemName}");
            slot.SetBackgroundColor(slot.GetOriginalColor());

            selectedIndices.Add(slot.inventoryIndex);
            result.Add((slot.currentItem, slot.amount));
        }

        if (selectedIndices.Count > 0)
        {
            Debug.Log($"Delete the item : {selectedIndices.Count}");
            InventoryNetworkManager.Instance.RequestDeleteBatchServerRpc(
                inventoryID,
                selectedIndices.ToArray()
            );
        }

        // Only deselect, don't clear — slots still exist
        selectedSlots.Clear();

        return result.Count > 0 ? result : null;
    }

    public bool IsInventoryEmpty()
    {
        if (allSlots == null || allSlots.Length == 0) return true;

        foreach (var slot in allSlots)
        {
            if (slot != null && slot.currentItem != null && slot.amount > 0)
                return false;
        }
        return true;
    }
}
