using UnityEngine;
using UnityEngine.UI;

public class FavoriteGiftItemUI : MonoBehaviour
{
    [Tooltip("Image ของไอคอนของจริง — ต้องเป็น child แยกต่างหากจากกรอบพื้นหลังเทาๆ " +
             "(กรอบพื้นหลังตั้ง sprite ตรงๆ ที่ Image ของ GameObject หลักใน Editor เลย ไม่ต้องยุ่งกับโค้ด " +
             "เพราะกรอบต้องโชว์ตลอดเวลา ไม่ว่าช่องจะมีของหรือว่าง)")]
    [SerializeField] private Image iconImage;

    [Header("Tier Tint (optional)")]
    [Tooltip("สีไอคอนตอนเป็นของที่ \"ชอบ\" (favoriteGifts)")]
    [SerializeField] private Color likedColor = Color.white;
    [Tooltip("สีไอคอนตอนเป็นของที่ \"รัก\" (lovedGifts) — เด่นกว่าปกติ")]
    [SerializeField] private Color lovedColor = new Color(1f, 0.55f, 0.7f); // ชมพูเข้ม

    /// <summary>true ถ้าช่องนี้กำลังโชว์ placeholder ว่างอยู่ (ยังไม่มีของ/ยังไม่ค้นพบ)</summary>
    public bool IsEmpty { get; private set; } = true;

    public void Setup(ItemSO item, bool isLoved = false)
    {
        if (item == null) { SetEmpty(); return; }
        if (iconImage == null) return;

        IsEmpty = false;
        iconImage.enabled = true;
        iconImage.sprite = item.icon;
        iconImage.preserveAspect = true;
        iconImage.color = isLoved ? lovedColor : likedColor;
    }

    /// <summary>ซ่อนไอคอน (ปิด Image ไอคอน) เหลือแค่กรอบเทาๆ ด้านหลังโชว์เฉยๆ — เรียกตอนยังไม่มี/ยังไม่ค้นพบของชิ้นนี้</summary>
    public void SetEmpty()
    {
        if (iconImage == null) return;

        IsEmpty = true;
        iconImage.enabled = false;
    }
}
