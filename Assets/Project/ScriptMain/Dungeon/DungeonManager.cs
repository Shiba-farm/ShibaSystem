// DungeonManager.cs
// Phase D — True Independent Floor Progression.
//
// Every active floor is a fully independent instance with its own physical slot
// (world-space origin).  Players progress through floors at their own pace while
// sharing each floor as a common space with anyone else currently on it.
//
// Key design rules
// ────────────────
// • FloorInstance        → data class holding everything about one active floor:
//                          slot, objects, waypoints, spawnPosition, players set.
// • _activeFloors        → Dictionary<int, FloorInstance>: floor# → instance.
//                          Created lazily on first entry; destroyed when last
//                          player leaves that floor.
// • _freeSlots           → Queue<int>: pool of available slot indices (0..MaxSlots-1).
//                          A slot is dequeued when a floor is created and enqueued
//                          back when it is destroyed.
// • Cosmetic tiles       → plain GameObjects, per-player, in player.SpawnedObjects.
//                          Only the HOST player ever has server-side tiles; non-hosts
//                          build them via ClientEnterFloor on their own machine.
// • Shared floor objects → NetworkObjects (ladder, rocks, ores, enemies), stored in
//                          FloorInstance.objects. Created once (first player on floor);
//                          destroyed when last player leaves.
// • Floor waypoints      → plain GameObjects in FloorInstance.waypointParent.
//                          Destroyed with the floor.
//
// EnterFloor() flow (server-only, affects ONLY the target player)
// ───────────────────────────────────────────────────────────────
//  1. Get or create FloorInstance (dequeue a slot from _freeSlots if new).
//  2. Move player tracking: remove from previous floor, add to new floor.
//  3. Update _slotAssignments[clientId] to floor's slot.
//  4. Update player save / FloorData.
//  5. Run DungeonGenerator (deterministic, seed-based).
//  6. Rebuild server-side cosmetic tiles for host only.
//  7. Lazy-generate shared floor objects (first player on this floor only).
//     Sets instance.isGenerated = true so subsequent players skip this.
//  8. Update player's netInstanceSlot to the floor's physical slot BEFORE
//     triggering the client's OnNetCurrentFloorChanged — so the client renders
//     tiles at the correct world-space origin.
//  9. Teleport only this player.
// 10. Call SetCurrentFloorNumber → triggers ClientEnterFloor on non-host client.
// 11. If previous floor is now empty → ClearFloorObjects (returns slot to pool).

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.AI.Navigation;
using Unity.Netcode;
using Unity.Behavior;

namespace MyGame.Dungeon
{
    [System.Serializable]
    public class DungeonInstanceSlot
    {
        [Tooltip("World-space root for this slot. Every grid position is offset from here.")]
        public Transform root;

        [Tooltip("Parent for cosmetic floor/wall tiles.")]
        public Transform tilesParent;

        [Tooltip("Parent for interactive server-spawned NetworkObjects: ladder, rocks, ores, enemies.")]
        public Transform objectsParent;

        [Tooltip("NavMeshSurface for this slot — rebaked when a floor is first generated here.")]
        public NavMeshSurface navMeshSurface;
    }

    /// <summary>
    /// All runtime state for one active floor instance.
    /// Allocated when the first player enters a floor; freed when the last player leaves.
    /// </summary>
    public class FloorInstance
    {
        /// <summary>Floor number (1-based).</summary>
        public int floorNumber;

        /// <summary>Index into DungeonManager.instances[] — determines world-space origin.</summary>
        public int slot;

        /// <summary>True once VisualizeFloorObjectsShared has run for this floor.</summary>
        public bool isGenerated;

        /// <summary>Shared NetworkObjects on this floor (ladder, rocks, ores, enemies).</summary>
        public readonly List<GameObject> objects = new();

        /// <summary>Parent GameObject that holds all AI roam waypoints for this floor.</summary>
        public GameObject waypointParent;

        /// <summary>Cached world-space player spawn position for this floor.</summary>
        public Vector3 spawnPosition;

        /// <summary>Client IDs of players currently on this floor.</summary>
        public readonly HashSet<ulong> players = new();
    }

    public class DungeonManager : NetworkBehaviour
    {
        public const int MaxSlots = 4;

        // ── Singleton ───────────────────────────────────────────────
        public static DungeonManager Instance { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            InitializeSlotPool();
        }

        // ── Inspector ────────────────────────────────────────────────
        [Header("Config")]
        public DungeonConfigSO config;

        [Header("Tile Size (meters)")]
        public float tileSize = 1f;

        [Header("Instance Slots (one per simultaneously active floor; set distinct root positions)")]
        public DungeonInstanceSlot[] instances = new DungeonInstanceSlot[MaxSlots];

        [Header("Dungeon Lighting")]
        [Tooltip("Name of the URP Rendering Layer to assign to all dungeon tiles and objects. " +
                 "Must match the name in Edit → Project Settings → Graphics → URP Global Settings → Rendering Layers. " +
                 "Leave empty to skip rendering-layer assignment.")]
        public string dungeonRenderingLayerName = "DungeonLayer";

        // ── Runtime (server-only) ────────────────────────────────────
        public int MaxFloors => 100;

        private int   _sharedMasterSeed;
        private ulong _primaryPlayerId = ulong.MaxValue;
        private readonly HashSet<ulong> _activeDungeonPlayers = new();

        // ── Slot Pool ────────────────────────────────────────────────
        // Available slot indices.  Dequeued when a floor is created;
        // enqueued back when a floor is destroyed.
        private readonly Queue<int> _freeSlots = new();

        private void InitializeSlotPool()
        {
            _freeSlots.Clear();
            for (int i = 0; i < MaxSlots; i++)
                _freeSlots.Enqueue(i);
        }

        // ── Active Floor Registry ────────────────────────────────────
        /// <summary>Maps floor number → its active FloorInstance. Server-only.</summary>
        private readonly Dictionary<int, FloorInstance> _activeFloors = new();

        // ── Player → Slot Mapping (API compatibility) ─────────────────
        // Updated whenever a player enters a floor.  Kept so external callers
        // of GetOrAssignSlot / ReleaseSlot continue to work unchanged.
        private readonly Dictionary<ulong, int> _slotAssignments = new();

        // ── Slot Assignment API ──────────────────────────────────────

        /// <summary>Returns the slot currently assigned to this player, or 0 as a fallback.</summary>
        public int GetOrAssignSlot(ulong clientId)
            => _slotAssignments.TryGetValue(clientId, out int slot) ? slot : 0;

        public void ReleaseSlot(ulong clientId) => _slotAssignments.Remove(clientId);

        private PlayerDungeonState GetPlayerState(ulong clientId)
        {
            if (NetworkManager.Singleton == null) return null;
            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client)) return null;
            return client.PlayerObject != null
                ? client.PlayerObject.GetComponent<PlayerDungeonState>() : null;
        }

        // ── Public API ────────────────────────────────────────────────

        /// <summary>
        /// Puts <paramref name="player"/> into the shared dungeon.
        /// First entrant (CREATE) generates the dungeon and sets the master seed.
        /// Subsequent entrants (JOIN) start at floor 1, reusing the shared seed
        /// so every player sees the same deterministic layout.
        /// Save-restores use <paramref name="existingSave"/> to resume from a saved floor.
        /// </summary>
        public void EnterDungeon(PlayerDungeonState player, DungeonSaveData existingSave = null)
        {
            if (!IsServer || player == null) return;

            // ── JOIN: dungeon already active, no save to restore ──
            bool joining = _activeDungeonPlayers.Count > 0 && existingSave == null;
            if (joining)
            {
                _activeDungeonPlayers.Add(player.OwnerClientId);

                var joinSave = new DungeonSaveData
                {
                    masterSeed          = _sharedMasterSeed,
                    currentFloor        = 1,
                    deepestFloorReached = 1
                };

                // Pass slot 0 as a placeholder; EnterFloor will set the real slot.
                player.SetDungeonRuntimeState(joinSave, 1, true, 0);
                StartCoroutine(TransitionToNextFloor(player, 1));

                Debug.Log($"[Dungeon] Client {player.OwnerClientId} joined shared dungeon → floor 1.");
                return;
            }

            // ── CREATE / restore ──
            var save = existingSave ?? player.DungeonSave ?? new DungeonSaveData
            {
                masterSeed = UnityEngine.Random.Range(int.MinValue, int.MaxValue)
            };

            int floor = Mathf.Max(1, save.currentFloor);

            _sharedMasterSeed = save.masterSeed;
            _primaryPlayerId  = player.OwnerClientId;
            _activeDungeonPlayers.Add(player.OwnerClientId);

            Debug.Log($"[Dungeon] Client {player.OwnerClientId} creating shared dungeon → floor {floor}.");

            // Pass slot 0 as placeholder; EnterFloor will set the real slot.
            player.SetDungeonRuntimeState(save, floor, true, 0);
            StartCoroutine(TransitionToNextFloor(player, floor));
        }

        /// <summary>
        /// Enters <paramref name="floor"/> for <paramref name="player"/> only.
        /// Each floor is a fully independent physical instance (its own slot / world-space origin).
        /// Other players on other floors are never modified.
        /// If the floor already exists (another player is there), its shared objects are reused.
        /// If the floor does not exist yet, it is created and assigned a fresh slot.
        /// </summary>
        public void EnterFloor(PlayerDungeonState player, int floor)
        {
            if (!IsServer || player == null) return;
            if (player.DungeonSave == null) return;

            if (floor > MaxFloors)
            {
                Debug.Log($"[Dungeon] Client {player.OwnerClientId} cleared all {MaxFloors} floors!");
                ExitDungeon(player);
                return;
            }

            // ── 1. Get or create FloorInstance ──────────────────────
            if (!_activeFloors.TryGetValue(floor, out var instance))
            {
                if (!_freeSlots.TryDequeue(out int newSlot))
                {
                    Debug.LogError($"[Dungeon] No free slots for floor {floor}. " +
                                   $"Max simultaneous active floors = {MaxSlots}.");
                    return;
                }
                instance = new FloorInstance { floorNumber = floor, slot = newSlot };
                _activeFloors[floor] = instance;
                Debug.Log($"[Dungeon] Floor {floor} allocated → slot {newSlot}.");
            }

            // ── 2. Update per-floor player tracking ─────────────────
            int previousFloor = player.CurrentFloor;   // 0 on first entry
            if (previousFloor > 0 && _activeFloors.TryGetValue(previousFloor, out var prevInstance))
                prevInstance.players.Remove(player.OwnerClientId);

            instance.players.Add(player.OwnerClientId);

            // ── 3. Update player → slot mapping ─────────────────────
            _slotAssignments[player.OwnerClientId] = instance.slot;

            // ── 4. Update player save state ──────────────────────────
            var save = player.DungeonSave;
            save.currentFloor = floor;
            if (floor > save.deepestFloorReached) save.deepestFloorReached = floor;

            int seed      = GenerateSeedForFloor(save.masterSeed, floor);
            var floorData = save.GetOrCreate(floor, seed);
            player.SetCurrentFloorData(floorData);

            // ── 5. Rebuild generator (deterministic from seed) ───────
            var gen = new DungeonGenerator(config, floor, seed);
            gen.Generate();

            // ── 6. Server-side cosmetic tiles: host player only ──────
            // Non-host clients build their own tiles via ClientEnterFloor.
            ClearPlayerTiles(player);
            if (player.OwnerClientId == NetworkManager.ServerClientId)
                VisualizeFloorTiles(gen, player, instance.slot);

            // ── 7. Lazy-generate shared floor objects ────────────────
            // isGenerated is set once (first player on this floor) so subsequent
            // players entering the same floor reuse existing NetworkObjects.
            if (!instance.isGenerated)
            {
                instance.isGenerated = true;

                VisualizeFloorObjectsShared(gen, player, instance);

                instance.spawnPosition =
                    GridToWorld(gen.PlayerSpawnPos, instance.slot)
                    + new Vector3(0f, config.objectYOffset + 0.1f, 0f);

                StartCoroutine(BakeThenSpawnEnemies(gen, player, instance));
            }

            // ── 8. Update netInstanceSlot BEFORE triggering the floor-change event ─
            // The client's OnNetCurrentFloorChanged reads netInstanceSlot to know
            // which world-space origin to use for tile rendering.
            player.SetInstanceSlot(instance.slot);

            // ── 9. Teleport only this player ─────────────────────────
            player.TeleportOwnerRpc(instance.spawnPosition, Quaternion.identity);

            // SetCurrentFloorNumber → triggers OnNetCurrentFloorChanged → ClientEnterFloor.
            player.SetCurrentFloorNumber(floor);

            // ── 10. Clean up previous floor if now empty ─────────────
            if (previousFloor > 0)
            {
                bool prevEmpty = !_activeFloors.TryGetValue(previousFloor, out var prev)
                                 || prev.players.Count == 0;
                if (prevEmpty) ClearFloorObjects(previousFloor);
            }
        }

        /// <summary>Advances <paramref name="player"/> to their next floor independently of all other players.</summary>
        public void GoNextFloor(PlayerDungeonState player)
        {
            if (!IsServer || player == null) return;
            if (!player.IsInDungeon || player.CurrentFloorData == null) return;

            player.CurrentFloorData.cleared = true;
            StartCoroutine(TransitionToNextFloor(player, player.CurrentFloor + 1));
        }

        /// <summary>Removes <paramref name="player"/> from the dungeon and returns them to their entry position.</summary>
        public void ExitDungeon(PlayerDungeonState player)
        {
            if (!IsServer || player == null) return;

            // Remove from floor tracking; destroy floor if now empty.
            int currentFloor = player.CurrentFloor;
            if (currentFloor > 0 && _activeFloors.TryGetValue(currentFloor, out var instance))
            {
                instance.players.Remove(player.OwnerClientId);
                if (instance.players.Count == 0) ClearFloorObjects(currentFloor);
            }

            _activeDungeonPlayers.Remove(player.OwnerClientId);
            ReleaseSlot(player.OwnerClientId);
            player.SetInDungeon(false);

            if (player.HasReturnPosition)
                player.TeleportOwnerRpc(player.ReturnPosition, player.ReturnRotation);

            ClearPlayerTiles(player);

            // Last player out: destroy any remaining floors and reset the slot pool.
            if (_activeDungeonPlayers.Count == 0)
            {
                _primaryPlayerId = ulong.MaxValue;

                foreach (var kv in _activeFloors)
                {
                    foreach (var go in kv.Value.objects)
                        if (go) Destroy(go);
                    if (kv.Value.waypointParent != null) Destroy(kv.Value.waypointParent);
                }
                _activeFloors.Clear();

                InitializeSlotPool();
            }
        }

        // ── Coroutines ────────────────────────────────────────────────

        private IEnumerator BakeThenSpawnEnemies(DungeonGenerator gen, PlayerDungeonState player, FloorInstance instance)
        {
            yield return null;

            var navMesh = (instance.slot >= 0 && instance.slot < instances.Length)
                          ? instances[instance.slot]?.navMeshSurface : null;
            if (navMesh != null) navMesh.BuildNavMesh();

            yield return null;

            GenerateFloorWaypoints(gen, instance);
            SpawnEnemies(gen, player, instance);
        }

        /// <summary>
        /// Fades the screen, enters the floor, then restores the screen.
        /// Host players use the local DungeonFloorTransition singleton directly.
        /// Non-host players receive BeginFloorFadeRpc / CompleteFloorTransitionRpc
        /// so they see an identical animation on their own screen.
        /// </summary>
        private IEnumerator TransitionToNextFloor(PlayerDungeonState player, int nextFloor)
        {
            bool isHostPlayer = player.OwnerClientId == NetworkManager.ServerClientId;
            float fadeDuration = (IsHost && DungeonFloorTransition.Instance != null)
                                 ? DungeonFloorTransition.Instance.fadeDuration : 0.4f;

            if (isHostPlayer)
            {
                var trans = DungeonFloorTransition.Instance;
                if (trans != null) yield return StartCoroutine(trans.FadeIn());

                EnterFloor(player, nextFloor);

                yield return null;
                yield return null;
                yield return null;

                if (trans != null)
                {
                    trans.ShowFloorText(nextFloor);
                    yield return new WaitForSeconds(trans.holdDuration);
                    yield return StartCoroutine(trans.FadeOut());
                }
            }
            else
            {
                // Blind the client during the teleport.
                player.BeginFloorFadeRpc();
                yield return new WaitForSeconds(fadeDuration + 0.05f);

                EnterFloor(player, nextFloor);

                yield return null;
                yield return null;
                yield return null;

                // Show floor number, then fade back in.
                player.CompleteFloorTransitionRpc(nextFloor);
            }
        }

        // ── Cleanup Helpers ───────────────────────────────────────────

        /// <summary>
        /// Destroys this player's server-side cosmetic tiles.
        /// For non-host players this is always a no-op (tiles only exist on their own client).
        /// </summary>
        private void ClearPlayerTiles(PlayerDungeonState player)
        {
            foreach (var go in player.SpawnedObjects)
                if (go) Destroy(go);
            player.SpawnedObjects.Clear();

            if (player.WaypointParent != null)
            {
                Destroy(player.WaypointParent);
                player.WaypointParent = null;
            }
        }

        /// <summary>
        /// Destroys all shared NetworkObjects, waypoints, and tracking data for <paramref name="floor"/>.
        /// Returns the slot to the pool so it can be reused by a future floor.
        /// Called when the last player leaves that floor.
        /// </summary>
        private void ClearFloorObjects(int floor)
        {
            if (!_activeFloors.TryGetValue(floor, out var instance)) return;

            foreach (var go in instance.objects)
                if (go) Destroy(go);

            if (instance.waypointParent != null) Destroy(instance.waypointParent);

            // Return slot to pool for reuse.
            _freeSlots.Enqueue(instance.slot);

            _activeFloors.Remove(floor);

            Debug.Log($"[Dungeon] Floor {floor} destroyed (no remaining players). " +
                      $"Slot {instance.slot} returned to pool. " +
                      $"Free slots: {_freeSlots.Count}/{MaxSlots}.");
        }

        /// <summary>
        /// Full tile cleanup for <paramref name="player"/>.
        /// Called by PlayerDungeonState.OnNetworkDespawn and ClientExitDungeon.
        /// Safe to call any time — only touches player.SpawnedObjects (never shared floor objects).
        /// </summary>
        public void ClearFloor(PlayerDungeonState player)
        {
            foreach (var go in player.SpawnedObjects)
                if (go) Destroy(go);
            player.SpawnedObjects.Clear();

            if (player.WaypointParent != null)
            {
                Destroy(player.WaypointParent);
                player.WaypointParent = null;
            }
        }

        // ── Visualization ─────────────────────────────────────────────

        private void GenerateFloorWaypoints(DungeonGenerator gen, FloorInstance instance)
        {
            if (instance.waypointParent != null) Destroy(instance.waypointParent);

            var parent = new GameObject($"SlimeWaypoints_Floor{instance.floorNumber}");

            var floorTiles = new List<Vector2Int>();
            for (int x = 0; x < config.gridWidth; x++)
                for (int y = 0; y < config.gridHeight; y++)
                    if (gen.Grid[x, y] == DungeonTileType.Floor)
                        floorTiles.Add(new Vector2Int(x, y));

            int count = Mathf.Min(20, floorTiles.Count);
            for (int i = 0; i < count; i++)
            {
                int idx     = UnityEngine.Random.Range(0, floorTiles.Count);
                var worldPos = GridToWorld(floorTiles[idx], instance.slot)
                               + new Vector3(0f, config.objectYOffset, 0f);
                var wp = new GameObject($"WP_{i}");
                wp.transform.position = worldPos;
                wp.transform.SetParent(parent.transform);
                floorTiles.RemoveAt(idx);
            }

            instance.waypointParent = parent;
        }

        // Cosmetic tiles + walls.  Runs on the server for host visuals and on
        // the client via ClientEnterFloor.  No NetworkObjects are created here.
        private void VisualizeFloorTiles(DungeonGenerator gen, PlayerDungeonState player, int slot)
        {
            int w = config.gridWidth, h = config.gridHeight;
            var ts         = config.tileSet;
            var tilesParent = (slot >= 0 && slot < instances.Length) ? instances[slot]?.tilesParent : null;

            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                {
                    if (gen.Grid[x, y] != DungeonTileType.Floor) continue;

                    if (gen.IsRoomTile[x, y])
                    {
                        var roomPrefab = ts?.prefabRoomFloor ?? ts?.prefabOpen;
                        if (roomPrefab != null)
                            SpawnTile(roomPrefab, new Vector2Int(x, y), 0f, tilesParent, player, slot);
                        continue;
                    }

                    int mask = ComputeNeighborMask(gen.Grid, x, y, w, h);
                    var (prefab, rotY) = ts != null ? ts.GetTileForMask(mask) : (null, 0f);
                    if (prefab == null) continue;
                    SpawnTile(prefab, new Vector2Int(x, y), rotY, tilesParent, player, slot);
                }

            if (ts?.prefabWall != null)
            {
                var dirs = new (int dx, int dz, float rotY)[]
                {
                    ( 0,  1,   0f), ( 1,  0,  90f), ( 0, -1, 180f), (-1,  0, 270f),
                };

                for (int x = 0; x < w; x++)
                    for (int y = 0; y < h; y++)
                    {
                        if (gen.Grid[x, y] != DungeonTileType.Floor) continue;
                        foreach (var (dx, dz, rotY) in dirs)
                        {
                            int nx = x + dx, ny = y + dz;
                            if (nx < 0 || ny < 0 || nx >= w || ny >= h ||
                                gen.Grid[nx, ny] == DungeonTileType.Wall)
                            {
                                Vector3 wallPos = GridToWorld(new Vector2Int(x, y), slot)
                                                  + new Vector3(dx * tileSize * 0.5f, 0f, dz * tileSize * 0.5f);
                                var go = Instantiate(ts.prefabWall, wallPos,
                                                     Quaternion.Euler(0f, rotY, 0f), tilesParent);
                                float wallModelW = ts.wallModelWidth > 0f ? ts.wallModelWidth : 1f;
                                var s = go.transform.localScale;
                                go.transform.localScale = new Vector3(s.x * tileSize / wallModelW, s.y, s.z);
                                player.SpawnedObjects.Add(go);
                            }
                        }
                    }
            }
        }

        /// <summary>
        /// Spawns shared NetworkObjects (ladder, rocks, ores) for <paramref name="instance"/>.
        /// Results go into <c>instance.objects</c> — NOT into player.SpawnedObjects —
        /// so they survive individual player floor transitions and are shared by all
        /// players on that floor.
        /// </summary>
        private void VisualizeFloorObjectsShared(DungeonGenerator gen, PlayerDungeonState player, FloorInstance instance)
        {
            float yOff        = config.objectYOffset;
            var objectsParent = (instance.slot >= 0 && instance.slot < instances.Length)
                                ? instances[instance.slot]?.objectsParent : null;

            if (config.ladderPrefab)
                SpawnNetworkObject(config.ladderPrefab, gen.LadderPos, 0f, yOff, objectsParent, player, instance);

            foreach (var pos in gen.Rocks)
            {
                if (player.CurrentFloorData.IsRockBroken(pos)) continue;
                if (config.rockPrefab)
                    SpawnNetworkObject(config.rockPrefab, pos, 0f, yOff, objectsParent, player, instance);
            }

            foreach (var (pos, ore) in gen.Ores)
            {
                if (player.CurrentFloorData.IsOreHarvested(pos)) continue;
                if (ore.prefab == null) continue;

                var go = SpawnNetworkObject(ore.prefab, pos, 0f, yOff, objectsParent, player, instance);
                if (go == null) continue;

                var oreNode = go.GetComponent<DungeonOreNode>();
                oreNode?.Setup(ore, pos, this, player.OwnerClientId);
            }
        }

        private void SpawnEnemies(DungeonGenerator gen, PlayerDungeonState player, FloorInstance instance)
        {
            float yOff        = config.objectYOffset;
            var objectsParent = (instance.slot >= 0 && instance.slot < instances.Length)
                                ? instances[instance.slot]?.objectsParent : null;

            var waypointList = new List<GameObject>();
            if (instance.waypointParent != null)
                foreach (Transform child in instance.waypointParent.transform)
                    waypointList.Add(child.gameObject);

            foreach (var (pos, enemy) in gen.Enemies)
            {
                if (player.CurrentFloorData.IsEnemyKilled(pos)) continue;
                if (enemy.prefab == null) continue;

                var go = SpawnNetworkObject(enemy.prefab, pos, 0f, yOff, objectsParent, player, instance);
                if (go == null) continue;

                var behaviorAgent = go.GetComponent<BehaviorGraphAgent>();
                if (behaviorAgent != null && waypointList.Count > 0)
                    behaviorAgent.BlackboardReference.SetVariableValue("WayPoints", waypointList);
            }
        }

        // ── Client-side sync ──────────────────────────────────────────

        /// <summary>Builds local cosmetic tiles for the client. No NetworkObjects spawned.</summary>
        public void ClientEnterFloor(PlayerDungeonState player, int floor, int masterSeed, int slot)
        {
            if (floor <= 0 || slot < 0) return;

            ClearFloor(player);

            int seed = GenerateSeedForFloor(masterSeed, floor);
            var gen  = new DungeonGenerator(config, floor, seed);
            gen.Generate();

            VisualizeFloorTiles(gen, player, slot);
        }

        public void ClientExitDungeon(PlayerDungeonState player) => ClearFloor(player);

        // ── Dungeon Object Callbacks ───────────────────────────────────

        public void OnOreHarvested(Vector2Int pos, ulong ownerClientId) => GetPlayerState(ownerClientId)?.CurrentFloorData?.MarkOreHarvested(pos);
        public void OnEnemyKilled(Vector2Int pos, ulong ownerClientId)  => GetPlayerState(ownerClientId)?.CurrentFloorData?.MarkEnemyKilled(pos);
        public void OnRockBroken(Vector2Int pos, ulong ownerClientId)   => GetPlayerState(ownerClientId)?.CurrentFloorData?.MarkRockBroken(pos);

        // ── Tile Helpers ──────────────────────────────────────────────

        private int ComputeNeighborMask(DungeonTileType[,] grid, int x, int y, int w, int h)
        {
            int mask = 0;
            if (IsFloor(grid, x,     y + 1, w, h)) mask |= 1;
            if (IsFloor(grid, x + 1, y,     w, h)) mask |= 2;
            if (IsFloor(grid, x,     y - 1, w, h)) mask |= 4;
            if (IsFloor(grid, x - 1, y,     w, h)) mask |= 8;
            return mask;
        }

        private bool IsFloor(DungeonTileType[,] grid, int x, int y, int w, int h)
        {
            if (x < 0 || y < 0 || x >= w || y >= h) return false;
            return grid[x, y] == DungeonTileType.Floor;
        }

        // ── Spawn Helpers ─────────────────────────────────────────────

        // Cosmetic tile — tracked per-player for cleanup.
        private GameObject SpawnTile(GameObject prefab, Vector2Int gridPos, float rotY,
                                     Transform parent, PlayerDungeonState player, int slot)
        {
            var go = Instantiate(prefab, GridToWorld(gridPos, slot),
                                 Quaternion.Euler(0f, rotY, 0f), parent);
            SetDungeonRenderingLayer(go);
            player.SpawnedObjects.Add(go);
            return go;
        }

        // NetworkObject — tracked per-floor (FloorInstance.objects) so it outlives
        // individual player floor transitions and is shared by all players on the floor.
        private GameObject SpawnNetworkObject(GameObject prefab, Vector2Int gridPos, float rotY,
                                              float yOffset, Transform parent,
                                              PlayerDungeonState player, FloorInstance instance)
        {
            if (!IsServer) return null;

            var go = Instantiate(prefab,
                                 GridToWorld(gridPos, instance.slot) + new Vector3(0f, yOffset, 0f),
                                 Quaternion.Euler(0f, rotY, 0f),
                                 parent);

            var member = go.AddComponent<DungeonInstanceMember>();
            member.slot          = instance.slot;
            member.ownerClientId = player.OwnerClientId;

            SetDungeonRenderingLayer(go);

            var netObj = go.GetComponent<NetworkObject>();
            if (netObj != null)
                netObj.Spawn(true);
            else
                Debug.LogWarning($"[DungeonManager] {prefab.name} has no NetworkObject — plain Instantiate used.");

            instance.objects.Add(go);
            return go;
        }

        // ── Rendering Layer Helper ────────────────────────────────────

        /// <summary>
        /// Recursively sets the URP Rendering Layer on all Renderers under
        /// <paramref name="go"/> to <see cref="dungeonRenderingLayerName"/>.
        /// Called after every tile and NetworkObject spawn so dungeon geometry
        /// is only illuminated by dungeon-specific lights (torches, dim ambient)
        /// and not by the farm Directional Light (Sun).
        /// No-op if dungeonRenderingLayerName is empty or the layer is not
        /// registered in URP Global Settings.
        /// </summary>
        private void SetDungeonRenderingLayer(GameObject go)
        {
            if (go == null || string.IsNullOrEmpty(dungeonRenderingLayerName)) return;

            uint mask = RenderingLayerMask.GetMask(dungeonRenderingLayerName);
            if (mask == 0) return; // layer name not found — silent no-op

            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                r.renderingLayerMask = mask;
        }

        // ── Utility ───────────────────────────────────────────────────

        public Vector3 GridToWorld(Vector2Int grid, int slot)
        {
            Vector3 origin = (slot >= 0 && slot < instances.Length && instances[slot]?.root != null)
                             ? instances[slot].root.position : Vector3.zero;
            return origin + new Vector3(grid.x * tileSize, 0f, grid.y * tileSize);
        }

        public Vector2Int WorldToGrid(Vector3 world, int slot)
        {
            Vector3 origin = (slot >= 0 && slot < instances.Length && instances[slot]?.root != null)
                             ? instances[slot].root.position : Vector3.zero;
            Vector3 local  = world - origin;
            return new Vector2Int(
                Mathf.RoundToInt(local.x / tileSize),
                Mathf.RoundToInt(local.z / tileSize));
        }

        private static int GenerateSeedForFloor(int masterSeed, int floor)
        {
            unchecked
            {
                int h = masterSeed;
                h = h * 31 + floor;
                h = h ^ (h >> 16);
                return h;
            }
        }
    }
}
