using System.Collections.Generic;
using System.IO;
using Unity.Netcode;
using UnityEngine;

public class SaveLoadManager : NetworkBehaviour
{
    public static SaveLoadManager Instance { get; private set; }

    // ── Inspector ────────────────────────────────────────────
    [Header("World Saveables (assign in Inspector)")]
    [SerializeField] private WorldTimeManager worldTimeManager;
    [SerializeField] private CurrencyManager currencyManager;
    [SerializeField] private DebtManager debtManager;


    [Header("Settings")]
    [SerializeField] private string saveFileName = "shiba_farm_slot_{0}.json";

    private int _currentSlot = 0;

    // ── Private State ────────────────────────────────────────
    private GameSaveData _loadedSave;   // held in memory after load, used for late joiners
    private bool _saveLoaded = false;
    private ISaveStorage _storage;

    // ── Ordered lists — order matters for restore ────────────
    private readonly List<ISaveable> _worldSaveables = new();

    // Per-player saveables are fetched from the player object at runtime
    // Order must be: InventoryNetworkManager → HotbarUIController → rest
    private static readonly System.Type[] PlayerSaveOrder = new[]
    {
        typeof(InventoryNetworkManager),
        typeof(HotbarUIController),      // must be after inventory
        typeof(StatManager),
        typeof(PlayerController),
        typeof(PlayerHeldItem),          // last — reacts to above
    };


    // NOTE: static helpers used by main menu before NGO/storage initializes.
    // When adding SteamCloudStorage, also update SaveSlotReader to check cloud.
    public static string GetSavePathStatic(int slot)
    => Path.Combine(Application.persistentDataPath, $"shiba_farm_slot_{slot}.json");

    public static bool SaveFileExists(int slot) => File.Exists(GetSavePathStatic(slot));

    // ────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null) { Destroy(this); return; }
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        _worldSaveables.Add(worldTimeManager);
        _worldSaveables.Add(currencyManager);
        _worldSaveables.Add(debtManager);

        _storage = new LocalFileStorage();

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;

        // Read which slot we're using regardless of new/load
        if (GlobalSaveContext.Instance != null)
        {
            _currentSlot = GlobalSaveContext.Instance.TargetSlot;

            if (GlobalSaveContext.Instance.ShouldLoadOnStart)
            {
                GlobalSaveContext.Instance.Consume();
                LoadGame(_currentSlot);
            }
        }
    }

    public void SaveGame()
    {
        SaveGame(_currentSlot);   // always saves to the slot we loaded/started with
    }

    private void SaveGame(int slot)
    {
        if (!IsServer) return;

        var save = new GameSaveData
        {
            savedAt = System.DateTime.UtcNow.ToString("o"),
            slotIndex = slot
        };

        foreach (var saveable in _worldSaveables)
            saveable.CaptureState(save);

        foreach (var client in NetworkManager.ConnectedClientsList)
        {
            var playerObj = client.PlayerObject;
            if (playerObj == null) continue;
            CapturePlayerState(playerObj.gameObject, client.ClientId, save);
        }

        string json = JsonUtility.ToJson(save, prettyPrint: true);
        string fileName = string.Format(saveFileName, slot);
        _storage.Write(fileName, json);

        _loadedSave = save;
        _saveLoaded = true;

        Debug.Log($"[SaveLoad] Saved to slot {slot}");
    }

    private void LoadGame(int slot)
    {

        string fileName = string.Format(saveFileName, slot);

        if (!_storage.Exists(fileName))   // ← goes through storage interface
        {
            Debug.Log($"[SaveLoad] No save in slot {slot}");
            return;
        }

        string json = _storage.Read(fileName);
        _loadedSave = JsonUtility.FromJson<GameSaveData>(json);
        _saveLoaded = true;
        _currentSlot = slot;

        foreach (var saveable in _worldSaveables)
            saveable.RestoreState(_loadedSave);

        foreach (var client in NetworkManager.ConnectedClientsList)
        {
            var playerObj = client.PlayerObject;
            if (playerObj == null) continue;
            RestorePlayerState(playerObj.gameObject, client.ClientId);
        }

        Debug.Log($"[SaveLoad] Loaded slot {slot}");
    }

    public override void OnNetworkDespawn()
    {
        if (!IsServer) return;
        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
    }

    // ────────────────────────────────────────────────────────
    // SAVE
    // ────────────────────────────────────────────────────────

    private void CapturePlayerState(GameObject playerObj, ulong clientId, GameSaveData save)
    {
        foreach (var type in PlayerSaveOrder)
        {
            if (playerObj.TryGetComponent(type, out var component)
                && component is ISaveable saveable)
            {
                saveable.CaptureState(save, clientId);
            }
        }
    }

    // Called by NGO when any client connects (including host)
    private void OnClientConnected(ulong clientId)
    {
        if (!_saveLoaded) return;

        // Player object may not exist yet — wait one frame
        StartCoroutine(RestorePlayerNextFrame(clientId));
    }

    private System.Collections.IEnumerator RestorePlayerNextFrame(ulong clientId)
    {
        yield return null;  // wait for NGO to finish spawning the player object

        if (!NetworkManager.ConnectedClients.TryGetValue(clientId, out var client))
            yield break;

        var playerObj = client.PlayerObject;
        if (playerObj == null)
        {
            Debug.LogWarning($"[SaveLoad] No player object found for client {clientId}");
            yield break;
        }

        RestorePlayerState(playerObj.gameObject, clientId);
    }

    private void RestorePlayerState(GameObject playerObj, ulong clientId)
    {
        var playerData = _loadedSave?.FindPlayer(clientId);
        if (playerData == null)
        {
            Debug.Log($"[SaveLoad] No save data for client {clientId} — using defaults.");
            return;
        }

        // Restore in strict order
        foreach (var type in PlayerSaveOrder)
        {
            if (playerObj.TryGetComponent(type, out var component)
                && component is ISaveable saveable)
            {
                saveable.RestoreState(_loadedSave, clientId);
            }
        }

        Debug.Log($"[SaveLoad] Restored player {clientId}");
    }
}
