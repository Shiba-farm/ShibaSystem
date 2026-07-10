using UnityEngine;

/// <summary>
/// Contract ที่ "อะไรก็ได้ในโลกเกม" implement เพื่อปรากฏบนแผนที่ — NPC, จุดเควส,
/// หมุดที่กำหนดเอง ฯลฯ MapTabView ไม่รู้จัก class จริงของแต่ละ marker เลย
/// (Dependency Inversion) แค่วน MapMarkerRegistry แล้วอ่านค่าจาก interface นี้
/// </summary>
public interface IMapMarkerSource
{
    MapMarkerType MarkerType { get; }
    string Label { get; }
    Sprite Icon { get; }
    Transform MarkerTransform { get; }
    bool IsMarkerVisible { get; }
}
