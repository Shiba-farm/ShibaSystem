// DungeonFloorData.cs
// เก็บข้อมูล Save ของแต่ละชั้น (seed-based: เก็บแค่ action ของผู้เล่น)

using System;
using System.Collections.Generic;
using UnityEngine;

namespace MyGame.Dungeon
{
    /// <summary>เก็บ position แบบ Serializable (แทน Vector2Int)</summary>
    [Serializable]
    public struct V2Data
    {
        public int x, y;
        public V2Data(int x, int y) { this.x = x; this.y = y; }
        public V2Data(Vector2Int v)  { this.x = v.x; this.y = v.y; }
        public Vector2Int ToVector2Int() => new Vector2Int(x, y);
    }

    /// <summary>ข้อมูล Save ของ 1 ชั้น</summary>
    [Serializable]
    public class DungeonFloorData
    {
        public int floorNumber;
        public int seed;                            // seed ที่ใช้ generate ชั้นนี้

        // สิ่งที่ผู้เล่นทำไปแล้วในชั้นนี้ (เก็บเพื่อ skip ตอน re-visualize)
        public List<V2Data> harvestedOres   = new List<V2Data>();
        public List<V2Data> killedEnemies   = new List<V2Data>();
        public List<V2Data> brokenRocks     = new List<V2Data>();

        public bool cleared = false;  // ผ่านบันไดชั้นนี้แล้วหรือยัง

        public DungeonFloorData(int floor, int seed)
        {
            this.floorNumber = floor;
            this.seed        = seed;
        }

        public bool IsOreHarvested(Vector2Int pos) => harvestedOres.Contains(new V2Data(pos));
        public bool IsEnemyKilled(Vector2Int pos)  => killedEnemies.Contains(new V2Data(pos));
        public bool IsRockBroken(Vector2Int pos)   => brokenRocks.Contains(new V2Data(pos));

        public void MarkOreHarvested(Vector2Int pos) { if (!IsOreHarvested(pos)) harvestedOres.Add(new V2Data(pos)); }
        public void MarkEnemyKilled(Vector2Int pos)  { if (!IsEnemyKilled(pos))  killedEnemies.Add(new V2Data(pos)); }
        public void MarkRockBroken(Vector2Int pos)   { if (!IsRockBroken(pos))   brokenRocks.Add(new V2Data(pos)); }
    }

    /// <summary>ข้อมูล Save ของ Dungeon ทั้งหมด</summary>
    [Serializable]
    public class DungeonSaveData
    {
        public int  deepestFloorReached = 0;
        public int  currentFloor        = 1;
        public int  masterSeed          = 0;        // seed หลักของ run นี้

        public List<DungeonFloorData> floors = new List<DungeonFloorData>();

        /// <summary>ดึงหรือสร้าง FloorData ของชั้น floor</summary>
        public DungeonFloorData GetOrCreate(int floor, int seed)
        {
            foreach (var f in floors)
                if (f.floorNumber == floor) return f;

            var newData = new DungeonFloorData(floor, seed);
            floors.Add(newData);
            return newData;
        }
    }
}
