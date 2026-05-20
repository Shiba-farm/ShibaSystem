// BSPNode.cs
// Binary Space Partitioning สำหรับสร้างห้องใน Dungeon

using System.Collections.Generic;
using UnityEngine;

namespace MyGame.Dungeon
{
    /// <summary>Node ใน BSP Tree</summary>
    public class BSPNode
    {
        public RectInt bounds;      // พื้นที่ทั้งหมดของ node นี้
        public BSPNode left;        // ส่วนซ้าย/บน
        public BSPNode right;       // ส่วนขวา/ล่าง
        public RectInt room;        // ห้องจริงๆ ที่ carve (เฉพาะ leaf)

        public bool IsLeaf => left == null && right == null;

        public BSPNode(RectInt bounds)
        {
            this.bounds = bounds;
        }

        // ──────────────────────────────────────────────────────────────────────
        // Split
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>
        /// แบ่ง node ออกเป็น 2 ส่วน
        /// คืน false ถ้าเล็กเกินจะแบ่ง
        /// </summary>
        public bool Split(System.Random rng, int minSize)
        {
            if (!IsLeaf) return false;

            // ตัดสินใจว่าจะแบ่งแนวตั้ง (horizontal split) หรือแนวนอน (vertical split)
            bool splitH;

            if (bounds.width > bounds.height * 1.25f)
                splitH = false;                          // กว้างเกิน → แบ่งแนวตั้ง
            else if (bounds.height > bounds.width * 1.25f)
                splitH = true;                           // สูงเกิน → แบ่งแนวนอน
            else
                splitH = rng.Next(0, 2) == 0;           // ใกล้เคียงกัน → สุ่ม

            int max = splitH ? bounds.height : bounds.width;
            if (max < minSize * 2) return false;        // เล็กเกินไป

            int split = rng.Next(minSize, max - minSize);

            if (splitH)
            {
                left  = new BSPNode(new RectInt(bounds.x,         bounds.y,          bounds.width, split));
                right = new BSPNode(new RectInt(bounds.x,         bounds.y + split,  bounds.width, bounds.height - split));
            }
            else
            {
                left  = new BSPNode(new RectInt(bounds.x,         bounds.y,          split,                 bounds.height));
                right = new BSPNode(new RectInt(bounds.x + split, bounds.y,          bounds.width - split,  bounds.height));
            }

            return true;
        }

        // ──────────────────────────────────────────────────────────────────────
        // Create Room
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>สร้างห้องในพื้นที่ bounds (เฉพาะ leaf node)</summary>
        public void CreateRoom(System.Random rng, int minSize)
        {
            if (!IsLeaf)
            {
                if (left  != null) left.CreateRoom(rng, minSize);
                if (right != null) right.CreateRoom(rng, minSize);
                return;
            }

            int padding = 1;
            int maxW = bounds.width  - padding * 2;
            int maxH = bounds.height - padding * 2;

            if (maxW < minSize || maxH < minSize)
            {
                room = new RectInt(bounds.x + padding, bounds.y + padding,
                                   Mathf.Max(minSize, maxW), Mathf.Max(minSize, maxH));
                return;
            }

            int rw = rng.Next(minSize, maxW + 1);
            int rh = rng.Next(minSize, maxH + 1);
            int rx = bounds.x + padding + rng.Next(0, maxW - rw + 1);
            int ry = bounds.y + padding + rng.Next(0, maxH - rh + 1);

            room = new RectInt(rx, ry, rw, rh);
        }

        /// <summary>จุดกึ่งกลางของห้อง (ใช้เชื่อมทางเดิน)</summary>
        public Vector2Int RoomCenter()
        {
            return new Vector2Int(room.x + room.width / 2, room.y + room.height / 2);
        }

        // ──────────────────────────────────────────────────────────────────────
        // Get All Leaves
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>รวบ leaf node ทั้งหมดมาใส่ list</summary>
        public void GetLeaves(List<BSPNode> leaves)
        {
            if (IsLeaf) { leaves.Add(this); return; }
            left?.GetLeaves(leaves);
            right?.GetLeaves(leaves);
        }
    }
}
