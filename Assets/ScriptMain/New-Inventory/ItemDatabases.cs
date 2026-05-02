using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemDatabases", menuName = "Items/ItemDatabases")]
public class ItemDatabases : ScriptableObject
{
    [SerializeField] private List<ItemSO> allItems = new List<ItemSO>();
    private Dictionary<int, ItemSO> itemLookup = new Dictionary<int, ItemSO>();
    private bool isInitialized = false;

    public void Initialize()
    {
        itemLookup.Clear();
        foreach (var item in allItems)
        {
            if (item != null)
            {
                if (!itemLookup.ContainsKey(item.itemID))
                {
                    Debug.Log($"Registering Item: {item.itemName} with ID: {item.itemID}");
                    itemLookup.Add(item.itemID, item);
                }
            }
        }
        isInitialized = true;
    }

    public ItemSO GetItemByID(int id)
    {
        if (!isInitialized || itemLookup.Count == 0) 
            Initialize();

        if (itemLookup.TryGetValue(id, out var item))
        {
            return item;
        }
        
        Debug.LogWarning($"Item ID {id} not found in database!");
        return null;
    }
}
