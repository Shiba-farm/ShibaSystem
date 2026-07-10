using Unity.Netcode;
using UnityEngine;

/// <summary>
/// รับ request equip/unequip จาก client แล้ว validate + เรียก EquipmentData บน
/// server เท่านั้น — รูปแบบ RPC เดียวกับ InventoryNetworkManager ทุกจุด
/// (resolve ข้อมูลทั้งหมดที่ server ก่อนแก้ NetworkList ไม่เชื่อ client เด็ดขาด)
/// </summary>
public class EquipmentNetworkManager : NetworkBehaviour
{
    public static EquipmentNetworkManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestEquipServerRpc(int sourceInventoryID, int sourceSlotIndex, EquipSlot targetSlot, RpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;

        InventoryData sourceInventory = InventoryDataRegistry.GetByOwnerAndID(clientId, sourceInventoryID);
        EquipmentData equipment = EquipmentDataRegistry.GetByOwner(clientId);
        if (sourceInventory == null || equipment == null) return;
        if (sourceSlotIndex < 0 || sourceSlotIndex >= sourceInventory.InventoryItems.Count) return;

        equipment.EquipFromInventorySlot(sourceInventory, sourceSlotIndex, targetSlot);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestUnequipServerRpc(EquipSlot slot, int targetInventoryID, RpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;

        InventoryData targetInventory = InventoryDataRegistry.GetByOwnerAndID(clientId, targetInventoryID);
        EquipmentData equipment = EquipmentDataRegistry.GetByOwner(clientId);
        if (targetInventory == null || equipment == null) return;

        equipment.UnequipToFirstEmptySlot(slot, targetInventory);
    }
}
