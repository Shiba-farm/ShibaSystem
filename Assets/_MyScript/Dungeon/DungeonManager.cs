// DungeonManager.cs
// Singleton ควบคุม Dungeon ทั้งหมด — Enter, Generate, Visualize, Save

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.AI.Navigation;
using Unity.Netcode;
using Unity.Behavior;

namespace MyGame.Dungeon
{
    public class DungeonManager : NetworkSaveableBehaviour
    {
        // ── Singleton ─────────────────────────────────────────
        public static DungeonManager Instance { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        // ── ISaveable ─────────────────────────────────────────
        public override bool IsPlayerSaveable => false;

        public override void CaptureState(GameSaveData save, ulong clientId = 0)
        {
            if (!IsInDungeon) return;
            save.dungeon = _saveData;
        }

        public override void RestoreState(GameSaveData save, ulong clientId = 0)
        {
            if (save.dungeon == null) return;
            Debug.Log($"[DungeonManager] Restoring dungeon state... : floor = {save.dungeon.currentFloor}, deepest = {save.dungeon.deepestFloorReached}");
            EnterDungeon(save.dungeon);
        }

        private GameObject _waypointParent;

        // ── Inspector ─────────────────────────────────────────
        [Header("Config")]
        public DungeonConfigSO config;

        [Header("Tile Size (meters)")]
        public float tileSize = 1f;

        [Header("Parents")]
        public Transform tilesParent;
        public Transform objectsParent;

        [Header("NavMesh")]
        public NavMeshSurface navMeshSurface;

        // ── Runtime State ─────────────────────────────────────
        public int CurrentFloor { get; private set; } = 1;
        public int MaxFloors => 100;
        public bool IsInDungeon { get; private set; } = false;

        private DungeonSaveData _saveData;
        private DungeonFloorData _currentFloorData;
        private readonly List<GameObject> _spawnedObjects = new();

        // ── Events ────────────────────────────────────────────
        public static event Action<int> OnFloorEntered;
        public static event Action<int> OnFloorCleared;

        // ── Public API ────────────────────────────────────────

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn(); // handles Register

            if (!IsServer) return;

            // SaveLoadManager already ran RestoreState before we registered,
            // so we need to restore ourselves manually on spawn
            if (SaveLoadManager.Instance != null)
            {
                // Ask SaveLoadManager for the already-loaded save data
                var save = SaveLoadManager.Instance.GetLoadedSave();
                if (save?.dungeon != null)
                {
                    Debug.Log("[DungeonManager] Late restore from loaded save.");
                    RestoreState(save);
                    return;
                }
            }

            // No save data = new game, enter fresh dungeon
            EnterDungeon();
        }

        public void EnterDungeon(DungeonSaveData existingSave = null)
        {
            _saveData = existingSave ?? new DungeonSaveData
            {
                masterSeed = UnityEngine.Random.Range(int.MinValue, int.MaxValue)
            };
            Debug.Log($"[Dungeon] Entering dungeon... : existing save = {existingSave != null}, seed = {existingSave?.masterSeed}");

            IsInDungeon = true;
            CurrentFloor = Mathf.Max(1, _saveData.currentFloor);
            EnterFloor(CurrentFloor);
        }

        public void EnterFloor(int floor)
        {
            if (floor > MaxFloors)
            {
                Debug.Log("[Dungeon] Cleared all 100 floors!");
                ExitDungeon();
                return;
            }

            CurrentFloor = floor;
            _saveData.currentFloor = floor;
            if (floor > _saveData.deepestFloorReached)
                _saveData.deepestFloorReached = floor;

            int seed = GenerateSeedForFloor(floor);
            _currentFloorData = _saveData.GetOrCreate(floor, seed);

            ClearFloor();

            var gen = new DungeonGenerator(config, floor, seed);
            gen.Generate();

            VisualizeFloorWithoutEnemies(gen);

            Vector3 spawnPos = GridToWorld(gen.PlayerSpawnPos)
                + new Vector3(0f, config.objectYOffset + 0.1f, 0f);

            SpawnPointManager.Instance?.SetDungeonSpawn(spawnPos, Quaternion.identity);
            MovePlayersToSpawn(gen);
            StartCoroutine(BakeThenSpawnEnemies(gen));
            OnFloorEntered?.Invoke(floor);
        }

        public void GoNextFloor()
        {
            _currentFloorData.cleared = true;
            OnFloorCleared?.Invoke(CurrentFloor);
            StartCoroutine(TransitionToNextFloor(CurrentFloor + 1));
        }

        public void ExitDungeon()
        {
            ClearFloor();
            IsInDungeon = false;
            SpawnPointManager.Instance?.ClearDungeonOverride();
        }

        // ── Player Spawning ───────────────────────────────────

        private void MovePlayersToSpawn(DungeonGenerator gen)
        {
            if (!IsServer) return;

            foreach (var client in NetworkManager.ConnectedClientsList)
            {
                var playerObj = client.PlayerObject;
                if (playerObj == null) continue;

                Vector3 pos;
                Quaternion rot;

                if (SpawnPointManager.Instance != null)
                {
                    pos = SpawnPointManager.Instance.GetNextPosition();
                    rot = SpawnPointManager.Instance.GetNextRotation();
                }
                else
                {
                    pos = GridToWorld(gen.PlayerSpawnPos) + new Vector3(0f, config.objectYOffset, 0f);
                    rot = Quaternion.identity;
                }

                TeleportPlayerClientRpc(playerObj.GetComponent<NetworkObject>().NetworkObjectId, pos, rot);
            }
        }

        [ClientRpc]
        private void TeleportPlayerClientRpc(ulong networkObjectId, Vector3 position, Quaternion rotation)
        {
            if (NetworkManager.SpawnManager.SpawnedObjects
                .TryGetValue(networkObjectId, out var netObj))
            {
                netObj.transform.SetPositionAndRotation(position, rotation);

                var rb = netObj.GetComponent<Rigidbody>();
                if (rb != null)
                    StartCoroutine(FreezeAndRelease(rb));
            }
        }

        private IEnumerator FreezeAndRelease(Rigidbody rb)
        {
            var original = rb.constraints;
            rb.constraints = RigidbodyConstraints.FreezeAll;
            rb.linearVelocity = Vector3.zero;
            yield return null;
            yield return null;
            rb.constraints = original;
        }

        // ── Coroutines ────────────────────────────────────────

        private IEnumerator BakeThenSpawnEnemies(DungeonGenerator gen)
        {
            yield return null;
            Debug.Log("[Dungeon] Building NavMesh...");
            navMeshSurface?.BuildNavMesh();
            Debug.Log("[Dungeon] NavMesh built.");
            yield return null;

            GenerateRoamWaypoints(gen);
            Debug.Log($"[Dungeon] Generated waypoints: {_waypointParent?.transform.childCount}");
            SpawnEnemies(gen);
        }

        private IEnumerator TransitionToNextFloor(int nextFloor)
        {
            var trans = DungeonFloorTransition.Instance;

            if (trans != null) yield return StartCoroutine(trans.FadeIn());

            EnterFloor(nextFloor);

            yield return null;
            yield return null;
            yield return null;

            if (trans != null)
            {
                trans.ShowFloorText(nextFloor);
                yield return new WaitForSeconds(trans.holdDuration);
            }
            else yield return new WaitForSeconds(1.2f);

            if (trans != null) yield return StartCoroutine(trans.FadeOut());
        }

        // ── Visualization ─────────────────────────────────────

        public void ClearFloor()
        {
            foreach (var go in _spawnedObjects)
                if (go) Destroy(go);
            _spawnedObjects.Clear();
        }

        private void GenerateRoamWaypoints(DungeonGenerator gen)
        {
            // Clear old waypoints
            if (_waypointParent != null)
                Destroy(_waypointParent);

            _waypointParent = new GameObject("SlimeWaypoints");

            // Pick random floor tiles as waypoints
            var floorTiles = new List<Vector2Int>();
            for (int x = 0; x < config.gridWidth; x++)
                for (int y = 0; y < config.gridHeight; y++)
                    if (gen.Grid[x, y] == DungeonTileType.Floor)
                        floorTiles.Add(new Vector2Int(x, y));

            // Sample N random floor positions as waypoints
            int waypointCount = Mathf.Min(20, floorTiles.Count);
            for (int i = 0; i < waypointCount; i++)
            {
                int idx = UnityEngine.Random.Range(0, floorTiles.Count);
                var worldPos = GridToWorld(floorTiles[idx])
                             + new Vector3(0f, config.objectYOffset, 0f);

                var wp = new GameObject($"WP_{i}");
                wp.transform.position = worldPos;
                wp.transform.SetParent(_waypointParent.transform);

                floorTiles.RemoveAt(idx);
            }
        }

        private void VisualizeFloorWithoutEnemies(DungeonGenerator gen) => VisualizeFloor(gen);

        private void SpawnEnemies(DungeonGenerator gen)
        {
            float yOff = config.objectYOffset;
            foreach (var (pos, enemy) in gen.Enemies)
            {
                if (_currentFloorData.IsEnemyKilled(pos)) continue;
                if (enemy.prefab == null) continue;

                var go = SpawnObjectY(enemy.prefab, pos, 0f, yOff, objectsParent);
                if (go == null) continue;

                // Assign waypoints to slime's blackboard
                var behaviorAgent = go.GetComponent<BehaviorGraphAgent>();
                if (behaviorAgent != null && _waypointParent != null)
                {
                    // Build list of waypoint GameObjects
                    var waypointList = new List<GameObject>();
                    foreach (Transform child in _waypointParent.transform)
                        waypointList.Add(child.gameObject);

                    bool result = behaviorAgent.BlackboardReference
                        .SetVariableValue("WayPoints", waypointList);
                    Debug.Log($"[Dungeon] SetVariableValue WayPoints result: {result} count:{waypointList.Count}");
                }
            }
        }

        private void VisualizeFloor(DungeonGenerator gen)
        {
            int w = config.gridWidth;
            int h = config.gridHeight;
            var ts = config.tileSet;

            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                {
                    if (gen.Grid[x, y] != DungeonTileType.Floor) continue;

                    if (gen.IsRoomTile[x, y])
                    {
                        var roomPrefab = ts?.prefabRoomFloor ?? ts?.prefabOpen;
                        if (roomPrefab != null)
                            SpawnObject(roomPrefab, new Vector2Int(x, y), 0f, tilesParent);
                        continue;
                    }

                    int mask = ComputeNeighborMask(gen.Grid, x, y, w, h);
                    var (prefab, rotY) = ts != null ? ts.GetTileForMask(mask) : (null, 0f);
                    if (prefab == null) continue;
                    SpawnObject(prefab, new Vector2Int(x, y), rotY, tilesParent);
                }

            if (ts?.prefabWall != null)
            {
                var dirs = new (int dx, int dz, float rotY)[]
                {
                    ( 0,  1,   0f),
                    ( 1,  0,  90f),
                    ( 0, -1, 180f),
                    (-1,  0, 270f),
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
                                Vector3 wallPos = GridToWorld(new Vector2Int(x, y))
                                    + new Vector3(dx * tileSize * 0.5f, 0f, dz * tileSize * 0.5f);
                                var go = Instantiate(ts.prefabWall, wallPos,
                                                     Quaternion.Euler(0f, rotY, 0f), tilesParent);
                                float wallModelW = ts.wallModelWidth > 0f ? ts.wallModelWidth : 1f;
                                var s = go.transform.localScale;
                                go.transform.localScale = new Vector3(s.x * tileSize / wallModelW, s.y, s.z);
                                _spawnedObjects.Add(go);
                            }
                        }
                    }
            }

            float yOff = config.objectYOffset;

            if (config.ladderPrefab)
                SpawnObjectY(config.ladderPrefab, gen.LadderPos, 0f, yOff, objectsParent);

            foreach (var pos in gen.Rocks)
            {
                if (_currentFloorData.IsRockBroken(pos)) continue;
                if (config.rockPrefab)
                    SpawnObjectY(config.rockPrefab, pos, 0f, yOff, objectsParent);
            }

            foreach (var (pos, ore) in gen.Ores)
            {
                if (_currentFloorData.IsOreHarvested(pos)) continue;
                if (ore.prefab == null) continue;
                SpawnObjectY(ore.prefab, pos, 0f, yOff, objectsParent);
            }
        }

        // ── Dungeon Object Callbacks ───────────────────────────

        public void OnOreHarvested(Vector2Int pos) => _currentFloorData?.MarkOreHarvested(pos);
        public void OnEnemyKilled(Vector2Int pos) => _currentFloorData?.MarkEnemyKilled(pos);
        public void OnRockBroken(Vector2Int pos) => _currentFloorData?.MarkRockBroken(pos);

        // ── Tile Helpers ──────────────────────────────────────

        private int ComputeNeighborMask(DungeonTileType[,] grid, int x, int y, int w, int h)
        {
            int mask = 0;
            if (IsFloor(grid, x, y + 1, w, h)) mask |= 1;
            if (IsFloor(grid, x + 1, y, w, h)) mask |= 2;
            if (IsFloor(grid, x, y - 1, w, h)) mask |= 4;
            if (IsFloor(grid, x - 1, y, w, h)) mask |= 8;
            return mask;
        }

        private bool IsFloor(DungeonTileType[,] grid, int x, int y, int w, int h)
        {
            if (x < 0 || y < 0 || x >= w || y >= h) return false;
            return grid[x, y] == DungeonTileType.Floor;
        }

        // ── Spawn Helpers ─────────────────────────────────────

        private GameObject SpawnObject(GameObject prefab, Vector2Int gridPos, float rotY, Transform parent)
        {
            var go = Instantiate(prefab, GridToWorld(gridPos), Quaternion.Euler(0f, rotY, 0f), parent);
            _spawnedObjects.Add(go);
            return go;
        }

        private GameObject SpawnObjectY(GameObject prefab, Vector2Int gridPos, float rotY, float yOffset, Transform parent)
        {
            if (!IsServer) return null;

            var go = Instantiate(prefab, GridToWorld(gridPos) + new Vector3(0f, yOffset, 0f),
                                 Quaternion.Euler(0f, rotY, 0f), parent);

            var netObj = go.GetComponent<NetworkObject>();
            if (netObj != null)
                netObj.Spawn(true); // true = destroy with scene
            else
                Debug.LogWarning($"[DungeonManager] {prefab.name} has no NetworkObject — using plain Instantiate.");

            _spawnedObjects.Add(go);
            return go;
        }

        // ── Utility ───────────────────────────────────────────

        public Vector3 GridToWorld(Vector2Int grid)
            => new Vector3(grid.x * tileSize, 0f, grid.y * tileSize);

        public Vector2Int WorldToGrid(Vector3 world)
            => new Vector2Int(
                Mathf.RoundToInt(world.x / tileSize),
                Mathf.RoundToInt(world.z / tileSize));

        private int GenerateSeedForFloor(int floor)
        {
            unchecked
            {
                int h = _saveData.masterSeed;
                h = h * 31 + floor;
                h = h ^ (h >> 16);
                return h;
            }
        }
    }
}
