using System.Collections.Generic;

/// <summary>Registry แบบเดียวกับ InventoryDataRegistry — ใช้ค้นหา EquipmentData ของ client ฝั่ง server</summary>
public static class EquipmentDataRegistry
{
    private static readonly Dictionary<ulong, EquipmentData> _byOwner = new();

    public static void RegisterOwner(ulong clientId, EquipmentData data) => _byOwner[clientId] = data;
    public static void UnregisterOwner(ulong clientId) => _byOwner.Remove(clientId);
    public static EquipmentData GetByOwner(ulong clientId) =>
        _byOwner.TryGetValue(clientId, out var d) ? d : null;
}
