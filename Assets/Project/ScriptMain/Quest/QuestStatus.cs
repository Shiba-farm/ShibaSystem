// สถานะ runtime ของเควสต่อผู้เล่นหนึ่งคน — เควสที่ NotStarted จะไม่ถูกเก็บใน
// NetworkList เลย (ดู QuestManager) เพื่อให้รองรับเควสหลักร้อยโดย NetworkList เล็ก
public enum QuestStatus
{
    NotStarted,
    Active,
    Completed,
}
