using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerShopInventoryMainUIs : InventoryMainUIs
{
    [SerializeField] private CartDataSignal sharedCartSignal;
    private SharedCartData sharedCart;

    protected override void OnEnable()
    {
        base.OnEnable();

        if (sharedCartSignal.Current != null)
            sharedCart = sharedCartSignal.Current;
        else
            sharedCartSignal.OnCartReady += cart => sharedCart = cart;
    }

    protected override void OnSlotClicked(InventorySlotUIs slot, PointerEventData eventData)
    {
        if (activeData == null)
        {
            Debug.Log($"Active data is empty : {activeData == null}");
            return;
        }
        NetworkItems item = activeData.InventoryItems[slot.inventoryIndex];
        if (item.ItemID == 0)
        {
            Debug.Log($"Slot index : {slot.slotIndex} is empty : {item}");
            return;
        }

        Debug.Log($"Player clicked item {item.ItemID} in slot {slot.slotIndex} to sell.");

        // Move from player inventory → sellbox
        sharedCart.AddItemServerRpc(activeData.InventoryID, slot.inventoryIndex);
    }
}
