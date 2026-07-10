using Unity.Netcode;
using UnityEngine;

/// <summary>
/// แปะไว้ที่ Player prefab ตัวเดียวกับ LocalPlayerMapMarker — แต่ตัวนี้ลงทะเบียนเป็น
/// marker บนแผนที่เฉพาะตอนเป็นผู้เล่น "คนอื่น" เท่านั้น (!IsOwner) เพื่อให้เห็นเพื่อน
/// ร่วมเกาะคนอื่นบนแผนที่ด้วย ตามที่ comment ของ LocalPlayerMapMarker แนะนำไว้แต่แรก
/// (สร้าง MarkerSource แยกสำหรับผู้เล่นคนอื่น ไม่ใช้ตัวเดียวกับ local)
///
/// ทั้งสองตัวแปะอยู่ prefab เดียวกันได้โดยไม่ชนกัน เพราะแต่ละ instance จะมีแค่ตัวใด
/// ตัวหนึ่งที่ลงทะเบียนจริงตามเงื่อนไข IsOwner: ของตัวเอง -> LocalPlayerMapMarker,
/// ของคนอื่นที่ network replicate มาให้เห็น -> ตัวนี้
/// </summary>
public class RemotePlayerMapMarker : NetworkBehaviour, IMapMarkerSource
{
    [SerializeField] private Sprite icon;
    [SerializeField] private string label = "Player";

    public MapMarkerType MarkerType => MapMarkerType.Player;
    public string Label => label;
    public Sprite Icon => icon;
    public Transform MarkerTransform => transform;
    public bool IsMarkerVisible => true;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) MapMarkerRegistry.Register(this);
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner) MapMarkerRegistry.Unregister(this);
    }
}
