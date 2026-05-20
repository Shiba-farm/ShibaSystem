// DungeonEnemySO.cs
// ScriptableObject เก็บข้อมูลศัตรูแต่ละชนิด

using UnityEngine;

namespace MyGame.Dungeon
{
    [CreateAssetMenu(fileName = "NewEnemy", menuName = "Dungeon/Enemy")]
    public class DungeonEnemySO : ScriptableObject
    {
        [Header("ข้อมูลพื้นฐาน")]
        public string enemyName = "New Enemy";
        public GameObject prefab;

        [Header("Stats ชั้น 1")]
        public int baseHealth = 20;
        public int baseDamage = 5;

        [Header("Scaling ทุก 10 ชั้น")]
        public int healthPerTenFloors  = 10;  // HP เพิ่มทุก 10 ชั้น
        public int damagePerTenFloors  = 2;   // Damage เพิ่มทุก 10 ชั้น

        [Header("ช่วงชั้นที่ Spawn ได้")]
        public int minFloor = 1;
        public int maxFloor = 100;

        [Header("น้ำหนักการ Spawn")]
        [Min(1)]
        public int spawnWeight = 10;

        /// <summary>คำนวณ HP ของศัตรูในชั้น floor</summary>
        public int GetHealthForFloor(int floor)
        {
            int bands = (floor - 1) / 10;
            return baseHealth + healthPerTenFloors * bands;
        }

        /// <summary>คำนวณ Damage ของศัตรูในชั้น floor</summary>
        public int GetDamageForFloor(int floor)
        {
            int bands = (floor - 1) / 10;
            return baseDamage + damagePerTenFloors * bands;
        }
    }
}
