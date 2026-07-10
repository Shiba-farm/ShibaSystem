using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class SharedCartData : NetworkBehaviour
{
    [SerializeField] public InventoryData cartInventory;
    [SerializeField] private CartDataSignal sharedCartSignal;

    public override void OnNetworkSpawn()
    {
        sharedCartSignal.Register(this);
    }

    public override void OnNetworkDespawn()
    {
        sharedCartSignal.Unregister();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void AddItemServerRpc(int fromInventoryID, int slotIndex, RpcParams rpcParams = default)
    {
        ulong senderClientId = rpcParams.Receive.SenderClientId;

        // Server verifies: does this inventoryID actually belong to the sender?
        InventoryData source = InventoryDataRegistry.GetByOwnerAndID(senderClientId, fromInventoryID);
        Debug.Log($"Client {senderClientId} requests to add item from Inventory {fromInventoryID}, Slot {slotIndex} to cart.");
        if (source == null)
        {
            Debug.LogWarning($"Client {senderClientId} tried to access inventory {fromInventoryID} they don't own!");
            return; // rejected
        }

        NetworkItems item = source.InventoryItems[slotIndex];
        if (item.ItemID == 0) return;

        Debug.Log($"Server Cart: Adding item {item.ItemID} x{item.Amount} from Client {senderClientId}'s Inventory {fromInventoryID} to cart.");

        source.RemoveItem(item.ItemID, item.Amount);
        cartInventory.AddItem(item.ItemID, item.Amount);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RemoveItemServerRpc(int cartSlotIndex,
        RpcParams rpcParams = default)
    {
        ulong requesterClientId = rpcParams.Receive.SenderClientId;

        NetworkItems item = cartInventory.InventoryItems[cartSlotIndex];
        if (item.ItemID == 0) return;

        // Goes to whoever clicked remove — no tracking, no questions
        InventoryData requesterInventory =
            InventoryDataRegistry.GetByOwnerAndID(requesterClientId, 0);

        requesterInventory?.AddItem(item.ItemID, item.Amount);
        cartInventory.RemoveItem(item.ItemID, item.Amount);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RemoveAllItemServerRpc(
        RpcParams rpcParams = default)
    {
        ulong requesterClientId = rpcParams.Receive.SenderClientId;
        InventoryData requesterInventory =
            InventoryDataRegistry.GetByOwnerAndID(requesterClientId, 0);

        if (requesterInventory == null) return;

        // 1. Snapshot the cart — no network writes yet
        // var snapshot = new List<NetworkItems>(cartInventory.InventoryItems.Count);

        // 2. Batch-build what needs to go into player inventory
        var toTransfer = new Dictionary<int, int>(); // itemId → total amount
        foreach (var item in cartInventory.InventoryItems)
        {
            if (item.ItemID == 0) continue; // skip empty slots, don't early-return
            if (!toTransfer.ContainsKey(item.ItemID))
                toTransfer[item.ItemID] = 0;
            toTransfer[item.ItemID] += item.Amount;
        }

        foreach (var kvp in toTransfer)
        {
            requesterInventory.AddItem(kvp.Key, kvp.Value);
        }

        cartInventory.ClearInventory();
    }

    public int GetTotalValue()
    {
        Debug.Log("Calculating total cart value...");
        int total = 0;
        foreach (var item in cartInventory.InventoryItems)
        {
            if (item.ItemID == 0) continue;
            ItemSO itemSO = GameDataManager.Instance.itemDatabases.GetItemByID(item.ItemID);
            if (itemSO != null)
            {
                total += itemSO.sellPrice * item.Amount;
            }
        }
        Debug.Log($"Total cart value: {total}");
        return total;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void SellAllServerRpc()
    {
        foreach (var item in cartInventory.InventoryItems)
        {
            if (item.ItemID == 0) continue;

            ItemSO itemSO = GameDataManager.Instance.itemDatabases.GetItemByID(item.ItemID);
            if (itemSO == null) continue;

            int goldEarned = itemSO.sellPrice * item.Amount;

            GameDataManager.Instance.RecordSale(item.ItemID, item.Amount, goldEarned);
        }

        int totalGold = GetTotalValue();
        cartInventory.ClearInventory();
    }
}
