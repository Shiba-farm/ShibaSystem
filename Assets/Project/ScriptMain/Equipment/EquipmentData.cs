using Unity.Netcode;
using UnityEngine;

/// <summary>
/// เก็บไอเทมที่ผู้เล่นสวมอยู่ในแต่ละ EquipSlot — โครงสร้างและ lifecycle เลียนแบบ
/// InventoryData ทุกจุด (Signal pattern, Registry pattern, Save/Load ผ่าน
/// NetworkSaveableBehaviour) เพื่อให้ทีมที่คุ้นกับ Inventory อ่านโค้ดนี้ได้ทันที
/// วางไว้บน Player prefab จุดเดียวกับ InventoryData / StatManager
/// </summary>
public class EquipmentData : NetworkSaveableBehaviour
{
    [SerializeField] private EquipmentDataSignal connectionSignal;
    public NetworkList<NetworkEquipment> EquippedItems;
    public override bool IsPlayerSaveable => true;

    private StatManager _statManager;

    private void Awake()
    {
        EquippedItems = new NetworkList<NetworkEquipment>();
        _statManager = GetComponent<StatManager>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        EquipmentDataRegistry.RegisterOwner(OwnerClientId, this);

        if (IsServer)
        {
            InitializeSlots();
            SaveLoadManager.Instance?.Register(this);
        }
        if (IsOwner)
        {
            // Signal สำหรับ UI เท่านั้น (PlayerPreviewUI, EquipmentSlotsPanelUI)
            // ทำงานได้ถูกต้องเพราะ UI ต้องแสดงข้อมูลของ local player เท่านั้น
            connectionSignal.UpdateEquipmentData(this);
        }

        // แจ้ง PlayerWearableVisual โดยตรง (ไม่ผ่าน Signal ที่เป็น shared SO)
        GetComponentInChildren<PlayerWearableVisual>()?.ConnectToData(this);
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        EquipmentDataRegistry.UnregisterOwner(OwnerClientId);
        if (IsServer)
            SaveLoadManager.Instance?.Unregister(this);

        GetComponentInChildren<PlayerWearableVisual>()?.DisconnectData();
    }

    private void InitializeSlots()
    {
        if (EquippedItems.Count > 0) return;
        foreach (EquipSlot slot in System.Enum.GetValues(typeof(EquipSlot)))
            EquippedItems.Add(new NetworkEquipment { Slot = slot, ItemID = 0 });
    }

    public int GetEquippedItemId(EquipSlot slot)
    {
        for (int i = 0; i < EquippedItems.Count; i++)
            if (EquippedItems[i].Slot == slot) return EquippedItems[i].ItemID;
        return 0;
    }

    /// <summary>
    /// รวม speedBonus ของไอเทม Wearable ที่สวมอยู่ทุกช่อง (Helmet/Ring/Shield/Boots) —
    /// PlayerController.HandleMovement() เรียกทุกเฟรมเพื่อบวกเข้า targetSpeed ตรง ๆ
    /// คำนวณสดจากของที่สวมอยู่จริงเสมอ ไม่มีทาง desync (ไม่ต้องรอ OnEquip/OnUnequip push ค่า)
    /// </summary>
    public float GetTotalSpeedBonus()
    {
        float total = 0f;
        for (int i = 0; i < EquippedItems.Count; i++)
        {
            if (EquippedItems[i].ItemID == 0) continue;
            if (GameDataManager.Instance.itemDatabases.GetItemByID(EquippedItems[i].ItemID) is WearableItemSO wearable)
                total += wearable.speedBonus;
        }
        return total;
    }

    /// <summary>
    /// สวมไอเทมจาก inventory slot ลงช่อง equip — ถ้าช่องมีไอเทมเดิมอยู่แล้วจะสลับ
    /// (ไอเทมเดิมกลับไปที่ inventory slot เดียวกัน) เรียกจาก server เท่านั้น —
    /// validation ทั้งหมดทำที่ EquipmentNetworkManager ก่อนเรียกมาที่นี่
    /// </summary>
    public void EquipFromInventorySlot(InventoryData sourceInventory, int sourceSlotIndex, EquipSlot targetSlot)
    {
        if (!IsServer) return;

        NetworkItems sourceItem = sourceInventory.InventoryItems[sourceSlotIndex];
        ItemSO itemSO = GameDataManager.Instance.itemDatabases.GetItemByID(sourceItem.ItemID);
        if (itemSO is not WearableItemSO wearable || wearable.Slot != targetSlot) return;

        int previousItemId = GetEquippedItemId(targetSlot);

        // ปลดไอเทมเดิมออกก่อน (ถ้ามี) แล้วใส่ไอเทมเดิมกลับลง inventory slot เดียวกัน
        if (previousItemId != 0)
        {
            ItemSO previousSO = GameDataManager.Instance.itemDatabases.GetItemByID(previousItemId);
            if (previousSO is WearableItemSO previousWearable && _statManager != null)
                previousWearable.OnUnequip(_statManager);
        }

        SetSlot(targetSlot, sourceItem.ItemID);
        sourceInventory.InventoryItems[sourceSlotIndex] = new NetworkItems { ItemID = previousItemId, Amount = previousItemId != 0 ? 1 : 0 };

        if (_statManager != null) wearable.OnEquip(_statManager);
    }

    /// <summary>
    /// ถอดไอเทมจากช่อง equip กลับไปที่ inventory — หาช่องว่างแรกเอง (เหมือนวิธีที่
    /// InventoryData.AddItem ใช้) ทำให้ UI ไม่ต้องรู้ว่า slot ไหนว่างอยู่
    /// </summary>
    public void UnequipToFirstEmptySlot(EquipSlot slot, InventoryData targetInventory)
    {
        if (!IsServer) return;

        int itemId = GetEquippedItemId(slot);
        if (itemId == 0) return;

        for (int i = 0; i < targetInventory.InventoryItems.Count; i++)
        {
            if (targetInventory.InventoryItems[i].ItemID != 0) continue;

            ItemSO itemSO = GameDataManager.Instance.itemDatabases.GetItemByID(itemId);
            if (itemSO is WearableItemSO wearable && _statManager != null)
                wearable.OnUnequip(_statManager);

            SetSlot(slot, 0);
            targetInventory.InventoryItems[i] = new NetworkItems { ItemID = itemId, Amount = 1 };
            return;
        }

        Debug.LogWarning("[EquipmentData] Inventory ไม่มีช่องว่างให้ถอดไอเทมออก");
    }

    private void SetSlot(EquipSlot slot, int itemId)
    {
        for (int i = 0; i < EquippedItems.Count; i++)
        {
            if (EquippedItems[i].Slot != slot) continue;
            EquippedItems[i] = new NetworkEquipment { Slot = slot, ItemID = itemId };
            return;
        }
    }

    public override void CaptureState(GameSaveData save, ulong clientId = 0)
    {
        var playerData = save.GetOrCreatePlayer(OwnerClientId);
        playerData.equipment.Clear();
        foreach (var e in EquippedItems)
            playerData.equipment.Add(new EquipmentSlotSaveData { slot = e.Slot, itemID = e.ItemID });
    }

    public override void RestoreState(GameSaveData save, ulong clientId = 0)
    {
        if (!IsServer) return;
        var playerData = save.FindPlayer(OwnerClientId);
        if (playerData == null) return;

        foreach (var saved in playerData.equipment)
            SetSlot(saved.slot, saved.itemID);
    }
}

[System.Serializable]
public class EquipmentSlotSaveData
{
    public EquipSlot slot;
    public int itemID;
}
