// DungeonGenerator.cs
// สร้าง Layout ของ Dungeon ด้วย BSP Algorithm

using System.Collections.Generic;
using UnityEngine;

namespace MyGame.Dungeon
{
    /// <summary>
    /// Generate grid layout และ object placement positions สำหรับ 1 ชั้น
    /// ไม่ spawn GameObject เอง — DungeonManager จะนำ output ไป visualize
    /// </summary>
    public class DungeonGenerator
    {
        // ──────────────────────────────────────────────────────────────────────
        // Output Data
        // ──────────────────────────────────────────────────────────────────────
        public DungeonTileType[,] Grid       { get; private set; }
        public bool[,]            IsRoomTile { get; private set; }  // true = พื้นในห้อง BSP
        public Vector2Int PlayerSpawnPos { get; private set; }
        public Vector2Int LadderPos      { get; private set; }
        public List<(Vector2Int pos, DungeonOreSO ore)>    Ores    { get; private set; }
        public List<(Vector2Int pos, DungeonEnemySO enemy)> Enemies { get; private set; }
        public List<Vector2Int>                             Rocks   { get; private set; }

        // ──────────────────────────────────────────────────────────────────────
        // Private
        // ──────────────────────────────────────────────────────────────────────
        private DungeonConfigSO config;
        private int floor;
        private System.Random rng;
        private List<BSPNode> leaves;

        public DungeonGenerator(DungeonConfigSO config, int floor, int seed)
        {
            this.config = config;
            this.floor  = floor;
            this.rng    = new System.Random(seed);
        }

        // ──────────────────────────────────────────────────────────────────────
        // Generate
        // ──────────────────────────────────────────────────────────────────────

        public void Generate()
        {
            int w = config.gridWidth;
            int h = config.gridHeight;

            Grid       = new DungeonTileType[w, h];
            IsRoomTile = new bool[w, h];
            Ores       = new List<(Vector2Int, DungeonOreSO)>();
            Enemies    = new List<(Vector2Int, DungeonEnemySO)>();
            Rocks      = new List<Vector2Int>();
            leaves     = new List<BSPNode>();

            // 1. BSP split
            var root = new BSPNode(new RectInt(0, 0, w, h));
            SplitRecursive(root, config.bspDepth);

            // 2. Create rooms ใน leaf nodes
            root.CreateRoom(rng, config.minRoomSize);
            root.GetLeaves(leaves);

            // 3. Carve rooms ลงบน Grid
            foreach (var leaf in leaves)
                CarveRoom(leaf.room);

            // 4. Connect rooms ด้วยทางเดิน
            ConnectNodes(root);

            // 5. กำหนด spawn positions
            var firstCenter = leaves[0].RoomCenter();
            var lastCenter  = leaves[leaves.Count - 1].RoomCenter();

            PlayerSpawnPos = firstCenter;
            LadderPos      = lastCenter;

            // 6. วาง objects
            PlaceRocks();
            PlaceOres();
            PlaceEnemies();
        }

        // ──────────────────────────────────────────────────────────────────────
        // BSP Splitting
        // ──────────────────────────────────────────────────────────────────────

        private void SplitRecursive(BSPNode node, int depth)
        {
            if (depth <= 0) return;
            if (node.Split(rng, config.minRoomSize))
            {
                SplitRecursive(node.left,  depth - 1);
                SplitRecursive(node.right, depth - 1);
            }
        }

        // ──────────────────────────────────────────────────────────────────────
        // Carve Room
        // ──────────────────────────────────────────────────────────────────────

        private void CarveRoom(RectInt r)
        {
            for (int x = r.x; x < r.x + r.width;  x++)
            for (int y = r.y; y < r.y + r.height; y++)
            {
                SafeSetFloor(x, y);
                // ทุก tile ในห้อง BSP = IsRoomTile
                if (x >= 0 && y >= 0 && x < config.gridWidth && y < config.gridHeight)
                    IsRoomTile[x, y] = true;
            }
        }

        private void SafeSetFloor(int x, int y)
        {
            if (x < 0 || y < 0 || x >= config.gridWidth || y >= config.gridHeight) return;
            Grid[x, y] = DungeonTileType.Floor;
        }

        // ──────────────────────────────────────────────────────────────────────
        // Connect Rooms
        // ──────────────────────────────────────────────────────────────────────

        private void ConnectNodes(BSPNode node)
        {
            if (node.IsLeaf) return;
            ConnectNodes(node.left);
            ConnectNodes(node.right);

            var a = GetAnyLeaf(node.left).RoomCenter();
            var b = GetAnyLeaf(node.right).RoomCenter();
            CarveLShaped(a, b);
        }

        private BSPNode GetAnyLeaf(BSPNode node)
        {
            while (!node.IsLeaf)
                node = node.left ?? node.right;
            return node;
        }

        private void CarveLShaped(Vector2Int a, Vector2Int b)
        {
            if (rng.Next(0, 2) == 0)
            {
                CarveH(a.y, a.x, b.x);
                CarveV(b.x, a.y, b.y);
            }
            else
            {
                CarveV(a.x, a.y, b.y);
                CarveH(b.y, a.x, b.x);
            }
        }

        private void CarveH(int y, int x1, int x2)
        {
            int minX = Mathf.Min(x1, x2);
            int maxX = Mathf.Max(x1, x2);
            int half  = config.corridorWidth / 2;

            for (int x = minX; x <= maxX; x++)
                for (int w = -half; w < config.corridorWidth - half; w++)
                    SafeSetFloor(x, y + w);
        }

        private void CarveV(int x, int y1, int y2)
        {
            int minY = Mathf.Min(y1, y2);
            int maxY = Mathf.Max(y1, y2);
            int half  = config.corridorWidth / 2;

            for (int y = minY; y <= maxY; y++)
                for (int w = -half; w < config.corridorWidth - half; w++)
                    SafeSetFloor(x + w, y);
        }

        // ──────────────────────────────────────────────────────────────────────
        // Place Objects
        // ──────────────────────────────────────────────────────────────────────

        private List<Vector2Int> GetFloorTiles(Vector2Int excludeA, Vector2Int excludeB)
        {
            var tiles = new List<Vector2Int>();
            for (int x = 0; x < config.gridWidth;  x++)
            for (int y = 0; y < config.gridHeight; y++)
            {
                if (Grid[x, y] == DungeonTileType.Floor)
                {
                    var p = new Vector2Int(x, y);
                    if (p != excludeA && p != excludeB)
                        tiles.Add(p);
                }
            }
            return tiles;
        }

        private void PlaceRocks()
        {
            var avail = GetFloorTiles(PlayerSpawnPos, LadderPos);
            int count = config.GetRockCount(floor);
            Shuffle(avail);
            for (int i = 0; i < count && i < avail.Count; i++)
                Rocks.Add(avail[i]);
        }

        private void PlaceOres()
        {
            var avail     = GetFloorTiles(PlayerSpawnPos, LadderPos);
            var orePool   = config.GetAvailableOres(floor);
            int count     = config.GetOreCount(floor);

            // กรอง tile ที่มี rock อยู่แล้ว
            var rockSet   = new HashSet<Vector2Int>(Rocks);
            avail.RemoveAll(p => rockSet.Contains(p));
            Shuffle(avail);

            for (int i = 0; i < count && i < avail.Count; i++)
            {
                var ore = config.GetRandomOre(orePool, rng);
                if (ore != null) Ores.Add((avail[i], ore));
            }
        }

        private void PlaceEnemies()
        {
            var avail       = GetFloorTiles(PlayerSpawnPos, LadderPos);
            var enemyPool   = config.GetAvailableEnemies(floor);
            int count       = config.GetEnemyCount(floor);

            var usedSet     = new HashSet<Vector2Int>(Rocks);
            foreach (var o in Ores) usedSet.Add(o.pos);
            avail.RemoveAll(p => usedSet.Contains(p));
            Shuffle(avail);

            for (int i = 0; i < count && i < avail.Count; i++)
            {
                var enemy = config.GetRandomEnemy(enemyPool, rng);
                if (enemy != null) Enemies.Add((avail[i], enemy));
            }
        }

        // ──────────────────────────────────────────────────────────────────────
        // Utility
        // ──────────────────────────────────────────────────────────────────────

        private void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
