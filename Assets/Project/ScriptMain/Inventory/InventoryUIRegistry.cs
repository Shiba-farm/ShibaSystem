using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class InventoryUIRegistry
{
    // รองรับหลาย panel ต่อ inventoryID — เช่น HUD hotbar + hotbar ใน inventory panel
    private static Dictionary<int, List<InventoryMainUIs>> _panels = new();

    public static void Register(int inventoryID, InventoryMainUIs panel)
    {
        if (!_panels.TryGetValue(inventoryID, out var list))
        {
            list = new List<InventoryMainUIs>();
            _panels[inventoryID] = list;
        }
        if (!list.Contains(panel))
            list.Add(panel);
    }

    public static void Unregister(int inventoryID, InventoryMainUIs panel)
    {
        if (!_panels.TryGetValue(inventoryID, out var list)) return;
        list.Remove(panel);
        if (list.Count == 0) _panels.Remove(inventoryID);
    }

    public static InventoryMainUIs GetPanel(int inventoryID)
    {
        if (!_panels.TryGetValue(inventoryID, out var list)) return null;
        return list.FirstOrDefault(p => p.activeData != null);
    }

    public static void RequestItemTransfer(SlotAddress from, SlotAddress to, int amount)
    {
        // ตรวจสอบจาก UI panel registry ซึ่งเป็น local-player-only
        // (InventoryDataRegistry เป็น global — ถูก overwrite เมื่อ player อื่น join)
        // ค้นหา panel ที่ inventoryID ตรงกัน และมี activeData อยู่
        var fromPanel = GetPanel(from.InventoryID);
        if (fromPanel?.activeData == null)
        {
            // HotbarInPanelUIs ใช้ slotDataID=1 แต่ register ที่ panelID=10
            // ดังนั้นถ้าหาที่ from.InventoryID ไม่เจอให้ fallback ไปดูว่ามี panel active อยู่ไหม
            bool anyActive = false;
            foreach (var list in _panels.Values)
            {
                foreach (var p in list)
                {
                    if (p != null && p.activeData != null) { anyActive = true; break; }
                }
                if (anyActive) break;
            }
            if (!anyActive)
            {
                Debug.LogWarning($"[InventoryUIRegistry] No active panel for ID {from.InventoryID} — drag cancelled.");
                return;
            }
        }

        InventoryNetworkManager.Instance.RequestCrossInventoryMoveServerRpc(
            from.InventoryID, from.SlotIndex,
            to.InventoryID,   to.SlotIndex,
            amount
        );
    }
}
