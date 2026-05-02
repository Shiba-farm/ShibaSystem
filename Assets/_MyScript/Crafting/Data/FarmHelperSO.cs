using UnityEngine;

/// <summary>
/// ตัวช่วยฟาร์ม (Farm Helper) — วางได้บนฟาร์มเป็น Object 3D
/// เช่น บัวรดน้ำอัตโนมัติ, หุ่นไล่กา, เครื่องให้ปุ๋ย ฯลฯ
/// ตัวช่วยแต่ละตัวมีรัศมีทำงาน (radius) และผลที่ให้ (effect)
/// </summary>
[CreateAssetMenu(menuName = "Crafting/Farm Helper")]
public class FarmHelperSO : ScriptableObject
{
    [Header("Info")]
    public string helperName;

    [TextArea(2, 3)]
    public string description;

    public Sprite icon;

    [Header("3D")]
    [Tooltip("Prefab ที่จะ Spawn ในฟาร์มเมื่อวาง")]
    public GameObject placementPrefab;

    [Header("Effect")]
    [Tooltip("ประเภทผลลัพธ์ที่ตัวช่วยทำ")]
    public HelperEffectType effectType;

    [Tooltip("รัศมีทำงาน (จำนวน tile รอบ ๆ ที่ครอบคลุม)")]
    [Min(1)]
    public int effectRadius = 2;

    [Tooltip("ค่า bonus (ขึ้นอยู่กับ effect type)")]
    public float effectValue = 1f;

    [Header("Durability")]
    [Tooltip("ความทนทาน — จำนวนวันก่อนต้องซ่อม (-1 = ไม่เสื่อม)")]
    public int durabilityDays = -1;

    [Header("Punishment")]
    [Tooltip("มูลค่าที่ถูกทำลาย (ใช้คำนวณ priority ว่าลูกน้องเจ้าหนี้ทำลายอันไหนก่อน)")]
    public int destructionPriority = 1;
}

/// <summary>ประเภท Effect ของตัวช่วย</summary>
public enum HelperEffectType
{
    /// <summary>บัวรดน้ำอัตโนมัติ — รดน้ำ SoilTile ทุกเช้า</summary>
    AutoWater,

    /// <summary>หุ่นไล่กา — ป้องกัน Crow/Bird Event (ลดโอกาสพืชถูกทำลาย)</summary>
    Scarecrow,

    /// <summary>เครื่องให้ปุ๋ย — เพิ่มความเร็วเติบโตของพืช</summary>
    Fertilizer,

    /// <summary>รั้วป้องกัน — ลดความเสียหายจากพายุ/แมลง</summary>
    Fence,

    /// <summary>กับดักแมลง — ลดโอกาสแมลงกินพืช</summary>
    InsectTrap,

    /// <summary>ดักสัตว์ — ป้องกันสัตว์ป่าขโมยพืช</summary>
    AnimalTrap,
}
