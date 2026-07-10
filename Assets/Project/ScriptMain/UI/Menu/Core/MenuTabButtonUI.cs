using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ปุ่มแท็บเดี่ยว — รู้แค่ TabId ของตัวเอง ยิง callback ตอนถูกคลิก
///
/// "Tab rise" effect:
///   ใช้ LayoutElement.preferredHeight เพิ่มความสูงของ active tab
///   ร่วมกับ HLG ที่ตั้งค่า:
///     ChildControlHeight   = 1  (ให้ HLG อ่าน preferredHeight)
///     ChildForceExpandHeight = 0  (ไม่บังคับขยายเต็ม container)
///     ChildAlignment        = LowerLeft (6) — tabs เรียงชิดล่าง
///   ผลลัพธ์: active tab สูงกว่า → ดูเหมือนดันขึ้น โดยไม่ต้องแตะ anchoredPosition
///   (anchoredPosition ถูก HLG ควบคุม ห้ามแตะโดยตรง)
/// </summary>
[RequireComponent(typeof(Button))]
public class MenuTabButtonUI : MonoBehaviour
{
    [SerializeField] private MenuTabId tabId;
    [SerializeField] private Color activeColor   = new Color(1f, 1f, 1f, 1f);
    [SerializeField] private Color inactiveColor = new Color(0.85f, 0.85f, 0.85f, 1f);
    [SerializeField] private float activeOffsetY = 8f;
    [SerializeField] private float naturalHeight = 120f;

    public MenuTabId TabId => tabId;
    public event Action<MenuTabId> OnClicked;

    private Button        _button;
    private Image         _image;
    private LayoutElement _layoutElement;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(() => OnClicked?.Invoke(tabId));
        _image  = GetComponent<Image>();

        // เพิ่ม LayoutElement อัตโนมัติ (ไม่ต้องเพิ่มใน Inspector)
        _layoutElement = GetComponent<LayoutElement>() ?? gameObject.AddComponent<LayoutElement>();
        _layoutElement.preferredHeight = naturalHeight;
    }

    public void SetActive(bool isActive)
    {
        // เปลี่ยนสี background Image
        if (_image != null)
            _image.color = isActive ? activeColor : inactiveColor;

        // เพิ่ม/ลด preferredHeight → HLG ปรับความสูง → tab ดันขึ้น/ลง
        if (_layoutElement != null)
            _layoutElement.preferredHeight = isActive ? naturalHeight + activeOffsetY : naturalHeight;
    }
}
