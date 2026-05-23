using System;
using Unity.Netcode;
using UnityEngine;

public class InventoryData : NetworkBehaviour
{
    [SerializeField] private int inventorySize = 20;
    [SerializeField] private InventoryDataSignal connectionSignal;
    [SerializeField] private int inventoryID;
    [SerializeField] private bool needMockData = true;
    public NetworkList<NetworkItems> InventoryItems;
    public int InventoryID => inventoryID;

    void Awake()
    {
        InventoryItems = new NetworkList<NetworkItems>();
    }

    public override void OnNetworkSpawn()
    {
        InventoryDataRegistry.Register(inventoryID, this);
        InventoryDataRegistry.RegisterOwner(OwnerClientId, this);
        if (IsServer)
        {
            InitializeInventory();
        }
        if (IsOwner)
        {
            connectionSignal.UpdateInventoryData(this);
        }
    }

    public override void OnNetworkDespawn()
    {
        InventoryDataRegistry.Unregister(inventoryID);
        InventoryDataRegistry.UnregisterOwner(OwnerClientId, this);
    }

    private void InitializeInventory()
    {
        if (InventoryItems.Count == 0)
        {
            for (int i = 0; i < inventorySize; i++)
            {
                InventoryItems.Add(new NetworkItems { ItemID = 0, Amount = 0 });
            }
        }

        if (needMockData)
        {
            InventoryItems[0] = new NetworkItems { ItemID = 20, Amount = 2 }; // e.g. Sword
            InventoryItems[1] = new NetworkItems { ItemID = 5, Amount = 4 }; // e.g. Potions
            InventoryItems[2] = new NetworkItems { ItemID = 17, Amount = 2 }; // e.g. Potions
            InventoryItems[3] = new NetworkItems { ItemID = 18, Amount = 1 }; // e.g. Potions
            InventoryItems[4] = new NetworkItems { ItemID = 19, Amount = 1 }; // e.g. Potions

            Debug.Log("Server: Inventory Initialized with Mock Data.");
        }
    }

    public void AddItem(int itemId, int amount)
    {
        ItemSO itemSO = GameDataManager.Instance.itemDatabases.GetItemByID(itemId);
        int maxStack = (itemSO != null && itemSO.isStackable) ? itemSO.maxStack : 1;

        for (int i = 0; i < InventoryItems.Count; i++)
        {
            if (InventoryItems[i].ItemID != itemId) continue;

            int canAdd = maxStack - InventoryItems[i].Amount;
            if (canAdd <= 0) continue;

            int toAdd = Mathf.Min(amount, canAdd);
            var updated = InventoryItems[i];
            updated.Amount += toAdd;
            InventoryItems[i] = updated;

            amount -= toAdd;
            if (amount <= 0) return;
        }

        // 2. Logic: If not found, add a new entry
        for (int i = 0; i < InventoryItems.Count; i++)
        {
            if (InventoryItems[i].ItemID == 0)
            {
                // Replace the empty slot with the new item
                InventoryItems[i] = new NetworkItems
                {
                    ItemID = itemId,
                    Amount = amount
                };
                Debug.Log($"Filled empty slot {i} with ItemID {itemId} x{amount} with inventory count : {InventoryItems.Count}");
                return;
            }
        }

        if (amount > 0)
        {
            Debug.Log($"Inventory full, dropping {amount}x ItemID {itemId} with inventory size of : {InventoryItems.Count}");
            // WorldItemManager.Instance.SpawnWorldItem(itemID, amount, dropPosition);
        }

        Debug.LogWarning("Inventory is full! No empty slots or existing stacks available.");
    }

    public void RemoveItem(int itemId, int amount)
    {
        for (int i = 0; i < InventoryItems.Count; i++)
        {
            if (InventoryItems[i].ItemID == itemId)
            {
                var updatedItem = InventoryItems[i];
                updatedItem.Amount -= amount;
                if (updatedItem.Amount <= 0)
                {
                    updatedItem = new NetworkItems { ItemID = 0, Amount = 0 };
                }
                InventoryItems[i] = updatedItem; // Syncs the change
                return;
            }
        }
    }
    public int GetItemCount(int itemId)
    {
        int count = 0;
        foreach (var item in InventoryItems)
        {
            if (item.ItemID == itemId)
            {
                count += item.Amount;
            }
        }
        return count;
    }

    public void ClearInventory()
    {
        for (int i = 0; i < InventoryItems.Count; i++)
        {
            InventoryItems[i] = new NetworkItems { ItemID = 0, Amount = 0 };
        }
    }
}
