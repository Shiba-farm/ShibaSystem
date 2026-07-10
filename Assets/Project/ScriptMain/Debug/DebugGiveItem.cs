using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Debug-only: กด G เพื่อ give ไอเทมชุดทดสอบใน inventory ของผู้เล่น local
/// ลบ / disable component นี้ก่อน build จริง
/// วางบน GameObject ใดก็ได้ในฉาก (เช่น NetworkManager หรือ DebugTools)
/// </summary>
public class DebugGiveItem : MonoBehaviour
{
    [System.Serializable]
    public struct DebugGiveEntry
    {
        public ItemSO item;
        [Tooltip("จำนวนที่จะ give ของไอเทมชิ้นนี้โดยเฉพาะ (แยกอิสระจากชิ้นอื่น)")]
        public int amount;
    }

    [Header("ลาก ItemSO ที่จะ give ใส่ + ตั้งจำนวนแยกแต่ละชิ้นได้เลย")]
    [SerializeField] private DebugGiveEntry[] itemsToGive;

    [Header("inventoryID ของกระเป๋าหลัก (ปกติ = 0)")]
    [SerializeField] private int targetInventoryID = 0;

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.G)) return;
        if (!NetworkManager.Singleton.IsClient) return;

        if (InventoryNetworkManager.Instance == null)
        {
            Debug.LogWarning("[DebugGiveItem] InventoryNetworkManager.Instance ไม่มีในฉาก");
            return;
        }

        foreach (var entry in itemsToGive)
        {
            if (entry.item == null) continue;

            int amount = Mathf.Max(1, entry.amount); // กันใส่ 0/ค่าติดลบมาโดยไม่ตั้งใจ
            // RequestAddItemServerRpc ใช้ SenderClientId จาก rpcParams ค้นหา inventory เอง
            InventoryNetworkManager.Instance.RequestAddItemServerRpc(targetInventoryID, entry.item.itemID, amount);
            Debug.Log($"[DebugGiveItem] Give {entry.item.itemName} (ID={entry.item.itemID}) x{amount}");
        }
    }
}
