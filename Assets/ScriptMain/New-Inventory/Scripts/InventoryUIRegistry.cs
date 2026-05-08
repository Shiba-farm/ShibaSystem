using System.Collections.Generic;
using UnityEngine;

public static class InventoryUIRegistry
{
    private static Dictionary<int, InventoryMainUIs> _panels = new();

    public static void Register(int inventoryID, InventoryMainUIs panel)
        => _panels[inventoryID] = panel;

    public static void Unregister(int inventoryID)
        => _panels.Remove(inventoryID);

    public static InventoryMainUIs GetPanel(int inventoryID)
        => _panels.TryGetValue(inventoryID, out var p) ? p : null;

    public static void RequestItemTransfer(SlotAddress from, SlotAddress to, int amount)
    {
        var fromPanel = GetPanel(from.InventoryID);
        if (fromPanel?.activeData == null)
        {
            Debug.LogWarning("No active source panel found — drag cancelled.");
            return;
        }

        InventoryNetworkManager.Instance.RequestCrossInventoryMoveServerRpc(
            from.InventoryID, from.SlotIndex,
            to.InventoryID, to.SlotIndex,
            amount
        );
    }
}
