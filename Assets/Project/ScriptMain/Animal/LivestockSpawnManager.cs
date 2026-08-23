using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Scene-placed NetworkBehaviour singleton — spawns purchased livestock into the world
/// (server-only) and hands the buyer a rope item so they can lead the animal home.
///
/// Mirrors NetworkItemSpawner's per-ID prefab lookup pattern, but keyed on AnimalSO.animalId
/// instead of ItemSO.itemID, since livestock prefabs are creatures (NetworkObject + AI/etc.)
/// rather than pickup-able WorldItems.
///
/// Called by AnimalStockServerManager.BuyLiveStockServerRpc after a purchase is validated
/// (gold deducted) — see the "SpawnManager" step in the buy-flow diagram.
/// </summary>
public class LivestockSpawnManager : NetworkBehaviour
{
    public static LivestockSpawnManager Instance { get; private set; }

    [System.Serializable]
    public class LivestockPrefabEntry
    {
        public int animalId;
        public GameObject prefab;
    }

    [Header("Prefabs")]
    [Tooltip("ใช้ตอนไม่เจอ prefab เฉพาะของ animalId นั้นๆ ใน Prefab Overrides ด้านล่าง")]
    [SerializeField] private GameObject defaultLivestockPrefab;
    [SerializeField] private List<LivestockPrefabEntry> prefabOverrides = new();

    [Header("Spawn Placement")]
    [Tooltip("ระยะห่างจากตัวผู้เล่นที่จะ spawn สัตว์ (ไปทางด้านหน้าที่ผู้เล่นหันอยู่)")]
    [SerializeField] private float spawnDistance = 2f;

    [Header("Rope (Optional)")]
    [Tooltip("ไอเทมเชือกที่จะให้ผู้เล่นตอนซื้อสัตว์สำเร็จ — ถ้าไม่ผูกไว้จะข้ามขั้นตอนนี้ไปเฉยๆ")]
    [SerializeField] private ItemSO ropeItem;
    [Tooltip("Inventory ID ที่จะใส่เชือกเข้าไป — 0 = กระเป๋าหลัก (ตามธรรมเนียมเดิมในโปรเจกต์นี้)")]
    [SerializeField] private int ropeInventoryID = 0;
    [SerializeField] private float fromGroundOffset = 0.3f;

    private Dictionary<int, GameObject> _prefabLookup;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        _prefabLookup = new Dictionary<int, GameObject>();
        foreach (var entry in prefabOverrides)
            if (entry != null) _prefabLookup[entry.animalId] = entry.prefab;
    }

    /// <summary>
    /// Spawns <paramref name="animal"/> in front of the given client's player, and gives
    /// them the rope item (if assigned). Server-only — no-op on clients.
    /// Returns the spawned GameObject, or null if it couldn't be spawned.
    /// </summary>
    public GameObject SpawnLivestockForPlayer(AnimalSO animal, ulong clientId)
    {
        if (!IsServer) return null;
        if (animal == null) return null;

        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client) || client.PlayerObject == null)
        {
            Debug.LogWarning($"[LivestockSpawnManager] ไม่พบ PlayerObject ของ client {clientId} — spawn livestock ไม่ได้");
            return null;
        }

        Transform playerTransform = client.PlayerObject.transform;

        var prefab = _prefabLookup.TryGetValue(animal.animalId, out var p) && p != null ? p : defaultLivestockPrefab;
        if (prefab == null)
        {
            Debug.LogWarning($"[LivestockSpawnManager] ไม่มี prefab ให้ animalId={animal.animalId} ({animal.animalName}) และไม่มี Default Livestock Prefab ผูกไว้ด้วย");
            return null;
        }

        Vector3 spawnPos = playerTransform.position + playerTransform.forward * spawnDistance + Vector3.up * fromGroundOffset;
        var obj = Instantiate(prefab, spawnPos, Quaternion.identity);
        var netObj = obj.GetComponent<NetworkObject>();
        if (netObj != null) netObj.Spawn();

        GiveRope(clientId);

        return obj;
    }

    private void GiveRope(ulong clientId)
    {
        if (ropeItem == null) return;

        InventoryData inventory = InventoryDataRegistry.GetByOwnerAndID(clientId, ropeInventoryID);
        if (inventory == null)
        {
            Debug.LogWarning($"[LivestockSpawnManager] ไม่พบ inventory (id={ropeInventoryID}) ของ client {clientId} — ให้เชือกไม่ได้");
            return;
        }

        inventory.AddItem(ropeItem.itemID, 1);
    }
}
