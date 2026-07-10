using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class InventoryData : NetworkSaveableBehaviour
{
    // ── Mock data entry — แก้ใน Inspector ได้โดยตรง ────────────────────────
    [System.Serializable]
    public struct MockItemEntry
    {
        public ItemSO item;   // drag ItemSO asset มาใส่ได้เลย
        public int amount;
    }

    [SerializeField] private int inventorySize = 20;
    [SerializeField] private InventoryDataSignal connectionSignal;
    [SerializeField] private int inventoryID;
    [SerializeField] private bool needMockData = true;

    [Tooltip("ลาก ItemSO จาก Project มาวางเพื่อตั้งของเริ่มต้น — ไม่ต้องรู้ ItemID")]
    [SerializeField] private List<MockItemEntry> mockItems = new();
    public NetworkList<NetworkItems> InventoryItems;
    public int InventoryID => inventoryID;
    public override bool IsPlayerSaveable => true;

    // AchievementManager อยู่บน root GameObject คนละอันกับ InventoryData (เหมือนที่ QuestManager
    // เจอ) เลยหาผ่าน GetComponentInParent ตอน spawn แทน GetComponent ตรงๆ
    private AchievementManager achievementManager; // red-flag code

    void Awake()
    {
        InventoryItems = new NetworkList<NetworkItems>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        achievementManager = GetComponentInParent<AchievementManager>();
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
            for (int i = 0; i < mockItems.Count && i < InventoryItems.Count; i++)
            {
                var entry = mockItems[i];
                if (entry.item == null) continue; // slot ว่าง = ข้าม
                InventoryItems[i] = new NetworkItems
                {
                    ItemID = entry.item.itemID,
                    Amount = Mathf.Max(1, entry.amount)
                };
            }
            Debug.Log($"[InventoryData] Mock data loaded for inventory {inventoryID} ({mockItems.Count} items)");
        }
    }

    public void AddItem(int itemId, int amount)
    {
        if (itemId <= 0)
        {
            Debug.LogError($"[InventoryData] AddItem: itemId {itemId} ไม่ถูกต้อง (ต้องมากกว่า 0) — เช็คว่า ItemSO มี itemID ตั้งไว้หรือยัง และอยู่ใน ItemDatabases หรือไม่");
            return;
        }

        // แจ้ง Achievement ว่าผู้เล่นได้ไอเทมนี้แล้ว — ให้ระบบ Collectible เช็คเองว่าตรงกับของ
        // สะสมชิ้นไหนไหม (เควส/Debug Give Item(G)/ระบบเก็บของในอนาคต เข้ามาทาง AddItem จุดนี้
        // จุดเดียวหมด เลยไม่ต้องผูก ReportItemObtained แยกทีละระบบ)
        achievementManager?.ReportItemObtained(itemId); // red-flag code

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

            Debug.Log($"Stacked ItemID {itemId} x{toAdd} onto slot {i} (now x{updated.Amount})");
            amount -= toAdd;
            if (amount <= 0) return;
        }

        // 2. เหลือของอยู่ (ไม่มีกองเดิมให้เติม หรือเติมจนเต็มแล้วแต่ยังไม่หมด) — ใส่ช่องว่างใหม่ทีละช่อง
        //    บั๊กที่เจอ: เดิมโค้ดจุดนี้ยัด "amount" ทั้งก้อนลงช่องเดียวโดยไม่ clamp ด้วย maxStack เลย
        //    ทำให้ไอเทม non-stackable (เช่น Wearable/Tool ที่ isStackable=false, maxStack=1) พอสั่งให้
        //    ทีละหลายชิ้น (เช่นปุ่ม G debug ให้ทีละ 10) จะโดนกองรวมเป็นช่องเดียว Amount=10 ทั้งที่ไม่ควร
        //    stack กันได้เลย — แก้โดยวนใส่ทีละช่อง ช่องละไม่เกิน maxStack จนกว่าของจะหมดหรือกระเป๋าเต็ม
        for (int i = 0; i < InventoryItems.Count && amount > 0; i++)
        {
            if (InventoryItems[i].ItemID != 0) continue;

            int toAdd = Mathf.Min(amount, maxStack);
            InventoryItems[i] = new NetworkItems { ItemID = itemId, Amount = toAdd };
            Debug.Log($"Filled empty slot {i} with ItemID {itemId} x{toAdd}");
            amount -= toAdd;
        }

        if (amount > 0)
        {
            Debug.LogWarning($"[InventoryData] AddItem: กระเป๋าเต็ม ใส่ ItemID {itemId} ไม่หมด เหลือตกหล่น {amount} ชิ้น");
            // WorldItemManager.Instance.SpawnWorldItem(itemID, amount, dropPosition);
        }
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
