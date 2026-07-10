using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CollectibleCategoryRowUI : MonoBehaviour
{
    [SerializeField] private CollectibleCategory category;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private Image selectedHighlight;
    [SerializeField] private Button button;
    [SerializeField] private Image logoImage; // ไอคอน/โลโก้ของหมวดนี้ — ลากรูปใส่ตรงนี้ใน Inspector

    public CollectibleCategory Category => category;
    /// <summary>Sprite โลโก้ของหมวดนี้ — ใช้โชว์ทั้งไอคอนเล็กในแถวซ้าย และโลโก้ใหญ่ที่หัวข้อฝั่งขวา</summary>
    public Sprite Icon => logoImage != null ? logoImage.sprite : null;
    public event Action<CollectibleCategory> OnClicked;

    private void Awake()
    {
        if (button != null) button.onClick.AddListener(() => OnClicked?.Invoke(category));
    }

    public void Refresh(int discovered, int total)
    {
        if (nameText != null) nameText.text = category.ToString();
        if (progressText != null) progressText.text = $"{discovered}/{total}";
    }

    public void SetSelected(bool selected)
    {
        if (selectedHighlight != null) selectedHighlight.enabled = selected;
    }
}
