using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class InventoryNetworkManager : NetworkBehaviour
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
        // ใช้ owner-based lookup เพื่อป้องกัน inventory ของ Player คนอื่นถูกแก้
        // (GetInventory แบบ global จะถูก overwrite เมื่อ Player 2 join และ register ID ซ้ำ)
        ulong requesterClientId = rpcParams.Receive.SenderClientId;
        InventoryData fromInventory = InventoryDataRegistry.GetByOwnerAndID(requesterClientId, fromInventoryID);
        InventoryData toInventory   = InventoryDataRegistry.GetByOwnerAndID(requesterClientId, toInventoryID);

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
            // Stackable: เพิ่มเข้าสแตคหรือเติมช่อง
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
        else if (toData.ItemID == 0)
        {
            // Non-stackable item → ช่องว่าง: เลื่อนทั้งชิ้น
            // (กรณี non-stackable → non-empty ต่างประเภทถูก handle ก่อนหน้าแล้ว)
            toInventory.InventoryItems[toSlotIndex]   = fromData;
            fromInventory.InventoryItems[fromSlotIndex] = new NetworkItems { ItemID = 0, Amount = 0 };
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

    /// <summary>
    /// ลบไอเทมโดยอ้างอิงจาก slot index โดยตรง (ถูกต้องกว่า RequestDeleteItemServerRpc
    /// ที่ใช้ itemID) — ใช้โดย TrashDropZone เพื่อการลบที่แม่นยำ
    /// </summary>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestDeleteBySlotServerRpc(int inventoryID, int slotIndex, int amount, RpcParams rpcParams = default)
    {
        ulong requesterClientId = rpcParams.Receive.SenderClientId;
        InventoryData inventory = InventoryDataRegistry.GetByOwnerAndID(requesterClientId, inventoryID);
        if (inventory == null) return;
        if (slotIndex < 0 || slotIndex >= inventory.InventoryItems.Count) return;

        NetworkItems item = inventory.InventoryItems[slotIndex];
        if (item.ItemID == 0) return;

        int newAmount = item.Amount - amount;
        if (newAmount <= 0)
            inventory.InventoryItems[slotIndex] = new NetworkItems { ItemID = 0, Amount = 0 };
        else
        {
            item.Amount = newAmount;
            inventory.InventoryItems[slotIndex] = item;
        }
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
}
