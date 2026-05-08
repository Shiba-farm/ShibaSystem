using Unity.Netcode;
using UnityEngine;

public class InventoryNetworkManager : NetworkBehaviour
{
    public static InventoryNetworkManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestCrossInventoryMoveServerRpc(
        int fromInventoryID, int fromSlotIndex,
        int toInventoryID, int toSlotIndex,
        int moveAmount,
        RpcParams rpcParams = default)
    {
        // Resolve both InventoryData references server-side
        InventoryData fromInventory = InventoryDataRegistry.GetInventory(fromInventoryID);
        InventoryData toInventory = InventoryDataRegistry.GetInventory(toInventoryID);

        if (fromInventory == null || toInventory == null) return;

        // Validate slot bounds
        if (fromSlotIndex < 0 || fromSlotIndex >= fromInventory.InventoryItems.Count) return;
        if (toSlotIndex < 0 || toSlotIndex >= toInventory.InventoryItems.Count) return;

        NetworkItems fromData = fromInventory.InventoryItems[fromSlotIndex];
        NetworkItems toData = toInventory.InventoryItems[toSlotIndex];

        moveAmount = Mathf.Clamp(moveAmount, 0, fromData.Amount);
        if (moveAmount <= 0 || fromData.ItemID == 0) return;

        if (toData.ItemID != 0 && toData.ItemID != fromData.ItemID)
        {
            if (moveAmount == fromData.Amount)
            {
                fromInventory.InventoryItems[fromSlotIndex] = toData;
                toInventory.InventoryItems[toSlotIndex] = fromData;
            }
            return;
        }

        ItemSO itemSO = GameDataManager.Instance.itemDatabases.GetItemByID(fromData.ItemID);
        if (itemSO != null && itemSO.isStackable)
        {
            int canAdd = itemSO.maxStack - toData.Amount;
            if (canAdd > 0)
            {
                int amountToMove = Mathf.Min(moveAmount, canAdd);
                toData.ItemID = fromData.ItemID;
                toData.Amount += amountToMove;
                toInventory.InventoryItems[toSlotIndex] = toData;

                fromData.Amount -= amountToMove;
                if (fromData.Amount <= 0) fromData = new NetworkItems { ItemID = 0, Amount = 0 };
                fromInventory.InventoryItems[fromSlotIndex] = fromData;
            }
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestAddItemServerRpc(int inventoryID, int itemID, int amount, RpcParams rpcParams = default)
    {
        ulong requesterClientId = rpcParams.Receive.SenderClientId;
        InventoryData inventory = InventoryDataRegistry.GetByOwnerAndID(requesterClientId, inventoryID);
        if (inventory == null) return;

        inventory.AddItem(itemID, amount);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestDeleteItemServerRpc(int inventoryID, int itemID, int amount, RpcParams rpcParams = default)
    {
        ulong requesterClientId = rpcParams.Receive.SenderClientId;
        InventoryData inventory = InventoryDataRegistry.GetByOwnerAndID(requesterClientId, inventoryID);
        if (inventory == null) return;

        inventory.RemoveItem(itemID, amount);
    }
}
