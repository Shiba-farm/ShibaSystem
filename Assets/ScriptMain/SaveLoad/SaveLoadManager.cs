using System.Collections.Generic;
using System.IO;
using Unity.Netcode;
using UnityEngine;

public class SaveLoadManager : NetworkBehaviour
{
    public static SaveLoadManager Instance { get; private set; }


    [Header("Settings")]
    [SerializeField] private string saveFileName = "shiba_farm_slot_{0}.json";

    private int _currentSlot = 0;

    // ── Private State ────────────────────────────────────────
    private GameSaveData _loadedSave;   // held in memory after load, used for late joiners
    private bool _saveLoaded = false;
    private ISaveStorage _storage;

    // ── Ordered lists — order matters for restore ────────────
    private readonly List<ISaveable> _worldSaveables = new();
    private readonly List<ISaveable> _playerSaveables = new();

    private static readonly List<ISaveable> _pendingRegistrations = new();
    private string _worldName = "";

    private bool _isTransitioningScene = false;


    // NOTE: static helpers used by main menu before NGO/storage initializes.
    // When adding SteamCloudStorage, also update SaveSlotReader to check cloud.
    public static string GetSavePathStatic(int slot)
    => Path.Combine(Application.persistentDataPath, $"shiba_farm_slot_{slot}.json");

    public static bool SaveFileExists(int slot) => File.Exists(GetSavePathStatic(slot));

    // In SaveLoadManager.cs
    public GameSaveData GetLoadedSave() => _saveLoaded ? _loadedSave : null;

    // ────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null) { Destroy(this); return; }
        Instance = this;
    }

    public void SetSceneTransitioning(bool value)
    {
        _isTransitioningScene = value;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        _storage = new LocalFileStorage();

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;

        foreach (var saveable in _pendingRegistrations)
            AddToList(saveable);
        _pendingRegistrations.Clear();

        // Read which slot we're using regardless of new/load
        if (GlobalSaveContext.Instance != null)
        {
            _currentSlot = GlobalSaveContext.Instance.TargetSlot;
            _worldName = GlobalSaveContext.Instance.PendingWorldName;

            if (GlobalSaveContext.Instance.ShouldLoadOnStart)
            {
                GlobalSaveContext.Instance.Consume();
                LoadGame(_currentSlot);
            }
        }
    }

    public void Register(ISaveable saveable)
    {
        if (saveable is HotbarUIController hotbar)
            Debug.Log("Found hotbar");
        if (!IsSpawned)
        {
            // SaveLoadManager not ready yet — queue it
            if (!_pendingRegistrations.Contains(saveable))
                _pendingRegistrations.Add(saveable);
            return;
        }

        Debug.Log($"[SaveLoad] Register called: {saveable.GetType().Name} " +
              $"player:{saveable.IsPlayerSaveable} " +
              $"playerSaveables count:{_playerSaveables.Count}");


        AddToList(saveable);
    }


    public void Unregister(ISaveable saveable)
    {
        Debug.Log($"[SaveLoad] Unregister called: {saveable.GetType().Name}");
        _playerSaveables.Remove(saveable);
        _worldSaveables.Remove(saveable);
        _pendingRegistrations.Remove(saveable);
    }

    private void AddToList(ISaveable saveable)
    {
        if (saveable.IsPlayerSaveable)
        {
            if (!_playerSaveables.Contains(saveable))
            {
                _playerSaveables.Add(saveable);
                Debug.Log($"[SaveLoad] Registered player saveable: {saveable.GetType().Name}");
            }
        }
        else
        {
            if (!_worldSaveables.Contains(saveable))
            {
                _worldSaveables.Add(saveable);
                Debug.Log($"[SaveLoad] Registered world saveable: {saveable.GetType().Name}");
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
            slotIndex = slot,
            worldName = _worldName
        };

        foreach (var saveable in _worldSaveables)
            saveable.CaptureState(save);

        foreach (var client in NetworkManager.ConnectedClientsList)
        {
            var playerObj = client.PlayerObject;
            if (playerObj == null) continue;
            CapturePlayerState(client.ClientId, save);
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
            RestorePlayerState(client.ClientId);
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

    private void CapturePlayerState(ulong clientId, GameSaveData save)
    {
        foreach (var saveable in _playerSaveables)
        {
            if (GetOwnerClientId(saveable) == clientId)
                saveable.CaptureState(save, clientId);
        }
    }

    private ulong GetOwnerClientId(ISaveable saveable)
    {
        // Local UI saveable — has its own OwnerClientId property
        if (saveable is ILocalSaveable local)
            return local.OwnerClientId;

        // NetworkBehaviour saveable — use NGO's built-in
        if (saveable is NetworkBehaviour nb)
            return nb.OwnerClientId;

        return 0;
    }


    // Called by NGO when any client connects (including host)
    private void OnClientConnected(ulong clientId)
    {
        if (!_saveLoaded) return;
        if (_isTransitioningScene) return;  // ← skip during scene transitions

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

        RestorePlayerState(client.ClientId);
    }

    private void RestorePlayerState(ulong clientId)
    {
        var playerData = _loadedSave?.FindPlayer(clientId);
        if (playerData == null)
        {
            Debug.Log($"[SaveLoad] No save data for client {clientId} — using defaults.");
            return;
        }

        foreach (var saveable in _playerSaveables)
        {
            if (GetOwnerClientId(saveable) == clientId)
                saveable.RestoreState(_loadedSave, clientId);
        }

        Debug.Log($"[SaveLoad] Restored player {clientId}");
    }
}
