using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One cell in the Animal Shop's scrollable grid (GridContainer/Viewport/Content).
/// Pooled and reused across category switches via UIListPool&lt;AnimalCellUI&gt; —
/// see AnimalPanelUI. Because the instance is reused, Setup() must replace the
/// click listener each time rather than stacking a new one on top of the old.
/// </summary>
public class AnimalCellUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [Tooltip("Optional — leave unassigned if the cell prefab is icon-only.")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private Button cellButton;

    private AnimalSO _animal;

    public void Setup(AnimalSO animal, Action<AnimalSO> onClick)
    {
        _animal = animal;

        if (iconImage != null)
        {
            iconImage.sprite = animal.icon;
            iconImage.enabled = animal.icon != null;
        }

        if (nameText != null)
            nameText.text = animal.animalName;

        if (cellButton != null)
        {
            // Pooled instance — clear the previous animal's listener before adding this one.
            cellButton.onClick.RemoveAllListeners();
            cellButton.onClick.AddListener(() => onClick(_animal));
        }
    }
}
