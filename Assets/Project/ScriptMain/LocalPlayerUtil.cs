using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Helper รวมศูนย์สำหรับหา Transform ของ "ตัวละครผู้เล่นของ client ตัวเอง" ให้ถูกต้องเสมอใน Multiplayer
///
/// ห้ามใช้ GameObject.FindGameObjectWithTag("Player") ตรงๆ ในโค้ดที่ต้องรันตอนมีผู้เล่นหลายคน เพราะฉาก
/// ของแต่ละ client จะมี GameObject tag "Player" มากกว่า 1 ตัวเสมอ (ตัวละครของเราเอง + ตัวละครของผู้เล่น
/// คนอื่นที่ Netcode replicate มาให้เห็นด้วย) FindGameObjectWithTag คืนตัวแรกที่เจอในฉากแบบไม่รับประกันว่า
/// เป็นของเราเอง — ถ้าไปจับตัวละครของอีกฝั่งมาใช้ผิดตัว ระบบที่เช็คระยะ/ตำแหน่งจากผู้เล่น (คุยกับ NPC,
/// ศัตรูไล่ตาม, แร่บินเข้าหาเก็บของ ฯลฯ) จะพังแบบเดารายทาง เช่น บั๊กที่เจอ: Client กดคุยกับ NPC ไม่ติด
/// ต้องรอให้ Host เดินเข้าใกล้ NPC ก่อน เพราะ Client ดันไปเช็คระยะจากตัว Host แทนตัวเอง
///
/// ใช้ NetworkManager.Singleton.LocalClient.PlayerObject ซึ่งเป็น API ของ Netcode ที่คืนตัวละครของ
/// client ตัวเองเสมอ ไม่ว่าจะรันในฐานะ Host หรือ Client — ถูกต้อง 100% ไม่ต้องเดา
/// </summary>
public static class LocalPlayerUtil
{
    /// <summary>คืน Transform ของตัวละครผู้เล่นของ client ตัวเอง — null ถ้ายังไม่ spawn/หาไม่เจอ</summary>
    public static Transform GetLocalPlayerTransform()
    {
        var localPlayerObj = NetworkManager.Singleton != null
            ? NetworkManager.Singleton.LocalClient?.PlayerObject
            : null;

        if (localPlayerObj != null) return localPlayerObj.transform;

        // fallback ตอนไม่มี NetworkManager เลย (เช่น ทดสอบฉาก offline คนเดียว ไม่มีระบบ network)
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        return p != null ? p.transform : null;
    }
}
