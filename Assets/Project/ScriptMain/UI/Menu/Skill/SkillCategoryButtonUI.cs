using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillCategoryButtonUI : MonoBehaviour
{
    [SerializeField] private SkillCategory category;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private Image selectedHighlight;
    [SerializeField] private Button button;

    public SkillCategory Category => category;
    public event Action<SkillCategory> OnClicked;

    private void Awake()
    {
        if (button != null) button.onClick.AddListener(() => OnClicked?.Invoke(category));
    }

    public void Refresh(int categoryLevel)
    {
        if (nameText != null) nameText.text = category.ToString();
        if (levelText != null) levelText.text = $"Lv. {categoryLevel}";
    }

    public void SetSelected(bool selected)
    {
        if (selectedHighlight != null) selectedHighlight.enabled = selected;
    }
}
