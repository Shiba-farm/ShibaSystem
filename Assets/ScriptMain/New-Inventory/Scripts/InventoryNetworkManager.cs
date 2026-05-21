using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class InventoryNetworkManager : NetworkBehaviour, ISaveable
{
    public static InventoryNetworkManager Instance { get; private set; }
    public event Action<bool> OnInventoryConfirmedEmpty;

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

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestDeleteBatchServerRpc(int inventoryID, int[] slotIndices, RpcParams rpcParams = default)
    {
        ulong requesterClientId = rpcParams.Receive.SenderClientId;
        InventoryData inventory = InventoryDataRegistry.GetByOwnerAndID(requesterClientId, inventoryID);
        Debug.Log($"Is inventory null : {inventory == null}");
        if (inventory == null) return;

        foreach (int slotIndex in slotIndices)
        {
            if (slotIndex < 0 || slotIndex >= inventory.InventoryItems.Count) continue;
            inventory.InventoryItems[slotIndex] = new NetworkItems { ItemID = 0, Amount = 0 };
        }

        CheckInventoryEmptyRpc(inventoryID, requesterClientId);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void CheckInventoryEmptyServerRpc(int inventoryID, RpcParams rpcParams = default)
    {
        ulong requesterClientId = rpcParams.Receive.SenderClientId;
        InventoryData inventory = InventoryDataRegistry.GetByOwnerAndID(requesterClientId, inventoryID);
        // Debug.Log($"Inventory Null ? : {inventory == null}");
        if (inventory == null) return;

        CheckInventoryEmptyRpc(inventoryID, requesterClientId);
    }

    // Extracted so both paths can reuse it
    private void CheckInventoryEmptyRpc(int inventoryID, ulong clientId)
    {
        InventoryData inventory = InventoryDataRegistry.GetByOwnerAndID(clientId, inventoryID);
        if (inventory == null) return;

        bool isEmpty = true;
        foreach (var item in inventory.InventoryItems)
        {
            if (item.ItemID != 0 && item.Amount > 0)
            {
                isEmpty = false;
                break;
            }
        }
        Debug.Log($"Is inventory empty : {isEmpty}");
        NotifyInventoryEmptyClientRpc(isEmpty, RpcTarget.Single(clientId, RpcTargetUse.Temp));
    }

    [Rpc(SendTo.SpecifiedInParams)]
    private void NotifyInventoryEmptyClientRpc(bool isEmpty, RpcParams rpcParams = default)
    {
        Debug.Log($"Notify the client : {isEmpty}");
        OnInventoryConfirmedEmpty?.Invoke(isEmpty);
    }

    public void CaptureState(GameSaveData save, ulong clientId = 0)
    {
        var playerData = save.GetOrCreatePlayer(clientId);

        playerData.inventory = CaptureInventory(clientId, inventoryID: 0);
        playerData.hotbar = CaptureInventory(clientId, inventoryID: 1);
    }

    private List<ItemSlotSaveData> CaptureInventory(ulong clientId, int inventoryID)
    {
        var result = new List<ItemSlotSaveData>();
        var inventory = InventoryDataRegistry.GetByOwnerAndID(clientId, inventoryID);
        if (inventory == null) return result;

        for (int i = 0; i < inventory.InventoryItems.Count; i++)
        {
            var item = inventory.InventoryItems[i];
            result.Add(new ItemSlotSaveData
            {
                slotIndex = i,
                itemID = item.ItemID,
                amount = item.Amount
            });
        }

        return result;
    }

    public void RestoreState(GameSaveData save, ulong clientId = 0)
    {
        if (!IsServer) return;
        var playerData = save.FindPlayer(clientId);
        if (playerData == null) return;

        RestoreInventory(clientId, inventoryID: 0, playerData.inventory);
        RestoreInventory(clientId, inventoryID: 1, playerData.hotbar);
    }

    private void RestoreInventory(ulong clientId, int inventoryID, List<ItemSlotSaveData> slots)
    {
        var inventory = InventoryDataRegistry.GetByOwnerAndID(clientId, inventoryID);
        if (inventory == null) return;

        foreach (var saved in slots)
        {
            if (saved.slotIndex >= inventory.InventoryItems.Count) continue;
            inventory.InventoryItems[saved.slotIndex] = new NetworkItems
            {
                ItemID = saved.itemID,
                Amount = saved.amount
            };
        }
    }
}
