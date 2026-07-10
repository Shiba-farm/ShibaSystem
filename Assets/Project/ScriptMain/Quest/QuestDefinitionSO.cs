using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ข้อมูล "นิยาม" ของเควสหนึ่งอัน — เขียนโดย designer ล้วน ๆ ไม่มี runtime state
/// อยู่ในนี้เลย (runtime state แยกไปอยู่ใน QuestManager.NetworkQuestEntry)
/// ทำให้เพิ่มเควสใหม่ = สร้าง asset ใหม่ ไม่ต้องแก้โค้ดแม้แต่บรรทัดเดียว — รองรับ
/// เควสหลักร้อยตามที่ต้องการ
/// </summary>
[CreateAssetMenu(menuName = "ShibaFarm/Quest/Quest Definition")]
public class QuestDefinitionSO : ScriptableObject
{
    [Header("Identity")]
    public int questId;
    public string title;
    public QuestCategory category = QuestCategory.Side;
    public Sprite logo;

    [Header("Content")]
    [TextArea(3, 8)] public string description;
    public int targetProgress = 1; // เช่น "เก็บปลา 5 ตัว" → targetProgress = 5

    [Header("Rewards")]
    public List<QuestRewardEntry> rewards = new();

    [Tooltip("จำนวนเงินที่ได้รับเมื่อทำเควสสำเร็จ (0 = ไม่ได้เงิน)")]
    public long moneyReward = 0;

    [Header("Prerequisites (optional)")]
    [Tooltip("ต้องทำเควสนี้ให้จบก่อนถึงจะเริ่มเควสนี้ได้ — ปล่อยว่างถ้าไม่มี")]
    public List<QuestDefinitionSO> prerequisiteQuests = new();
}
