using UnityEngine;

/// <summary>
/// นิยามของสะสมหนึ่งชิ้น (ปลา/แร่/พืช/ของคราฟต์) — ผูกกับ ItemSO ที่มีอยู่แล้วผ่าน
/// linkedItem เพื่อให้ระบบ gameplay (ตกปลา/ขุดเหมือง/ปลูกพืช/คราฟต์) แค่เรียก
/// AchievementManager.ReportItemObtained(itemID) เฉย ๆ โดยไม่ต้องรู้จัก collectibleId
/// เลย (decoupled ผ่านการ map itemID → collectibleId ในนี้)
/// </summary>
[CreateAssetMenu(menuName = "ShibaFarm/Achievement/Collectible Definition")]
public class CollectibleDefinitionSO : ScriptableObject
{
    [Header("Identity")]
    public int collectibleId;
    public CollectibleCategory category;
    public string displayName;
    public CollectibleRarity rarity = CollectibleRarity.Common;

    [Header("Visual")]
    public Sprite icon;
    [Tooltip("ไอคอนที่โชว์ตอนยังไม่เคยค้นพบ — ปล่อยว่างเพื่อใช้ icon เทาเริ่มต้นของ UI")]
    public Sprite unknownIcon;

    [Header("Link")]
    [Tooltip("ItemSO ที่พอเก็บ/คราฟต์ได้แล้วถือว่า \"ค้นพบ\" ของสะสมนี้")]
    public ItemSO linkedItem;
}
