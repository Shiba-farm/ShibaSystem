using System;
using Unity.Netcode;
using UnityEngine;

public class InventoryData : NetworkSaveableBehaviour
{
    [SerializeField] private int inventorySize = 20;
    [SerializeField] private InventoryDataSignal connectionSignal;
    [SerializeField] private int inventoryID;
    [SerializeField] private bool needMockData = true;
    public NetworkList<NetworkItems> InventoryItems;
    public int InventoryID => inventoryID;
    public override bool IsPlayerSaveable => true;

    void Awake()
    {
        InventoryItems = new NetworkList<NetworkItems>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        InventoryDataRegistry.Register(inventoryID, this);
        InventoryDataRegistry.RegisterOwner(OwnerClientId, this);
        if (IsServer)
        {
            InitializeInventory();
            SaveLoadManager.Instance?.Register(this);
        }
        if (IsOwner)
        {
            connectionSignal.UpdateInventoryData(this);
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        InventoryDataRegistry.Unregister(inventoryID);
        InventoryDataRegistry.UnregisterOwner(OwnerClientId, this);
        if (IsServer)
            SaveLoadManager.Instance?.Unregister(this);
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

    public override void CaptureState(GameSaveData save, ulong clientId = 0)
    {
        // Use OUR OWN OwnerClientId — ignore the passed clientId
        var playerData = save.GetOrCreatePlayer(OwnerClientId);

        var slots = new System.Collections.Generic.List<ItemSlotSaveData>();
        for (int i = 0; i < InventoryItems.Count; i++)
        {
            var item = InventoryItems[i];
            slots.Add(new ItemSlotSaveData
            {
                slotIndex = i,
                itemID = item.ItemID,
                amount = item.Amount
            });
        }

        // inventoryID tells us whether this is main inventory or hotbar
        if (inventoryID == 0) playerData.inventory = slots;
        if (inventoryID == 1) playerData.hotbar = slots;

        Debug.Log($"[InventoryData] Captured inv {inventoryID} for client {OwnerClientId}");
    }

    public override void RestoreState(GameSaveData save, ulong clientId = 0)
    {
        if (!IsServer) return;

        // Use OUR OWN OwnerClientId
        var playerData = save.FindPlayer(OwnerClientId);
        if (playerData == null) return;

        var slots = inventoryID == 0 ? playerData.inventory
                  : inventoryID == 1 ? playerData.hotbar
                  : null;

        if (slots == null) return;

        foreach (var saved in slots)
        {
            if (saved.slotIndex >= InventoryItems.Count) continue;
            InventoryItems[saved.slotIndex] = new NetworkItems
            {
                ItemID = saved.itemID,
                Amount = saved.amount
            };
        }

        Debug.Log($"[InventoryData] Restored inv {inventoryID} for client {OwnerClientId}");
    }
}
