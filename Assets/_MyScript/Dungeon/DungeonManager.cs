// DungeonManager.cs
// Singleton ควบคุม Dungeon ทั้งหมด — Enter, Generate, Visualize, Save

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;

namespace MyGame.Dungeon
{
    public class DungeonManager : MonoBehaviour
    {
        // ──────────────────────────────────────────────────────────────────────
        // Singleton
        // ──────────────────────────────────────────────────────────────────────
        public static DungeonManager Instance { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        // ──────────────────────────────────────────────────────────────────────
        // Inspector
        // ──────────────────────────────────────────────────────────────────────
        [Header("Config")]
        public DungeonConfigSO config;

        [Header("Tile Size (เมตร)")]
        public float tileSize = 1f;

        [Header("Player")]
        public Transform player;            // อ้างอิง player transform

        [Header("Parent สำหรับ Dungeon Objects")]
        public Transform tilesParent;
        public Transform objectsParent;

        [Header("NavMesh (Runtime Bake)")]
        [Tooltip("ลาก NavMeshSurface component จาก GameObject ตัวนี้มาใส่")]
        public NavMeshSurface navMeshSurface;

        // ──────────────────────────────────────────────────────────────────────
        // Runtime State
        // ──────────────────────────────────────────────────────────────────────
        public int CurrentFloor  { get; private set; } = 1;
        public int MaxFloors     => 100;
        public bool IsInDungeon  { get; private set; } = false;

        private DungeonSaveData saveData;
        private DungeonFloorData currentFloorData;
        private List<GameObject> spawnedObjects = new List<GameObject>();

        // ──────────────────────────────────────────────────────────────────────
        // Events
        // ──────────────────────────────────────────────────────────────────────
        public static event Action<int> OnFloorEntered;   // ส่ง floor number
        public static event Action<int> OnFloorCleared;   // ส่ง floor number

        // ──────────────────────────────────────────────────────────────────────
        // Public API
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>เข้า Dungeon / โหลด save ที่มีอยู่</summary>
        public void EnterDungeon(DungeonSaveData existingSave = null)
        {
            saveData = existingSave ?? new DungeonSaveData
            {
                masterSeed = UnityEngine.Random.Range(int.MinValue, int.MaxValue)
            };

            IsInDungeon  = true;
            CurrentFloor = Mathf.Max(1, saveData.currentFloor);
            EnterFloor(CurrentFloor);
        }

        /// <summary>เข้าชั้นที่กำหนด</summary>
        public void EnterFloor(int floor)
        {
            if (floor > MaxFloors)
            {
                Debug.Log("[Dungeon] คุณได้ผ่านชั้นสุดท้าย (100) แล้ว!");
                ExitDungeon();
                return;
            }

            CurrentFloor = floor;
            saveData.currentFloor = floor;
            if (floor > saveData.deepestFloorReached)
                saveData.deepestFloorReached = floor;

            int seed = GenerateSeedForFloor(floor);
            currentFloorData = saveData.GetOrCreate(floor, seed);

            ClearFloor();

            var gen = new DungeonGenerator(config, floor, seed);
            gen.Generate();

            // Visualize เฉพาะ Tile + Ore + Rock (ยังไม่ spawn Enemy)
            VisualizeFloorWithoutEnemies(gen);

            // วาง player
            if (player != null)
                player.position = GridToWorld(gen.PlayerSpawnPos);

            // Bake NavMesh ก่อน แล้วค่อย spawn Enemy หลัง bake เสร็จ
            StartCoroutine(BakeThenSpawnEnemies(gen));

            OnFloorEntered?.Invoke(floor);
        }

        private IEnumerator BakeThenSpawnEnemies(DungeonGenerator gen)
        {
            yield return null; // รอ 1 frame ให้ tiles ขึ้นก่อน

            // Bake NavMesh
            if (navMeshSurface != null)
            {
                navMeshSurface.BuildNavMesh();
                Debug.Log("[Dungeon] NavMesh baked ✅");
            }

            yield return null; // รออีก 1 frame ให้ NavMesh พร้อม

            // ค่อย spawn Enemy หลัง NavMesh พร้อมแล้ว
            SpawnEnemies(gen);
        }

        /// <summary>ลงบันไดไปชั้นถัดไป — มี Fade + Floor Text</summary>
        public void GoNextFloor()
        {
            currentFloorData.cleared = true;
            OnFloorCleared?.Invoke(CurrentFloor);
            StartCoroutine(TransitionToNextFloor(CurrentFloor + 1));
        }

        private IEnumerator TransitionToNextFloor(int nextFloor)
        {
            var trans = DungeonFloorTransition.Instance;

            // 1. Fade ดำ
            if (trans != null)
                yield return StartCoroutine(trans.FadeIn());

            // 2. Generate ชั้นใหม่ขณะหน้าจอดำ
            EnterFloor(nextFloor);

            // 3. รอ NavMesh bake + tile spawn (ประมาณ 3 frames)
            yield return null;
            yield return null;
            yield return null;

            // 4. แสดงข้อความ "ชั้น X"
            if (trans != null)
            {
                trans.ShowFloorText(nextFloor);
                yield return new WaitForSeconds(trans.holdDuration);
            }
            else
            {
                yield return new WaitForSeconds(1.2f);
            }

            // 5. Fade กลับ
            if (trans != null)
                yield return StartCoroutine(trans.FadeOut());
        }

        /// <summary>ออกจาก Dungeon</summary>
        public void ExitDungeon()
        {
            ClearFloor();
            IsInDungeon = false;
            Debug.Log("[Dungeon] ออกจาก Dungeon แล้ว");
        }

        // ──────────────────────────────────────────────────────────────────────
        // Floor Visualization
        // ──────────────────────────────────────────────────────────────────────

        public void ClearFloor()
        {
            foreach (var go in spawnedObjects)
                if (go) Destroy(go);
            spawnedObjects.Clear();
        }

        // เรียกจาก EnterFloor — ไม่รวม Enemy (spawn หลัง NavMesh bake)
        private void VisualizeFloorWithoutEnemies(DungeonGenerator gen)
        {
            VisualizeFloor(gen, spawnEnemies: false);
        }

        private void SpawnEnemies(DungeonGenerator gen)
        {
            float yOff = config.objectYOffset;
            foreach (var (pos, enemy) in gen.Enemies)
            {
                if (currentFloorData.IsEnemyKilled(pos)) continue;
                if (enemy.prefab == null) continue;
                var go = SpawnObjectY(enemy.prefab, pos, 0f, yOff, objectsParent);
                go.GetComponent<DungeonEnemyAI>()?.Init(enemy, CurrentFloor);
            }
        }

        private void VisualizeFloor(DungeonGenerator gen, bool spawnEnemies = true)
        {
            int w = config.gridWidth;
            int h = config.gridHeight;
            var ts = config.tileSet;

            // ── Floor Tiles (Modular) ────────────────────────────────────────
            for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
            {
                if (gen.Grid[x, y] != DungeonTileType.Floor) continue;

                // ห้อง BSP → ใช้ prefabRoomFloor (ไม่มีผนัง)
                if (gen.IsRoomTile[x, y])
                {
                    var roomPrefab = ts?.prefabRoomFloor ?? ts?.prefabOpen;
                    if (roomPrefab != null)
                        SpawnObject(roomPrefab, new Vector2Int(x, y), 0f, tilesParent);
                    continue;
                }

                // ทางเดิน corridor → เลือกโมเดลตาม neighbor mask
                int mask = ComputeNeighborMask(gen.Grid, x, y, w, h);
                var (prefab, rotY) = ts != null
                    ? ts.GetTileForMask(mask)
                    : (null, 0f);

                if (prefab == null) continue;
                SpawnObject(prefab, new Vector2Int(x, y), rotY, tilesParent);
            }

            // ── Wall Tiles — วางที่ขอบ Floor หันหน้าเข้าหา Floor ──────────
            if (ts != null && ts.prefabWall != null)
            {
                // ทิศทั้ง 4: (dx, dz, rotY)
                // rotY = ทิศที่ผนังหันหน้าไป (เข้าหาห้อง)
                var dirs = new (int dx, int dz, float rotY)[]
                {
                    ( 0,  1,   0f),   // N
                    ( 1,  0,  90f),   // E
                    ( 0, -1, 180f),   // S
                    (-1,  0, 270f),   // W
                };

                for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                {
                    if (gen.Grid[x, y] != DungeonTileType.Floor) continue;

                    foreach (var (dx, dz, rotY) in dirs)
                    {
                        int nx = x + dx;
                        int ny = y + dz;
                        // ถ้า neighbor เป็น Wall → วาง wall piece ที่ตำแหน่ง Floor นั้น ชิดขอบ
                        if (nx < 0 || ny < 0 || nx >= w || ny >= h ||
                            gen.Grid[nx, ny] == DungeonTileType.Wall)
                        {
                            // ตำแหน่ง offset ชิดขอบ tile (ครึ่ง tileSize)
                            Vector3 wallPos = GridToWorld(new Vector2Int(x, y))
                                + new Vector3(dx * tileSize * 0.5f, 0f, dz * tileSize * 0.5f);

                            var go = Instantiate(ts.prefabWall, wallPos,
                                                 Quaternion.Euler(0f, rotY, 0f), tilesParent);

                            // Auto-scale ให้กว้างเท่า tileSize เสมอ
                            float wallModelW = ts.wallModelWidth > 0f ? ts.wallModelWidth : 1f;
                            float scaleX = tileSize / wallModelW;
                            Vector3 s = go.transform.localScale;
                            go.transform.localScale = new Vector3(s.x * scaleX, s.y, s.z);

                            spawnedObjects.Add(go);
                        }
                    }
                }
            }

            float yOff = config.objectYOffset;

            // ── Ladder ──────────────────────────────────────────────────────
            if (config.ladderPrefab)
                SpawnObjectY(config.ladderPrefab, gen.LadderPos, 0f, yOff, objectsParent)
                    .GetComponent<DungeonLadder>()?.Setup(this);

            // ── Rocks ────────────────────────────────────────────────────────
            foreach (var pos in gen.Rocks)
            {
                if (currentFloorData.IsRockBroken(pos)) continue;
                if (config.rockPrefab)
                    SpawnObjectY(config.rockPrefab, pos, 0f, yOff, objectsParent);
            }

            // ── Ores ─────────────────────────────────────────────────────────
            foreach (var (pos, ore) in gen.Ores)
            {
                if (currentFloorData.IsOreHarvested(pos)) continue;
                if (ore.prefab == null) continue;
                var go = SpawnObjectY(ore.prefab, pos, 0f, yOff, objectsParent);
                go.GetComponent<DungeonOreNode>()?.Setup(ore, pos, this);
            }

            // ── Enemies — spawn ผ่าน SpawnEnemies() หลัง NavMesh bake แทน ──
        }

        // ──────────────────────────────────────────────────────────────────────
        // Tile Helpers
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>คำนวณ bitmask  N=1  E=2  S=4  W=8</summary>
        private int ComputeNeighborMask(DungeonTileType[,] grid, int x, int y, int w, int h)
        {
            int mask = 0;
            if (IsFloor(grid, x,     y + 1, w, h)) mask |= 1; // N
            if (IsFloor(grid, x + 1, y,     w, h)) mask |= 2; // E
            if (IsFloor(grid, x,     y - 1, w, h)) mask |= 4; // S
            if (IsFloor(grid, x - 1, y,     w, h)) mask |= 8; // W
            return mask;
        }

        private bool IsFloor(DungeonTileType[,] grid, int x, int y, int w, int h)
        {
            if (x < 0 || y < 0 || x >= w || y >= h) return false;
            return grid[x, y] == DungeonTileType.Floor;
        }

        private bool HasFloorNeighbor(DungeonTileType[,] grid, int x, int y, int w, int h)
        {
            return IsFloor(grid, x,     y + 1, w, h) ||
                   IsFloor(grid, x + 1, y,     w, h) ||
                   IsFloor(grid, x,     y - 1, w, h) ||
                   IsFloor(grid, x - 1, y,     w, h);
        }

        // ──────────────────────────────────────────────────────────────────────
        // Spawn Helper
        // ──────────────────────────────────────────────────────────────────────

        private GameObject SpawnObject(GameObject prefab, Vector2Int gridPos, float rotY, Transform parent)
        {
            var rot = Quaternion.Euler(0f, rotY, 0f);
            var go  = Instantiate(prefab, GridToWorld(gridPos), rot, parent);
            spawnedObjects.Add(go);
            return go;
        }

        /// <summary>Spawn พร้อม Y offset (ใช้กับ ore/enemy/rock)</summary>
        private GameObject SpawnObjectY(GameObject prefab, Vector2Int gridPos, float rotY, float yOffset, Transform parent)
        {
            var pos = GridToWorld(gridPos) + new Vector3(0f, yOffset, 0f);
            var rot = Quaternion.Euler(0f, rotY, 0f);
            var go  = Instantiate(prefab, pos, rot, parent);
            spawnedObjects.Add(go);
            return go;
        }

        // ──────────────────────────────────────────────────────────────────────
        // Callbacks จาก Objects ใน Dungeon
        // ──────────────────────────────────────────────────────────────────────

        public void OnOreHarvested(Vector2Int gridPos)
        {
            currentFloorData?.MarkOreHarvested(gridPos);
        }

        public void OnEnemyKilled(Vector2Int gridPos)
        {
            currentFloorData?.MarkEnemyKilled(gridPos);
        }

        public void OnRockBroken(Vector2Int gridPos)
        {
            currentFloorData?.MarkRockBroken(gridPos);
        }

        // ──────────────────────────────────────────────────────────────────────
        // Save / Load
        // ──────────────────────────────────────────────────────────────────────

        public DungeonSaveData GetSaveData() => saveData;

        public void LoadSaveData(DungeonSaveData data)
        {
            saveData = data;
        }

        // ──────────────────────────────────────────────────────────────────────
        // Utility
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>Grid position → World position</summary>
        public Vector3 GridToWorld(Vector2Int grid)
        {
            return new Vector3(grid.x * tileSize, 0f, grid.y * tileSize);
        }

        /// <summary>World position → Grid position</summary>
        public Vector2Int WorldToGrid(Vector3 world)
        {
            return new Vector2Int(
                Mathf.RoundToInt(world.x / tileSize),
                Mathf.RoundToInt(world.z / tileSize)
            );
        }

        /// <summary>สร้าง seed เฉพาะชั้น (deterministic)</summary>
        private int GenerateSeedForFloor(int floor)
        {
            // ผสม masterSeed กับ floor เพื่อให้แต่ละชั้นไม่ซ้ำกัน
            unchecked
            {
                int h = saveData.masterSeed;
                h = h * 31 + floor;
                h = h ^ (h >> 16);
                return h;
            }
        }
    }
}
