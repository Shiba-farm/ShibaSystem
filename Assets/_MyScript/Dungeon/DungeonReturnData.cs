// DungeonReturnData.cs
// เก็บข้อมูลสำหรับ Warp ไป-กลับ Dungeon (static = ข้ามระหว่าง Scene ได้)

using UnityEngine;

public static class DungeonReturnData
{
    /// <summary>ชื่อ Scene ของฟาร์ม (ใช้ตอน return จาก dungeon)</summary>
    public static string  farmScene       = "Prototye";   // ← ชื่อ Scene ฟาร์ม

    /// <summary>ตำแหน่งที่ player ยืนตอนเข้า dungeon (ใช้ spawn กลับมา)</summary>
    public static Vector3 returnPosition  = Vector3.zero;

    /// <summary>true = กลับจาก dungeon (ตาย หรือออกเอง)</summary>
    public static bool    returnFromDeath = false;
}
