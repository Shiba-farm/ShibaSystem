using Unity.Netcode;
using UnityEngine;

public class InventoryData : NetworkBehaviour
{
    [SerializeField] private int inventorySize = 20;
    [SerializeField] private InventoryDataSignal connectionSignal;
    public NetworkList<NetworkItems> InventoryItems;

    void Awake()
    {
        InventoryItems = new NetworkList<NetworkItems>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            InitializeInventory();
        }
        if (IsOwner)
        {
            connectionSignal.UpdateInventoryData(this);
        }

        CraftingManager.Instance.OnRecipeCrafted -= OnRecieveCraftedItem;
        CraftingManager.Instance.OnRecipeCrafted += OnRecieveCraftedItem;
    }

    private void OnRecieveCraftedItem(CraftingRecipeSO recipe)
    {
        if (recipe.resultItem != null && recipe.resultAmount > 0)
        {
            RequestAddItemServerRpc(recipe.resultItem.itemID, recipe.resultAmount);
        }
    }

    private void InitializeInventory()
    {
        // 1. First, ensure the list has exactly 16 slots.
        // This turns a "count 0" list into a "count 16" list of empty items.
        if (InventoryItems.Count == 0)
        {
            for (int i = 0; i < inventorySize; i++)
            {
                InventoryItems.Add(new NetworkItems { ItemID = 0, Amount = 0 });
            }
        }

        InventoryItems[0] = new NetworkItems { ItemID = 4, Amount = 1 }; // e.g. Sword
        InventoryItems[1] = new NetworkItems { ItemID = 5, Amount = 4 }; // e.g. Potions
        InventoryItems[2] = new NetworkItems { ItemID = 17, Amount = 2 }; // e.g. Potions

        Debug.Log("Server: Inventory Initialized with Mock Data.");
    }

    public void AddItem(int itemId, int amount)
    {
        int count = 0;
        for (int i = 0; i < InventoryItems.Count; i++)
        {
            if (InventoryItems[i].ItemID != 0) count++;
            if (InventoryItems[i].ItemID == itemId)
            {
                var updatedItem = InventoryItems[i];
                updatedItem.Amount += amount;
                InventoryItems[i] = updatedItem;
                return;
            }
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
                Debug.Log($"Filled empty slot {i} with ItemID {itemId} x{amount}");
                return;
            }
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

    [ServerRpc]
    public void RequestAddItemServerRpc(int id, int amount)
    {
        // 1. Logic: Check if we already have this item to stack it
        for (int i = 0; i < InventoryItems.Count; i++)
        {
            if (InventoryItems[i].ItemID == id)
            {
                var updatedItem = InventoryItems[i];
                updatedItem.Amount += amount;
                InventoryItems[i] = updatedItem; // Syncs the change
                return;
            }
        }

        if (InventoryItems.Count >= inventorySize)
        {
            Debug.LogWarning("Inventory is full!");
            return;
        }

        // 2. Logic: If not found, add a new entry
        InventoryItems.Add(new NetworkItems
        {
            ItemID = id,
            Amount = amount,
        });
    }

    [ServerRpc]
    public void RequestPutItemServerRpc(int fromIndex, int toIndex, int moveAmount)
    {
        if (fromIndex < 0 || fromIndex >= InventoryItems.Count ||
        toIndex < 0 || toIndex >= InventoryItems.Count ||
        fromIndex == toIndex) return;

        NetworkItems fromData = InventoryItems[fromIndex];
        NetworkItems toData = InventoryItems[toIndex];

        moveAmount = Mathf.Clamp(moveAmount, 0, fromData.Amount);
        if (moveAmount <= 0 || fromData.ItemID == 0) return;

        if (toData.ItemID != 0 && toData.ItemID != fromData.ItemID)
        {
            if (moveAmount < fromData.Amount)
            {
                Debug.Log("Rejecting partial-stack swap to prevent item overlap.");
                return; // Server does nothing, Client will eventually roll back
            }
            if (moveAmount == fromData.Amount)
            {
                Debug.Log($"Swapping slot {fromIndex} with {toIndex}");
                InventoryItems[fromIndex] = toData;
                InventoryItems[toIndex] = fromData;
            }
            return;
        }

        ItemSO itemSO = GameDataManager.Instance.itemDatabases.GetItemByID(fromData.ItemID);

        if (itemSO != null && itemSO.isStackable)
        {
            int maxStack = Mathf.Max(1, itemSO.maxStack);
            int canAdd = maxStack - toData.Amount;
            Debug.Log($"Attempting to stack ItemID {fromData.ItemID} from Slot {fromIndex} to Slot {toIndex}. Can add {canAdd} more to the stack. {toData.Amount} currently in destination slot.");

            if (canAdd > 0)
            {
                int amountToMove = Mathf.Min(moveAmount, canAdd);
                Debug.Log($"Stacking {amountToMove} of ItemID {fromData.ItemID} from Slot {fromIndex} to Slot {toIndex}");

                // Update Destination
                toData.ItemID = fromData.ItemID;
                toData.Amount += amountToMove;
                InventoryItems[toIndex] = toData;

                // Update Source
                fromData.Amount -= amountToMove;
                if (fromData.Amount <= 0) fromData = new NetworkItems { ItemID = 0, Amount = 0 };
                InventoryItems[fromIndex] = fromData;

                Debug.Log($"Stacking {amountToMove} of ItemID {fromData.ItemID} from Slot {fromIndex} to Slot {toIndex}");
                Debug.Log($"Post-Stack: Slot {fromIndex} has ItemID {fromData.ItemID} x{fromData.Amount}, Slot {toIndex} has ItemID {toData.ItemID} x{toData.Amount}");

                return; // Logic finished for stacking
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

    [ServerRpc]
    public void RequestDropItemServerRpc(int index)
    {
        if (index < InventoryItems.Count)
        {
            // Logic: Spawn the item in the 3D world here before removing
            InventoryItems.RemoveAt(index);
        }
    }
    [ServerRpc]
    public void RequestDeleteItemServerRpc(int index)
    {
        if (index >= 0 && index < InventoryItems.Count)
        {
            InventoryItems[index] = new NetworkItems { ItemID = 0, Amount = 0 };
        }
    }
}
