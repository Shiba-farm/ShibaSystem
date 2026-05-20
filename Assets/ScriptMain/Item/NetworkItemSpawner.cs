using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetworkItemSpawner : NetworkBehaviour
{
    public static NetworkItemSpawner Instance { get; private set; }

    [System.Serializable]
    public class WorldItemPrefabEntry
    {
        public int itemID;
        public WorldItem prefab;
    }

    [SerializeField] private WorldItem defaultPrefab;
    [SerializeField] private List<WorldItemPrefabEntry> prefabOverrides;

    private Dictionary<int, WorldItem> _prefabLookup;

    private void Awake()
    {
        Instance = this;
        _prefabLookup = new Dictionary<int, WorldItem>();
        foreach (var entry in prefabOverrides)
            _prefabLookup[entry.itemID] = entry.prefab;
    }

    public void SpawnItem(int itemID, int quantity, Vector3 position)
    {
        if (!IsServer) return;

        // use override prefab if exists, otherwise fall back to default
        var prefab = _prefabLookup.TryGetValue(itemID, out var p) ? p : defaultPrefab;

        for (int i = 0; i < quantity; i++)
        {
            // random offset so items scatter around the source
            Vector3 randomOffset = new Vector3(
                Random.Range(-1f, 1f),
                Random.Range(0.5f, 1.5f),   // slight upward pop
                Random.Range(-1f, 1f));

            var obj = Instantiate(prefab, position + randomOffset, Quaternion.identity);
            obj.itemID.Value = itemID;
            obj.quantity.Value = 1;         // each world item = 1 unit
            obj.GetComponent<NetworkObject>().Spawn();
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestSpawnItemServerRpc(int itemID, int qty, Vector3 pos)
    {
        SpawnItem(itemID, qty, pos);
    }
}
