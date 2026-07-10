using UnityEngine;
using UnityEngine.EventSystems;

public class SellboxInventoryMainUIs : InventoryMainUIs
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
        if (activeData == null) return;
        int networkIndex = slotToInventoryIndex[slot.slotIndex];
        NetworkItems item = activeData.InventoryItems[networkIndex];
        if (item.ItemID == 0) return;

        sharedCart.RemoveItemServerRpc(networkIndex);
    }
}
