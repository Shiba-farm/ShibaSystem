// DungeonTileSetSO.cs
// กำหนดว่า tile แต่ละรูปแบบใช้โมเดลไหน (Modular Tile Matching)
//
// Bitmask ทิศที่มี Floor เป็น neighbor:
//   N = 1 (บน)
//   E = 2 (ขวา)
//   S = 4 (ล่าง)
//   W = 8 (ซ้าย)

using UnityEngine;

namespace MyGame.Dungeon
{
    [CreateAssetMenu(fileName = "DungeonTileSet", menuName = "Dungeon/TileSet")]
    public class DungeonTileSetSO : ScriptableObject
    {
        [Header("━━ Room Floor (พื้นห้องโล่ง) ━━━━━━━━━━━━━━")]
        [Tooltip("พื้นใน BSP Room — ไม่มีผนัง ใช้โมเดล Open หรือ flat floor")]
        public GameObject prefabRoomFloor;

        [Header("━━ Corridor Tiles (ทางเดินแคบ) ━━━━━━━━━━━━")]
        [Tooltip("ทางตรง เหนือ↔ใต้  (mask 5)")]
        public GameObject prefabStraight;

        [Tooltip("มุม เหนือ+ตะวันออก  (mask 3) — โมเดลหันหน้าไป NE")]
        public GameObject prefabCorner;

        [Tooltip("แยก T  เหนือ+ตะวันออก+ใต้  (mask 7) — ขาดด้านตะวันตก")]
        public GameObject prefabTJunction;

        [Tooltip("แยก 4 ทาง  (mask 15)")]
        public GameObject prefabCross;

        [Tooltip("ทางตัน  หันใต้  (mask 4) — โมเดลหน้าชนผนังด้านเหนือ")]
        public GameObject prefabDeadEnd;

        [Tooltip("พื้นโล่ง / ห้อง  (mask 0 หรือ mask 15 ที่เป็นห้อง)")]
        public GameObject prefabOpen;

        [Header("━━ Wall ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━")]
        [Tooltip("กำแพง (spawn เฉพาะตำแหน่ง Wall ที่ติดกับ Floor)")]
        public GameObject prefabWall;

        [Tooltip("ความกว้างของโมเดล Wall (หน่วย Unity)\nดูได้จาก Inspector → Mesh → Bounds Size X\nระบบจะ scale ให้เท่า tileSize อัตโนมัติ")]
        public float wallModelWidth = 8f;

        [Header("━━ Fallback ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━")]
        [Tooltip("ใช้เมื่อ mask ไม่ตรงกับ prefab ไหนเลย")]
        public GameObject prefabFallback;

        // ──────────────────────────────────────────────────────────────────────
        // Lookup Table  (mask 0–15  →  prefab + rotationY)
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>
        /// คืน (prefab, rotationY) สำหรับ neighbor mask ที่กำหนด
        /// </summary>
        public (GameObject prefab, float rotY) GetTileForMask(int mask)
        {
            switch (mask)
            {
                // ── โดดเดี่ยว ──────────────────────────────────────────────
                case 0:  return (prefabOpen     ?? prefabFallback, 0f);

                // ── ทางตัน (dead end) ──────────────────────────────────────
                //   โมเดล default หันใต้ (mask 4 = S เท่านั้น)
                case 1:  return (prefabDeadEnd  ?? prefabFallback, 180f); // N
                case 2:  return (prefabDeadEnd  ?? prefabFallback, 90f);  // E
                case 4:  return (prefabDeadEnd  ?? prefabFallback, 0f);   // S
                case 8:  return (prefabDeadEnd  ?? prefabFallback, 270f); // W

                // ── ทางตรง (straight) ─────────────────────────────────────
                //   โมเดล default วาง N-S
                case 5:  return (prefabStraight ?? prefabFallback, 0f);   // N+S
                case 10: return (prefabStraight ?? prefabFallback, 90f);  // E+W

                // ── มุม (corner) ──────────────────────────────────────────
                //   โมเดล default มุม N+E
                case 3:  return (prefabCorner   ?? prefabFallback, 0f);   // N+E
                case 6:  return (prefabCorner   ?? prefabFallback, 90f);  // E+S
                case 12: return (prefabCorner   ?? prefabFallback, 180f); // S+W
                case 9:  return (prefabCorner   ?? prefabFallback, 270f); // N+W

                // ── แยก T (T-junction) ────────────────────────────────────
                //   โมเดล default N+E+S  (ขาดทิศ W)
                case 7:  return (prefabTJunction ?? prefabFallback, 0f);  // N+E+S
                case 14: return (prefabTJunction ?? prefabFallback, 90f); // E+S+W
                case 13: return (prefabTJunction ?? prefabFallback, 180f);// N+S+W
                case 11: return (prefabTJunction ?? prefabFallback, 270f);// N+E+W

                // ── แยก 4 ทาง (cross) ─────────────────────────────────────
                case 15: return (prefabCross    ?? prefabOpen ?? prefabFallback, 0f);

                default: return (prefabFallback, 0f);
            }
        }

        /// <summary>ตรวจว่า prefab ครบพอจะ run ได้ไหม</summary>
        public bool IsValid()
        {
            return prefabStraight != null &&
                   prefabCorner   != null &&
                   prefabTJunction!= null &&
                   prefabDeadEnd  != null;
        }
    }
}
