using Unity.Netcode;
using UnityEngine;

/// <summary>
/// แปะ component นี้ไว้ที่ root ของโมเดล 3D ผู้เล่น (ลูกของ Player prefab ที่มี
/// SkinnedMeshRenderer ฯลฯ) — เมื่อ spawn แล้วเป็นผู้เล่น local (IsOwner) จะลงทะเบียน
/// ตัวเองไว้ที่ LocalPlayerVisualRegistry ให้ PlayerPreviewRig เอาไปโคลนแสดงในเมนู
/// ผู้เล่นคนอื่นจะไม่ IsOwner ในเครื่องนี้ จึงไม่ถูกลงทะเบียนเด็ดขาด — ปลอดภัยต่อ
/// กฎ "ห้ามแสดงข้อมูลผู้เล่นอื่น" ของระบบเมนูแบบ multiplayer
/// </summary>
public class LocalPlayerVisualMarker : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        if (IsOwner) LocalPlayerVisualRegistry.Register(gameObject);
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner) LocalPlayerVisualRegistry.Unregister(gameObject);
    }
}
