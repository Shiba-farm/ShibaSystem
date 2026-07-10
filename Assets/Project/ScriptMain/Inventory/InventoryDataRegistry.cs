using System.Collections.Generic;
using UnityEngine;

public static class InventoryDataRegistry
{
    private static Dictionary<int, InventoryData> _inventories = new();
    private static Dictionary<ulong, List<InventoryData>> _ownerInventories = new();

    public static void Register(int id, InventoryData data) => _inventories[id] = data;

    public static void RegisterOwner(ulong clientId, InventoryData data)
    {
        if (!_ownerInventories.ContainsKey(clientId))
            _ownerInventories[clientId] = new List<InventoryData>();

        _ownerInventories[clientId].Add(data);
    }
    public static void Unregister(int id) => _inventories.Remove(id);
    public static void UnregisterOwner(ulong clientId, InventoryData data)
    {
        if (!_ownerInventories.ContainsKey(clientId)) return;
        _ownerInventories[clientId].Remove(data);

        if (_ownerInventories[clientId].Count == 0)
            _ownerInventories.Remove(clientId);
    }
    public static InventoryData GetInventory(int id)
        => _inventories.TryGetValue(id, out var d) ? d : null;
    public static List<InventoryData> GetAllByOwner(ulong clientId)
    => _ownerInventories.TryGetValue(clientId, out var list) ? list : null;

    public static InventoryData GetByOwnerAndID(ulong clientId, int inventoryID)
    {
        var list = GetAllByOwner(clientId);
        if (list == null) return null;
        return list.Find(d => d.InventoryID == inventoryID);
    }
}
