using UnityEngine;

/// <summary>
/// วางบนพื้นที่น้ำ — ใช้ Trigger Collider ตรวจว่า player เข้ามาในโซนตกปลา
/// สำหรับเรือ: BoatController จะเรียก EnterZone/ExitZone เองโดยไม่ใช้ Trigger
/// </summary>
public class FishingZone : MonoBehaviour
{
    [Header("Data")]
    public FishingZoneSO zoneData;

    [Header("Fishing Stand Point (optional)")]
    [Tooltip("ทิศที่ player จะหันหน้าไปตอนตกปลา — ถ้าไม่กำหนดจะยืนที่เดิม")]
    public Transform fishingDirectionPoint;

    [Header("Trigger (บนบก)")]
    [Tooltip("ปิดถ้าเป็น FishingZone บนเรือ (BoatController จัดการเอง)")]
    public bool useTrigger = true;

    Collider _col;

    void Awake()
    {
        _col = GetComponent<Collider>();
        if (_col != null) _col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!useTrigger) return;
        if (!other.CompareTag("Player")) return;
        FishingSystem.Instance?.EnterZone(this);
    }

    void OnTriggerExit(Collider other)
    {
        if (!useTrigger) return;
        if (!other.CompareTag("Player")) return;
        FishingSystem.Instance?.ExitZone(this);
    }
}
