// DungeonOreSO.cs
// ScriptableObject เก็บข้อมูลแร่แต่ละชนิด

using UnityEngine;

namespace MyGame.Dungeon
{
    [CreateAssetMenu(fileName = "NewOre", menuName = "Dungeon/Ore")]
    public class DungeonOreSO : ScriptableObject
    {
        [Header("ข้อมูลแร่")]
        public string oreName = "New Ore";
        public GameObject prefab;           // Prefab ของ ore node ใน scene

        [Header("ช่วงชั้นที่ Spawn ได้")]
        public int minFloor = 1;            // ชั้นต่ำสุดที่ spawn ได้
        public int maxFloor = 100;          // ชั้นสูงสุดที่ spawn ได้

        [Header("น้ำหนักการ Spawn")]
        [Min(1)]
        public int spawnWeight = 10;        // ยิ่งมาก ยิ่ง spawn บ่อย

        [Header("ของที่ได้จากการขุด")]
        public ItemSO dropItem;             // ItemSO ที่ drop ออกมา
        public Vector2Int yieldRange = new Vector2Int(1, 3); // จำนวน (min, max)

        /// <summary>สุ่มจำนวน drop ในช่วง yieldRange</summary>
        public int GetRandomYield()
        {
            return Random.Range(yieldRange.x, yieldRange.y + 1);
        }
    }
}
