using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ข้อมูล "ตัวตน" ของ NPC หนึ่งคนสำหรับระบบ Relationship — แยกออกจาก DialogueSO/
/// NPCInteractable เดิมโดยตั้งใจ (ของเดิมเก็บแค่บทพูด) เพื่อไม่ไปแก้ระบบสนทนาที่
/// มีอยู่แล้ว ผูกกับ NPCInteractable ผ่าน field เสริม (optional) ได้ถ้าต้องการ
/// </summary>
[CreateAssetMenu(menuName = "ShibaFarm/NPC/NPC Definition")]
public class NPCDefinitionSO : ScriptableObject
{
    [Header("Identity")]
    public int npcId;
    public string displayName;
    public Sprite portrait;

    [Header("Profile")]
    [TextArea(3, 10)] public string biography;

    [Header("Relationship")]
    [Tooltip("จำนวนหัวใจสูงสุดที่ NPC คนนี้มีได้")]
    public int maxHeartLevel = 6;
    [Tooltip("ไอเทมที่ NPC คนนี้ \"ชอบ\" — ให้ของนี้แล้วหัวใจขึ้นเร็วกว่าของทั่วไป")]
    public List<ItemSO> favoriteGifts = new();
    [Tooltip("ไอเทมที่ NPC คนนี้ \"รัก\" เป็นพิเศษ — ระดับสูงกว่า favoriteGifts หัวใจขึ้นเร็วที่สุด " +
             "(ถ้าไอเทมเดียวกันอยู่ทั้งสองลิสต์ จะนับเป็น loved ก่อนเสมอ)")]
    public List<ItemSO> lovedGifts = new();

    /// <summary>ตำแหน่ง (นับเฉพาะช่องที่ไม่ null) ของ itemId ใน favoriteGifts — ใช้จับคู่กับ slot ใน UI/discovered mask ไม่เจอคืน -1</summary>
    public int IndexOfFavoriteGift(int itemId) => IndexOfNonNull(favoriteGifts, itemId);

    /// <summary>ตำแหน่ง (นับเฉพาะช่องที่ไม่ null) ของ itemId ใน lovedGifts — ใช้จับคู่กับ slot ใน UI/discovered mask ไม่เจอคืน -1</summary>
    public int IndexOfLovedGift(int itemId) => IndexOfNonNull(lovedGifts, itemId);

    private static int IndexOfNonNull(List<ItemSO> list, int itemId)
    {
        if (list == null) return -1;
        int idx = -1;
        foreach (var it in list)
        {
            if (it == null) continue;
            idx++;
            if (it.itemID == itemId) return idx;
        }
        return -1;
    }
}
