using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// แถวเดียวในลิสต์เควสฝั่งซ้าย — รู้แค่ questId ของตัวเอง ยิง callback ตอนถูกคลิก
/// Hover → ขยายใหญ่ขึ้น (hoverScale) เพื่อ feedback ผู้เล่น
/// </summary>
public class QuestListRowUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private Image selectedHighlight;
    [SerializeField] private Button button;
    [SerializeField] private float hoverScale = 1.06f;

    public int QuestId { get; private set; }
    public event Action<int> OnClicked;

    private void Awake()
    {
        if (button != null) button.onClick.AddListener(() => OnClicked?.Invoke(QuestId));
    }

    public void Setup(QuestDefinitionSO definition)
    {
        QuestId = definition.questId;
        if (nameText != null) nameText.text = definition.title;
        SetSelected(false);
    }

    public void SetSelected(bool selected)
    {
        if (selectedHighlight != null) selectedHighlight.enabled = selected;
    }

    // ── Hover ────────────────────────────────────────────────────────────────
    public void OnPointerEnter(PointerEventData eventData)
    {
        transform.localScale = Vector3.one * hoverScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        transform.localScale = Vector3.one;
    }
}
