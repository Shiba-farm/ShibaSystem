// DungeonConfigSO.cs
// Config หลักของระบบ Dungeon ทั้งหมด

using System.Collections.Generic;
using UnityEngine;

namespace MyGame.Dungeon
{
    [CreateAssetMenu(fileName = "DungeonConfig", menuName = "Dungeon/Config")]
    public class DungeonConfigSO : ScriptableObject
    {
        [Header("ขนาด Grid")]
        public int gridWidth  = 64;
        public int gridHeight = 64;

        [Header("BSP Parameters")]
        [Min(1)]
        public int bspDepth   = 4;          // จำนวนรอบที่ split (ยิ่งมาก ห้องยิ่งเล็ก/เยอะ)
        [Min(4)]
        public int minRoomSize = 6;          // ขนาดห้องขั้นต่ำ

        [Header("Corridor")]
        [Min(1), Tooltip("ความกว้างของทางเดิน (หน่วย: tile)\nแนะนำ 2-3 สำหรับโมเดล 8 unit")]
        public int corridorWidth = 2;        // ความกว้างทางเดินเชื่อมห้อง

        [Header("จำนวน Object ที่ชั้น 1")]
        public int baseOreCount    = 5;
        public int baseEnemyCount  = 3;
        public int baseRockCount   = 8;

        [Header("Scaling ทุก 10 ชั้น")]
        public int oreCountPerTenFloors    = 2;
        public int enemyCountPerTenFloors  = 2;
        public int rockCountPerTenFloors   = 1;

        [Header("จำนวนสูงสุด (0 = ไม่จำกัด)")]
        public int maxEnemyCount = 15;
        public int maxOreCount   = 20;
        public int maxRockCount  = 30;

        [Header("Modular Tile Set")]
        public DungeonTileSetSO tileSet;

        [Header("Prefabs พิเศษ (ไม่ใช่ tile)")]
        public GameObject ladderPrefab;
        public GameObject rockPrefab;

        [Header("Object Spawn Height")]
        [Tooltip("ความสูง Y ของ ore/enemy/rock/ladder เหนือพื้น\nปรับถ้าโมเดลจมพื้นหรือลอยอยู่")]
        public float objectYOffset = 0.5f;

        [Header("Ores ทั้งหมดในเกม")]
        public DungeonOreSO[] ores;

        [Header("Enemies ทั้งหมดในเกม")]
        public DungeonEnemySO[] enemies;

        // ──────────────────────────────────────────────────────────────────────
        // Helper Methods
        // ──────────────────────────────────────────────────────────────────────

        public int GetOreCount(int floor)
        {
            int bands = (floor - 1) / 10;
            int count = baseOreCount + oreCountPerTenFloors * bands;
            return (maxOreCount > 0) ? Mathf.Min(count, maxOreCount) : count;
        }

        public int GetEnemyCount(int floor)
        {
            int bands = (floor - 1) / 10;
            int count = baseEnemyCount + enemyCountPerTenFloors * bands;
            return (maxEnemyCount > 0) ? Mathf.Min(count, maxEnemyCount) : count;
        }

        public int GetRockCount(int floor)
        {
            int bands = (floor - 1) / 10;
            int count = baseRockCount + rockCountPerTenFloors * bands;
            return (maxRockCount > 0) ? Mathf.Min(count, maxRockCount) : count;
        }

        /// <summary>กรอง ores ที่ spawn ได้ในชั้น floor</summary>
        public List<DungeonOreSO> GetAvailableOres(int floor)
        {
            var result = new List<DungeonOreSO>();
            if (ores == null) return result;
            foreach (var ore in ores)
                if (ore != null && floor >= ore.minFloor && floor <= ore.maxFloor)
                    result.Add(ore);
            return result;
        }

        /// <summary>กรอง enemies ที่ spawn ได้ในชั้น floor</summary>
        public List<DungeonEnemySO> GetAvailableEnemies(int floor)
        {
            var result = new List<DungeonEnemySO>();
            if (enemies == null) return result;
            foreach (var e in enemies)
                if (e != null && floor >= e.minFloor && floor <= e.maxFloor)
                    result.Add(e);
            return result;
        }

        /// <summary>สุ่มเลือก ore จาก pool โดยใช้ spawnWeight</summary>
        public DungeonOreSO GetRandomOre(List<DungeonOreSO> pool, System.Random rng)
        {
            if (pool == null || pool.Count == 0) return null;
            int total = 0;
            foreach (var o in pool) total += o.spawnWeight;
            int roll = rng.Next(0, total);
            int cum  = 0;
            foreach (var o in pool)
            {
                cum += o.spawnWeight;
                if (roll < cum) return o;
            }
            return pool[pool.Count - 1];
        }

        /// <summary>สุ่มเลือก enemy จาก pool โดยใช้ spawnWeight</summary>
        public DungeonEnemySO GetRandomEnemy(List<DungeonEnemySO> pool, System.Random rng)
        {
            if (pool == null || pool.Count == 0) return null;
            int total = 0;
            foreach (var e in pool) total += e.spawnWeight;
            int roll = rng.Next(0, total);
            int cum  = 0;
            foreach (var e in pool)
            {
                cum += e.spawnWeight;
                if (roll < cum) return e;
            }
            return pool[pool.Count - 1];
        }
    }
}
