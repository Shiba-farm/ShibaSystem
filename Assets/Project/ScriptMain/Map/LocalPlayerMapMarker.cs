using Unity.Netcode;
using UnityEngine;

/// <summary>
/// แปะไว้ที่ Player prefab — ลงทะเบียนตัวเองเป็น marker บนแผนที่ก็ต่อเมื่อเป็น
/// ผู้เล่น local เท่านั้น (IsOwner) ผู้เล่นคนอื่นในเครื่องนี้จะไม่ขึ้นบนแผนที่
/// (ตรงตามกฎ "ห้ามแสดงข้อมูลผู้เล่นอื่น" — ถ้าต้องการให้เห็นผู้เล่นคนอื่นบนแผนที่
/// ในอนาคต ให้สร้าง MarkerSource แยกที่อ่านจาก NetworkTransform ของผู้เล่นอื่นแทน
/// ไม่ใช่ใช้ตัวนี้)
/// </summary>
public class LocalPlayerMapMarker : NetworkBehaviour, IMapMarkerSource
{
    [SerializeField] private Sprite icon;
    [SerializeField] private string label = "You";

    public MapMarkerType MarkerType => MapMarkerType.Player;
    public string Label => label;
    public Sprite Icon => icon;
    public Transform MarkerTransform => transform;
    public bool IsMarkerVisible => true;

    public override void OnNetworkSpawn()
    {
        if (IsOwner) MapMarkerRegistry.Register(this);
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner) MapMarkerRegistry.Unregister(this);
    }
}
