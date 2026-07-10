using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// แสดง reward item หนึ่งชิ้นในช่องรางวัลของ Quest detail panel
///
/// [DEBUG] ใน Editor / Dev Build — คลิกที่ช่อง reward เพื่อ grant ไอเทมนั้นชิ้นเดียวทันที
///         โดยที่เควสยังไม่เสร็จ (ใช้ทดสอบว่าไอเทมเข้า Inventory จริงหรือไม่)
/// </summary>
public class QuestRewardItemUI : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Image             iconImage;
    [SerializeField] private TextMeshProUGUI   amountText;

    public QuestRewardEntry Reward { get; private set; }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    /// <summary>Set โดย QuestTabView.ShowDetail — เรียกเมื่อผู้ใช้คลิก slot นี้ใน debug mode</summary>
    public Action OnDebugClicked;
#endif

    public void Setup(QuestRewardEntry reward)
    {
        Reward = reward;
        if (reward.item == null) return;

        if (iconImage != null)
        {
            iconImage.sprite         = reward.item.icon;
            iconImage.preserveAspect = true;
            iconImage.enabled        = true;
        }

        if (amountText != null)
            amountText.text = reward.amount > 1 ? $"x{reward.amount}" : "";
    }

    /// <summary>
    /// ซ่อน icon และ amount — ใช้แสดงว่า slot นี้รับรางวัลไปแล้ว
    /// slot background ยังคงอยู่ แค่ข้างในว่างเปล่า
    /// </summary>
    public void SetGranted()
    {
        if (iconImage != null)
        {
            iconImage.sprite  = null;
            iconImage.enabled = false;
        }
        if (amountText != null) amountText.text = "";
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        OnDebugClicked = null;
#endif
    }

    // IPointerClickHandler — ทำงานโดยไม่ต้องมี Button component
    public void OnPointerClick(PointerEventData eventData)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        OnDebugClicked?.Invoke();
#endif
    }
}
