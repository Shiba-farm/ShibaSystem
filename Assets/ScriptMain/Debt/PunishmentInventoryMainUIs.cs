using UnityEngine;
using UnityEngine.EventSystems;

public class PunishmentInventoryMainUIs : InventoryMainUIs
{
    private bool isSelected = false;
    protected override void OnSlotClicked(InventorySlotUIs slot, PointerEventData eventData)
    {
        Debug.Log($"Click punishment on slot : {slot.inventoryIndex}");

        if(!isSelected)
        {
            DebtManager.Instance.AddTradeValue(100);
        }
        else
        {
            DebtManager.Instance.DeductTradeValue(100);
        }
        isSelected = !isSelected;
    }
}
