using UnityEngine;

/// <summary>
/// นิยามสกิลหนึ่งอัน — สกิลจริงยังไม่ถูกออกแบบ (ตามที่ผู้เล่นระบุ) ไฟล์นี้จึงเป็น
/// "โครง" ที่ยังไม่ผูก effect ใด ๆ เข้ากับ gameplay เพิ่มสกิลใหม่ = สร้าง asset ใหม่
/// แล้วค่อยไปเขียนโค้ด effect แยกตอนออกแบบสกิลจริงในอนาคต — UI ไม่ต้องแก้
/// </summary>
[CreateAssetMenu(menuName = "ShibaFarm/Skill/Skill Definition")]
public class SkillDefinitionSO : ScriptableObject
{
    [Header("Identity")]
    public int skillId;
    public SkillCategory category;
    public string displayName;
    [TextArea(2, 6)] public string description;
    public Sprite icon;

    [Header("Progression")]
    [Min(1)] public int maxLevel = 5;
    [Min(1)] public int skillPointCostPerLevel = 2;

    [Header("Unlock Requirement (optional)")]
    [Tooltip("ต้องอัปสกิลนี้ถึงเลเวลที่กำหนดก่อน — ปล่อยว่าง (null) ถ้าไม่มีเงื่อนไข")]
    public SkillDefinitionSO requiredSkill;
    public int requiredSkillLevel = 1;
}
