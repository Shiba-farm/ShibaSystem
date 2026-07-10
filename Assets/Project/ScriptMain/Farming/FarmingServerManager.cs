using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Server-authoritative manager for all farming actions (tilling, watering, harvesting).
///
/// Design contract:
///   • Clients NEVER send world positions — they send grid cell coordinates (two ints).
///   • The server re-validates that the requested cell is within reach of the sender's
///     actual player position before doing anything. A client that lies about the cell
///     simply gets rejected.
///   • All state mutations (spawning tilled dirt, etc.) happen here on the server
///     and replicate to all clients via NetworkObject spawn.
///
/// Setup:
///   1. Place this as a scene NetworkObject (not spawned at runtime).
///   2. Assign TilledDirtPrefab in the Inspector (must be registered in NetworkManager prefabs).
///   3. CellSize and MaxCells must match the values in FarmTileCursor.
/// </summary>
public class FarmingServerManager : NetworkBehaviour
{
    public static FarmingServerManager Instance { get; private set; }

    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Prefabs")]
    [Tooltip("NetworkObject prefab for dry tilled dirt. Must be in NetworkManager prefab list.")]
    [SerializeField] private NetworkObject tilledDirtPrefab;

    [Tooltip("NetworkObject prefab for wet/soaked tilled dirt. Must be in NetworkManager prefab list.")]
    [SerializeField] private NetworkObject wetDirtPrefab;

    [Tooltip("How far above the dirt surface to position a freshly planted seed visual.")]
    [SerializeField] private float seedHeightOffset = 0.05f;

    [Header("Grid — must match FarmTileCursor")]
    [Tooltip("World units per grid cell. Keep in sync with FarmTileCursor.cellSize.")]
    [SerializeField] private float cellSize = 1f;

    [Tooltip("Max reach radius in cells. Keep in sync with FarmTileCursor.maxCells.")]
    [SerializeField] private int   maxCells = 4;

    // ── Server-side state ─────────────────────────────────────────────────────
    // The spawned NetworkObjects are the sync mechanism — clients see them via
    // normal NGO object spawn. This dictionary is server-only bookkeeping.

    // Server-only: authoritative record used for spawn/despawn decisions.
    private readonly Dictionary<Vector2Int, NetworkObject> _tilledCells  = new();
    private readonly Dictionary<Vector2Int, NetworkObject> _plantedCells = new();

    // Server-only: crop growth state, one entry per planted cell.
    private readonly Dictionary<Vector2Int, CropData> _cropData = new();

    // Server-only: cells that currently show wet dirt (subset of _tilledCells).
    // Cleared each morning when soil resets to dry.
    private readonly HashSet<Vector2Int> _wetCells = new();

    // Server-only: cells that have been fertilized but not yet planted.
    // Value = accumulated effectValue (days) from applied fertilizers.
    // Consumed when a seed is planted on the cell.
    private readonly Dictionary<Vector2Int, int> _fertilizedCells = new();

    // All instances (server + every client): populated via ClientRpc so the cursor
    // can check cell state locally without a round-trip to the server.
    private readonly HashSet<Vector2Int> _knownTilledCells    = new();
    private readonly HashSet<Vector2Int> _knownPlantedCells   = new();
    private readonly HashSet<Vector2Int> _knownFertilizedCells = new();

    // ── Signals (static — UI subscribes without needing a scene reference) ───

    /// <summary>
    /// Fired on the local client only when the server confirms a successful harvest
    /// and items have been added to the player's inventory.
    /// Parameters: (itemId, amount)
    /// Subscribe in HarvestPopupUI to show the pickup notification.
    /// </summary>
    public static event Action<int, int> OnHarvestNotification;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        // Check if GameDataManager has a snapshot for this scene from before the
        // last scene transition. If so, re-spawn all tiles and crops one frame later
        // (so NGO and the terrain are fully ready to accept new spawns).
        string sceneName = SceneManager.GetActiveScene().name;
        FarmTransitionState pending = GameDataManager.Instance?.GetAndClearFarmState(sceneName);
        if (pending != null)
            StartCoroutine(RestoreFarmStateNextFrame(pending));
    }

    // ── Scene-transition capture / restore ────────────────────────────────────

    /// <summary>
    /// Serialises the current tile and crop state into a plain data snapshot.
    /// Called by GameDataManager.CaptureFarmForTransition() immediately before a
    /// LoadSceneMode.Single swap so the data survives the manager's despawn.
    /// </summary>
    public FarmTransitionState CaptureTransitionState()
    {
        var state = new FarmTransitionState();

        foreach (var kvp in _tilledCells)
        {
            if (kvp.Value == null) continue;
            state.tilledCells.Add(new TileTransitionData
            {
                cellX          = kvp.Key.x,
                cellZ          = kvp.Key.y,
                groundY        = kvp.Value.transform.position.y,
                isWet          = _wetCells.Contains(kvp.Key),
                isFertilized   = _fertilizedCells.ContainsKey(kvp.Key),
                fertilizedDays = _fertilizedCells.TryGetValue(kvp.Key, out int d) ? d : 0
            });
        }

        foreach (var kvp in _cropData)
        {
            var c = kvp.Value;
            state.croppedCells.Add(new CropTransitionData
            {
                cellX              = kvp.Key.x,
                cellZ              = kvp.Key.y,
                seedItemId         = c.seedItemId,
                currentStage       = c.currentStage,
                daysInCurrentStage = c.daysInCurrentStage,
                wateredThisDay     = c.wateredThisDay,
                hydration          = c.hydration,
                isReadyToHarvest   = c.isReadyToHarvest,
                groundY            = c.groundY
            });
        }

        return state;
    }

    /// <summary>
    /// Re-spawns all tilled dirt and crop visuals from the captured snapshot,
    /// then rebuilds the server-side dictionaries and notifies all clients.
    /// Runs one frame after OnNetworkSpawn so the terrain is ready for raycasts.
    /// </summary>
    private IEnumerator RestoreFarmStateNextFrame(FarmTransitionState state)
    {
        yield return null;

        // ── Tilled cells (and their fertilizer state) ─────────────────────────
        foreach (var t in state.tilledCells)
        {
            var cell = new Vector2Int(t.cellX, t.cellZ);
            Vector3 worldPos = CellToWorld(cell);
            worldPos.y = t.groundY;

            NetworkObject prefab   = t.isWet ? wetDirtPrefab : tilledDirtPrefab;
            NetworkObject spawned  = Instantiate(prefab, worldPos, Quaternion.identity);
            spawned.Spawn(destroyWithScene: true);

            _tilledCells[cell] = spawned;
            if (t.isWet) _wetCells.Add(cell);
            NotifyTilledClientRpc(cell.x, cell.y);

            if (t.isFertilized)
            {
                _fertilizedCells[cell] = t.fertilizedDays;
                NotifyFertilizedClientRpc(cell.x, cell.y);
            }
        }

        // ── Crops ─────────────────────────────────────────────────────────────
        foreach (var c in state.croppedCells)
        {
            var cell     = new Vector2Int(c.cellX, c.cellZ);
            var itemData = GameDataManager.Instance.itemDatabases.GetItemByID(c.seedItemId);
            if (itemData is not SeedItemSO seed) continue;

            int stageToShow = c.isReadyToHarvest
                ? seed.stages.Length - 1
                : c.currentStage;

            if (stageToShow >= 0 && stageToShow < seed.stages.Length
                && seed.stages[stageToShow].visualPrefab != null)
                SpawnStageVisual(cell, seed.stages[stageToShow].visualPrefab, c.groundY);

            _cropData[cell] = new CropData
            {
                seedItemId         = c.seedItemId,
                currentStage       = c.currentStage,
                daysInCurrentStage = c.daysInCurrentStage,
                wateredThisDay     = c.wateredThisDay,
                hydration          = c.hydration,
                isReadyToHarvest   = c.isReadyToHarvest,
                groundY            = c.groundY
            };

            NotifyPlantedClientRpc(cell.x, cell.y);
        }

        Debug.Log($"[FarmingServerManager] Restored {state.tilledCells.Count} tiles " +
                  $"and {state.croppedCells.Count} crops from scene transition.");
    }

    // ── Public RPC entry points ───────────────────────────────────────────────

    /// <summary>
    /// Client requests that a cell be tilled.
    /// cellX / cellZ are grid coordinates (not world positions).
    ///
    /// Called from PlayerItemUser on the local client when the hoe animation
    /// reaches its impact frame and the cursor is over valid terrain.
    /// </summary>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void TryTillServerRpc(int cellX, int cellZ, RpcParams rpcParams = default)
    {
        ulong      senderId = rpcParams.Receive.SenderClientId;
        Vector2Int cell     = new Vector2Int(cellX, cellZ);

        // 1. Validate range — reject if the cell is too far from the sender's player
        if (!NetworkManager.ConnectedClients.TryGetValue(senderId, out var senderClient)
            || senderClient.PlayerObject == null)
            return;

        float playerY = senderClient.PlayerObject.transform.position.y;

        if (!ValidateRange(cell, senderId))
        {
            Debug.LogWarning($"[FarmingServerManager] Client {senderId} requested out-of-range cell {cell}. Rejected.");
            return;
        }

        // 2. Prevent double-tilling
        if (_tilledCells.ContainsKey(cell))
        {
            Debug.Log($"[FarmingServerManager] Cell {cell} is already tilled.");
            return;
        }

        // 3. Spawn tilled dirt — runs on server, replicates to all clients
        SpawnTilledDirt(cell, playerY);
    }

    // ── Server-only helpers ───────────────────────────────────────────────────

    private void SpawnTilledDirt(Vector2Int cell, float playerY)
    {
        if (tilledDirtPrefab == null)
        {
            Debug.LogError("[FarmingServerManager] tilledDirtPrefab is not assigned!");
            return;
        }

        Vector3 worldPos = CellToWorld(cell);
        // Use player's Y as fallback so the probe never falls back to Y=0
        worldPos.y = SampleGroundY(worldPos, fallbackY: playerY);

        NetworkObject spawned = Instantiate(tilledDirtPrefab, worldPos, Quaternion.identity);
        spawned.Spawn(destroyWithScene: true);

        _tilledCells[cell] = spawned;

        // Broadcast to all clients so their local cursor can check tilled state.
        NotifyTilledClientRpc(cell.x, cell.y);

        Debug.Log($"[FarmingServerManager] Tilled dirt spawned at cell {cell} → world {worldPos}");
    }

    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Client requests to plant a seed on a tilled cell.
    /// Guards in order: range → cell is tilled → cell is empty → seed data exists
    ///                  → player owns the seed → spawn visual.
    /// Seed consumption and visual spawn happen atomically on the server.
    /// </summary>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void TryPlantServerRpc(int cellX, int cellZ, int seedItemId, RpcParams rpcParams = default)
    {
        ulong      senderId = rpcParams.Receive.SenderClientId;
        Vector2Int cell     = new Vector2Int(cellX, cellZ);

        // 1. Resolve sender's player
        if (!NetworkManager.ConnectedClients.TryGetValue(senderId, out var senderClient)
            || senderClient.PlayerObject == null)
            return;

        float playerY = senderClient.PlayerObject.transform.position.y;

        // 2. Range check
        if (!ValidateRange(cell, senderId))
        {
            Debug.LogWarning($"[FarmingServerManager] Client {senderId} tried to plant out-of-range cell {cell}. Rejected.");
            return;
        }

        // 3. Cell must be tilled
        if (!_tilledCells.ContainsKey(cell))
        {
            Debug.Log($"[FarmingServerManager] Cell {cell} is not tilled. Cannot plant.");
            return;
        }

        // 4. Cell must be empty
        if (_plantedCells.ContainsKey(cell))
        {
            Debug.Log($"[FarmingServerManager] Cell {cell} already has a crop growing.");
            return;
        }

        // 5. Resolve seed data
        var itemData = GameDataManager.Instance.itemDatabases.GetItemByID(seedItemId);
        if (itemData is not SeedItemSO seed)
        {
            Debug.LogWarning($"[FarmingServerManager] Item {seedItemId} is not a SeedItemSO. Rejected.");
            return;
        }

        if (seed.stages == null || seed.stages.Length == 0 || seed.stages[0].visualPrefab == null)
        {
            Debug.LogError($"[FarmingServerManager] Seed '{seed.name}' has no growth stages or stage 0 prefab is null.");
            return;
        }

        // 6. Consume one seed from the sender's inventory
        if (!TryConsumeSeed(senderId, seedItemId))
        {
            Debug.LogWarning($"[FarmingServerManager] Client {senderId} does not have seed {seedItemId} in inventory.");
            return;
        }

        // 7. Spawn first growth stage visual above the dirt
        SpawnSeedVisual(cell, seed, playerY);
    }

    private void SpawnSeedVisual(Vector2Int cell, SeedItemSO seed, float playerY)
    {
        float groundY = SampleGroundY(CellToWorld(cell), playerY);

        // Spawn the stage-0 visual
        SpawnStageVisual(cell, seed.stages[0].visualPrefab, groundY);

        // Register crop growth state — updated every day-end by AdvanceCropsForNewDay()
        var cropData = new CropData
        {
            seedItemId         = seed.itemID,
            currentStage       = 0,
            daysInCurrentStage = 0,
            wateredThisDay     = false,
            isReadyToHarvest   = false,
            groundY            = groundY,
            hydration          = seed.droughtTolerance,
        };
        _cropData[cell] = cropData;

        // If the cell was pre-fertilized before planting, consume and apply now.
        if (_fertilizedCells.TryGetValue(cell, out int pendingFertDays))
        {
            _fertilizedCells.Remove(cell);
            NotifyFertilizerRemovedClientRpc(cell.x, cell.y);
            ApplyFertilizer(cell, cropData, seed, pendingFertDays);
            Debug.Log($"[FarmingServerManager] Pre-fertilizer ({pendingFertDays}d) consumed on plant at {cell}.");
        }

        // Broadcast to all clients so their local cursor can filter out occupied cells.
        NotifyPlantedClientRpc(cell.x, cell.y);

        Debug.Log($"[FarmingServerManager] Seed '{seed.name}' planted at cell {cell} (stage 0)");
    }

    /// <summary>
    /// Instantiates and spawns one stage visual at the given cell.
    /// Registers the new NetworkObject in <c>_plantedCells</c>, replacing any previous entry.
    /// Used for both the initial plant and stage-advance visuals.
    /// </summary>
    private void SpawnStageVisual(Vector2Int cell, GameObject prefab, float groundY)
    {
        Vector3 worldPos = CellToWorld(cell);
        worldPos.y = groundY + seedHeightOffset;

        var go         = Instantiate(prefab, worldPos, Quaternion.identity);
        var networkObj = go.GetComponent<NetworkObject>();

        if (networkObj == null)
        {
            Debug.LogError($"[FarmingServerManager] Prefab '{prefab.name}' is missing a NetworkObject — cannot replicate to clients.");
            Destroy(go);
            return;
        }

        networkObj.Spawn(destroyWithScene: true);
        _plantedCells[cell] = networkObj;
    }

    /// <summary>
    /// Finds and removes one instance of <paramref name="itemId"/> from any inventory
    /// owned by <paramref name="clientId"/>. Returns false if the player has none.
    /// </summary>
    private bool TryConsumeSeed(ulong clientId, int itemId)
    {
        var inventories = InventoryDataRegistry.GetAllByOwner(clientId);
        if (inventories == null) return false;

        foreach (var inv in inventories)
        {
            if (inv.GetItemCount(itemId) > 0)
            {
                inv.RemoveItem(itemId, 1);
                return true;
            }
        }

        return false;
    }

    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Client requests that a tilled cell be watered.
    /// Guards: range → cell is tilled.
    /// On success: despawns the dry dirt prefab and spawns the wet dirt prefab in its place.
    /// No seed-presence check — you can water a cell whether or not it has a crop.
    /// </summary>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void TryWaterServerRpc(int cellX, int cellZ, RpcParams rpcParams = default)
    {
        ulong      senderId = rpcParams.Receive.SenderClientId;
        Vector2Int cell     = new Vector2Int(cellX, cellZ);

        // 1. Resolve sender's player
        if (!NetworkManager.ConnectedClients.TryGetValue(senderId, out var senderClient)
            || senderClient.PlayerObject == null)
            return;

        float playerY = senderClient.PlayerObject.transform.position.y;

        // 2. Range check
        if (!ValidateRange(cell, senderId))
        {
            Debug.LogWarning($"[FarmingServerManager] Client {senderId} tried to water out-of-range cell {cell}. Rejected.");
            return;
        }

        // 3. Cell must be tilled
        if (!_tilledCells.ContainsKey(cell))
        {
            Debug.Log($"[FarmingServerManager] Cell {cell} is not tilled. Cannot water.");
            return;
        }

        // 4. Swap dry dirt → wet dirt
        SwapToWetDirt(cell, playerY);
    }

    private void SwapToWetDirt(Vector2Int cell, float playerY)
    {
        if (wetDirtPrefab == null)
        {
            Debug.LogError("[FarmingServerManager] wetDirtPrefab is not assigned!");
            return;
        }

        // Despawn the current dirt (dry or previously wet) — destroy: true removes it on all clients
        if (_tilledCells.TryGetValue(cell, out var oldDirt) && oldDirt != null)
            oldDirt.Despawn(destroy: true);

        // Spawn wet dirt at the same cell position
        Vector3 worldPos = CellToWorld(cell);
        worldPos.y = SampleGroundY(worldPos, playerY);

        NetworkObject spawned = Instantiate(wetDirtPrefab, worldPos, Quaternion.identity);
        spawned.Spawn(destroyWithScene: true);

        // Keep tracking the cell — it is still tilled, just now wet
        _tilledCells[cell] = spawned;
        _wetCells.Add(cell);

        // Mark the crop as watered for today's growth check (no-op if no crop is planted yet)
        if (_cropData.TryGetValue(cell, out var crop))
            crop.wateredThisDay = true;

        Debug.Log($"[FarmingServerManager] Cell {cell} watered → wet dirt at {worldPos}");
    }

    // ── Client-side tilled registry ───────────────────────────────────────────

    /// <summary>
    /// Called on every client (including host) when the server confirms a new tilled cell.
    /// Keeps _knownTilledCells in sync so the cursor can check locally.
    /// </summary>
    [ClientRpc]
    private void NotifyTilledClientRpc(int cellX, int cellZ)
    {
        _knownTilledCells.Add(new Vector2Int(cellX, cellZ));
    }

    /// <summary>
    /// Returns true if <paramref name="cell"/> has been tilled.
    /// Safe to call from any client — reads from the locally synced set.
    /// </summary>
    public bool IsTilled(Vector2Int cell) => _knownTilledCells.Contains(cell);

    /// <summary>
    /// Called on every client (including host) when the server confirms a seed was planted.
    /// Keeps _knownPlantedCells in sync so the cursor can filter out occupied cells.
    /// </summary>
    [ClientRpc]
    private void NotifyPlantedClientRpc(int cellX, int cellZ)
    {
        _knownPlantedCells.Add(new Vector2Int(cellX, cellZ));
    }

    /// <summary>
    /// Returns true if <paramref name="cell"/> currently has a crop/seed growing.
    /// Safe to call from any client — reads from the locally synced set.
    /// </summary>
    public bool IsPlanted(Vector2Int cell) => _knownPlantedCells.Contains(cell);

    /// <summary>
    /// Returns true if <paramref name="cell"/> has pending fertilizer waiting to be consumed.
    /// Safe to call from any client — reads from the locally synced set.
    /// </summary>
    public bool IsFertilized(Vector2Int cell) => _knownFertilizedCells.Contains(cell);

    // ── Fertilizer ────────────────────────────────────────────────────────────

    /// <summary>
    /// Client requests to apply fertilizer to a cell.
    /// Guards: range → cell is tilled → item is a FarmHelperItemSO with Fertilizer effect.
    /// If the cell already has a crop, the growth is accelerated immediately.
    /// If the cell is bare, the fertilizer is stored and consumed the moment a seed is planted.
    /// </summary>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void TryFertilizeServerRpc(int cellX, int cellZ, int itemId, RpcParams rpcParams = default)
    {
        ulong      senderId = rpcParams.Receive.SenderClientId;
        Vector2Int cell     = new Vector2Int(cellX, cellZ);

        // 1. Range check
        if (!ValidateRange(cell, senderId))
        {
            Debug.LogWarning($"[FarmingServerManager] Client {senderId} tried to fertilize out-of-range cell {cell}. Rejected.");
            return;
        }

        // 2. Cell must be tilled
        if (!_tilledCells.ContainsKey(cell))
        {
            Debug.Log($"[FarmingServerManager] Cell {cell} is not tilled. Cannot fertilize.");
            return;
        }

        // 3. Resolve item and confirm it is a Fertilizer-type FarmHelper
        var itemData = GameDataManager.Instance.itemDatabases.GetItemByID(itemId);
        if (itemData is not FarmHelperItemSO helper || helper.effectType != FarmHelperEffect.Fertilizer)
        {
            Debug.LogWarning($"[FarmingServerManager] Item {itemId} is not a Fertilizer FarmHelper. Rejected.");
            return;
        }

        int fertDays = Mathf.Max(1, Mathf.RoundToInt(helper.effectValue));

        // 4. Consume one fertilizer from the sender's inventory
        if (!TryConsumeFarmHelper(senderId, itemId))
        {
            Debug.LogWarning($"[FarmingServerManager] Client {senderId} does not have item {itemId} in inventory.");
            return;
        }

        // 5a. Crop already planted — apply growth acceleration immediately
        if (_cropData.TryGetValue(cell, out var crop))
        {
            var seedData = GameDataManager.Instance.itemDatabases.GetItemByID(crop.seedItemId) as SeedItemSO;
            if (seedData != null)
                ApplyFertilizer(cell, crop, seedData, fertDays);
        }
        else
        {
            // 5b. Bare tilled cell — store for when a seed is planted later.
            //     Stacks with any previously applied fertilizer on the same cell.
            _fertilizedCells[cell] = _fertilizedCells.TryGetValue(cell, out int prev) ? prev + fertDays : fertDays;
            NotifyFertilizedClientRpc(cellX, cellZ);
            Debug.Log($"[FarmingServerManager] Cell {cell} pre-fertilized ({_fertilizedCells[cell]}d pending).");
        }
    }

    /// <summary>
    /// Removes one of <paramref name="itemId"/> from any inventory owned by <paramref name="clientId"/>.
    /// Returns false if the player has none.
    /// </summary>
    private bool TryConsumeFarmHelper(ulong clientId, int itemId)
    {
        var inventories = InventoryDataRegistry.GetAllByOwner(clientId);
        if (inventories == null) return false;

        foreach (var inv in inventories)
        {
            if (inv.GetItemCount(itemId) > 0)
            {
                inv.RemoveItem(itemId, 1);
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Accelerates a crop's growth by <paramref name="fertDays"/> watered days.
    /// Deducts time stage-by-stage; any excess days carry into the next stage.
    /// If all stages are consumed the crop becomes immediately harvestable.
    /// Swaps the visual NetworkObject whenever the displayed stage changes.
    /// </summary>
    private void ApplyFertilizer(Vector2Int cell, CropData crop, SeedItemSO seed, int fertDays)
    {
        if (crop.isReadyToHarvest || fertDays <= 0) return;

        int remaining           = fertDays;
        int stageBeforeApply    = crop.currentStage;

        while (remaining > 0 && !crop.isReadyToHarvest)
        {
            GrowthStage stageData     = seed.stages[crop.currentStage];
            int         daysLeft      = stageData.daysToGrow - crop.daysInCurrentStage;

            if (remaining >= daysLeft)
            {
                // Entire remaining time in this stage is covered — skip it
                remaining -= daysLeft;
                crop.currentStage++;
                crop.daysInCurrentStage = 0;

                if (crop.currentStage >= seed.stages.Length)
                    crop.isReadyToHarvest = true;
            }
            else
            {
                // Partial advancement within the current stage
                crop.daysInCurrentStage += remaining;
                remaining = 0;
            }
        }

        // Determine which stage's visual to show:
        //   • harvest-ready → last stage's prefab
        //   • otherwise     → current stage's prefab
        int targetStage = crop.isReadyToHarvest ? seed.stages.Length - 1 : crop.currentStage;

        if (targetStage != stageBeforeApply)
        {
            if (_plantedCells.TryGetValue(cell, out var oldVisual) && oldVisual != null)
                oldVisual.Despawn(destroy: true);

            SpawnStageVisual(cell, seed.stages[targetStage].visualPrefab, crop.groundY);
        }

        if (crop.isReadyToHarvest)
            Debug.Log($"[FarmingServerManager] Fertilizer instantly matured crop at {cell}!");
        else
            Debug.Log($"[FarmingServerManager] Fertilizer advanced crop at {cell} → stage {crop.currentStage} ({crop.daysInCurrentStage}/{seed.stages[crop.currentStage].daysToGrow}d).");
    }

    [ClientRpc]
    private void NotifyFertilizedClientRpc(int cellX, int cellZ)
    {
        _knownFertilizedCells.Add(new Vector2Int(cellX, cellZ));
    }

    [ClientRpc]
    private void NotifyFertilizerRemovedClientRpc(int cellX, int cellZ)
    {
        _knownFertilizedCells.Remove(new Vector2Int(cellX, cellZ));
    }

    /// <summary>
    /// Despawns the crop visual and removes all server-side and client-side records
    /// for <paramref name="cell"/>. Called when a crop dies from drought.
    /// The tilled dirt remains — the player can plant again.
    /// </summary>
    private void KillCrop(Vector2Int cell)
    {
        if (_plantedCells.TryGetValue(cell, out var visual) && visual != null)
            visual.Despawn(destroy: true);

        _plantedCells.Remove(cell);
        _cropData.Remove(cell);

        // Remove from all clients' known-planted sets so the seed cursor shows
        // this cell as available again
        NotifyPlantedRemovedClientRpc(cell.x, cell.y);

        Debug.Log($"[FarmingServerManager] Crop at {cell} died from drought.");
    }

    /// <summary>
    /// Called on every client when a crop dies.
    /// Mirrors the removal from the server's _plantedCells into each client's
    /// _knownPlantedCells so IsPlanted() returns false and the seed cursor updates.
    /// </summary>
    [ClientRpc]
    private void NotifyPlantedRemovedClientRpc(int cellX, int cellZ)
    {
        _knownPlantedCells.Remove(new Vector2Int(cellX, cellZ));
    }

    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns true only if <paramref name="cell"/> is within maxCells of the
    /// requesting client's actual server-side player position.
    /// </summary>
    private bool ValidateRange(Vector2Int cell, ulong clientId)
    {
        if (!NetworkManager.ConnectedClients.TryGetValue(clientId, out var client))
            return false;

        NetworkObject playerObj = client.PlayerObject;
        if (playerObj == null) return false;

        Vector2Int playerCell = WorldToCell(playerObj.transform.position);

        int dx = Mathf.Abs(cell.x - playerCell.x);
        int dz = Mathf.Abs(cell.y - playerCell.y);

        return dx <= maxCells && dz <= maxCells;
    }

    // ── Harvesting ────────────────────────────────────────────────────────────

    /// <summary>
    /// Client requests to harvest the crop at a cell.
    /// Guards: range → crop exists → isReadyToHarvest.
    /// On success: gives yield items to the requesting player, despawns the visual,
    /// removes all crop records, and notifies all clients the cell is empty again.
    /// The tilled dirt stays — the player can replant immediately.
    /// </summary>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void TryHarvestServerRpc(int cellX, int cellZ, RpcParams rpcParams = default)
    {
        ulong      senderId = rpcParams.Receive.SenderClientId;
        Vector2Int cell     = new Vector2Int(cellX, cellZ);

        // 1. Range check
        if (!ValidateRange(cell, senderId))
        {
            Debug.LogWarning($"[FarmingServerManager] Client {senderId} tried to harvest out-of-range cell {cell}. Rejected.");
            return;
        }

        // 2. Must have a crop record here
        if (!_cropData.TryGetValue(cell, out var crop))
        {
            Debug.Log($"[FarmingServerManager] No crop at {cell}. Nothing to harvest.");
            return;
        }

        // 3. Must be fully grown
        if (!crop.isReadyToHarvest)
        {
            Debug.Log($"[FarmingServerManager] Crop at {cell} is not ready (stage {crop.currentStage}).");
            return;
        }

        // 4. Resolve seed data for harvest yield
        var itemData = GameDataManager.Instance.itemDatabases.GetItemByID(crop.seedItemId);
        if (itemData is not SeedItemSO seed)
        {
            Debug.LogWarning($"[FarmingServerManager] Harvest: seed ID {crop.seedItemId} not found. Aborting.");
            return;
        }

        // 5. Calculate yield and give items to the harvesting player
        if (seed.harvestItem != null)
        {
            int yield = UnityEngine.Random.Range(seed.yieldRange.x, seed.yieldRange.y + 1);
            if (yield > 0)
            {
                GiveItemToPlayer(senderId, seed.harvestItem.itemID, yield);

                // Notify only the harvesting client so their pickup popup can display.
                NotifyHarvestClientRpc(
                    seed.harvestItem.itemID,
                    yield,
                    new ClientRpcParams
                    {
                        Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { senderId } }
                    });
            }

            Debug.Log($"[FarmingServerManager] Client {senderId} harvested {yield}x '{seed.harvestItem.itemID}' from {cell}.");
        }

        // 6. Clean up — despawn visual, remove all records, notify clients
        KillCrop(cell);
    }

    /// <summary>
    /// Sent to the harvesting client only — fires the static OnHarvestNotification event
    /// so UI components can react without holding a direct reference to this manager.
    /// </summary>
    [ClientRpc]
    private void NotifyHarvestClientRpc(int itemId, int amount, ClientRpcParams rpcParams = default)
    {
        OnHarvestNotification?.Invoke(itemId, amount);
    }

    /// <summary>
    /// Adds <paramref name="amount"/> copies of <paramref name="itemId"/> to the main
    /// inventory (id = 0) owned by <paramref name="clientId"/>.
    /// Mirrors the logic in InventoryNetworkManager.RequestAddItemServerRpc, but called
    /// server-side so we pass the target clientId directly instead of reading SenderClientId.
    /// </summary>
    private void GiveItemToPlayer(ulong clientId, int itemId, int amount)
    {
        InventoryData inventory = InventoryDataRegistry.GetByOwnerAndID(clientId, 0);
        if (inventory == null)
        {
            Debug.LogWarning($"[FarmingServerManager] No main inventory (id=0) found for client {clientId} — harvest items lost.");
            return;
        }

        Debug.Log($"[FarmingServerManager] GiveItemToPlayer → client={clientId}  inv.OwnerClientId={inventory.OwnerClientId}  inv.InventoryID={inventory.InventoryID}  item={itemId} x{amount}");
        inventory.AddItem(itemId, amount);
    }

    // ── Day-end crop growth ───────────────────────────────────────────────────

    /// <summary>
    /// Called by GameDataManager.AdvanceDay() (server-side) when the day ends.
    /// Iterates every planted crop and advances it by one stage if:
    ///   1. The stage's watering requirement was satisfied today, AND
    ///   2. The crop has now spent enough days in this stage (daysToGrow).
    /// Resets wateredThisDay on all crops regardless of whether they advanced.
    /// </summary>
    public void AdvanceCropsForNewDay()
    {
        if (!IsServer) return;

        // Snapshot keys — modifying _plantedCells inside the loop is safe this way
        var cells = new List<Vector2Int>(_cropData.Keys);

        foreach (var cell in cells)
        {
            var crop = _cropData[cell];

            // ── Resolve seed data (needed for both hydration cap and growth) ──
            var itemData = GameDataManager.Instance.itemDatabases.GetItemByID(crop.seedItemId);
            if (itemData is not SeedItemSO seed)
            {
                Debug.LogWarning($"[FarmingServerManager] CropData at {cell} references unknown seed ID {crop.seedItemId}. Skipping.");
                continue;
            }

            // ── 1. Hydration update ───────────────────────────────────────────
            if (crop.wateredThisDay)
                crop.hydration = Mathf.Min(crop.hydration + 1, seed.droughtTolerance);
            else
                crop.hydration--;

            // ── 2. Death check ────────────────────────────────────────────────
            if (crop.hydration <= 0)
            {
                KillCrop(cell);
                continue;   // no further processing for this cell
            }

            // ── 3. Growth check (skip if already harvestable) ─────────────────
            if (!crop.isReadyToHarvest)
            {
                GrowthStage stageData = seed.stages[crop.currentStage];
                bool wateringMet = !stageData.requiresWater || crop.wateredThisDay;

                if (wateringMet)
                {
                    crop.daysInCurrentStage++;

                    if (crop.daysInCurrentStage >= stageData.daysToGrow)
                    {
                        // This stage is complete — move to the next one
                        crop.currentStage++;
                        crop.daysInCurrentStage = 0;

                        if (crop.currentStage >= seed.stages.Length)
                        {
                            // All stages done — ready to harvest, keep last visual
                            crop.isReadyToHarvest = true;
                            Debug.Log($"[FarmingServerManager] Crop at {cell} is fully grown and ready to harvest!");
                        }
                        else
                        {
                            // Swap to the next stage's visual
                            if (_plantedCells.TryGetValue(cell, out var oldVisual) && oldVisual != null)
                                oldVisual.Despawn(destroy: true);

                            SpawnStageVisual(cell, seed.stages[crop.currentStage].visualPrefab, crop.groundY);
                            Debug.Log($"[FarmingServerManager] Crop at {cell} → stage {crop.currentStage}.");
                        }
                    }
                }
                else
                {
                    Debug.Log($"[FarmingServerManager] Crop at {cell} was not watered — stage {crop.currentStage} did not progress.");
                }
            }

            // Always reset watered flag for the new day
            crop.wateredThisDay = false;
        }

        // Return all wet soil tiles to dry at the start of the new day
        ResetWetSoilToDry();
    }

    /// <summary>
    /// Despawns every wet-dirt NetworkObject and replaces it with a dry-dirt one.
    /// Only touches cells that were actually watered — skips dry tiles entirely.
    /// Called once per day-end, after crop growth has been evaluated.
    /// </summary>
    private void ResetWetSoilToDry()
    {
        if (tilledDirtPrefab == null)
        {
            Debug.LogError("[FarmingServerManager] tilledDirtPrefab is not assigned — cannot reset wet soil.");
            return;
        }

        foreach (var cell in _wetCells)
        {
            if (!_tilledCells.TryGetValue(cell, out var wetObj) || wetObj == null)
                continue;

            // Reuse the existing object's Y so we don't need another raycast
            float currentY = wetObj.transform.position.y;

            wetObj.Despawn(destroy: true);

            Vector3 worldPos = CellToWorld(cell);
            worldPos.y = currentY;

            var dryObj = Instantiate(tilledDirtPrefab, worldPos, Quaternion.identity);
            dryObj.Spawn(destroyWithScene: true);

            _tilledCells[cell] = dryObj;
        }

        int resetCount = _wetCells.Count;
        _wetCells.Clear();
        Debug.Log($"[FarmingServerManager] Reset {resetCount} wet soil tile(s) to dry.");
    }

    // ── Coordinate helpers ────────────────────────────────────────────────────

    private Vector2Int WorldToCell(Vector3 world)
        => new Vector2Int(
            Mathf.RoundToInt(world.x / cellSize),
            Mathf.RoundToInt(world.z / cellSize));

    private Vector3 CellToWorld(Vector2Int cell)
        => new Vector3(cell.x * cellSize, 0f, cell.y * cellSize);

    /// <summary>
    /// Samples terrain height at the given XZ position via a downward raycast.
    /// Probe fires from a high absolute Y (500) so it works regardless of the
    /// terrain platform's actual elevation.
    /// Falls back to the sender player's Y if the terrain layer is not found or missed.
    /// </summary>
    private float SampleGroundY(Vector3 origin, float fallbackY = 0f)
    {
        int terrainLayerIndex = LayerMask.NameToLayer("Terrain");
        if (terrainLayerIndex < 0)
        {
            Debug.LogWarning("[FarmingServerManager] 'Terrain' layer not found — using fallback Y.");
            return fallbackY;
        }

        LayerMask mask  = 1 << terrainLayerIndex;
        // Start from high above so the probe always starts above the terrain,
        // no matter how elevated the platform is.
        Vector3   probe = new Vector3(origin.x, 500f, origin.z);

        return Physics.Raycast(probe, Vector3.down, out RaycastHit hit, 1000f, mask)
            ? hit.point.y
            : fallbackY;
    }
}

// ── CropData ──────────────────────────────────────────────────────────────────

/// <summary>
/// Server-only record of a planted crop's growth state.
/// One instance exists per planted cell inside FarmingServerManager._cropData.
/// Never sent to clients directly — visual changes replicate via NetworkObject spawn/despawn.
/// </summary>
public class CropData
{
    /// <summary>Item ID of the SeedItemSO that was planted here.</summary>
    public int  seedItemId;

    /// <summary>Index into SeedItemSO.stages — which visual/rules are currently active.</summary>
    public int  currentStage;

    /// <summary>
    /// How many qualifying days (watering condition met) have accumulated in the current stage.
    /// Resets to 0 on each stage advance.
    /// </summary>
    public int  daysInCurrentStage;

    /// <summary>True if this cell received at least one watering action today.</summary>
    public bool wateredThisDay;

    /// <summary>
    /// Current drought stress level. Starts at SeedItemSO.droughtTolerance when planted.
    /// +1 on a watered day (capped at droughtTolerance), -1 on a dry day.
    /// Reaching 0 kills the crop.
    /// </summary>
    public int hydration;

    /// <summary>
    /// True when currentStage has advanced past the last entry in SeedItemSO.stages.
    /// The crop is now ready to be harvested and will not advance further.
    /// </summary>
    public bool isReadyToHarvest;

    /// <summary>
    /// Terrain surface Y recorded at planting time.
    /// Reused when swapping stage visuals so stage-advance spawns don't need a new raycast.
    /// </summary>
    public float groundY;
}

// ── Scene-transition farm snapshot data ───────────────────────────────────────

/// <summary>Snapshot of a single tilled cell for scene-transition bridging.</summary>
public class TileTransitionData
{
    public int   cellX;
    public int   cellZ;
    public float groundY;
    public bool  isWet;
    public bool  isFertilized;
    public int   fertilizedDays;
}

/// <summary>Snapshot of a single crop for scene-transition bridging.</summary>
public class CropTransitionData
{
    public int   cellX;
    public int   cellZ;
    public int   seedItemId;
    public int   currentStage;
    public int   daysInCurrentStage;
    public bool  wateredThisDay;
    public int   hydration;
    public bool  isReadyToHarvest;
    public float groundY;
}

/// <summary>
/// Full farm snapshot captured before a LoadSceneMode.Single transition.
/// Stored in GameDataManager keyed by scene name so visiting a non-farming
/// scene never overwrites the MainGame snapshot with empty data.
/// </summary>
public class FarmTransitionState
{
    public List<TileTransitionData> tilledCells  = new List<TileTransitionData>();
    public List<CropTransitionData> croppedCells = new List<CropTransitionData>();
}
