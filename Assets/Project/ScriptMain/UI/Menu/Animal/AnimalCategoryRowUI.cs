using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One row in the left category list of the Animal Shop panel
/// (CategoryRow_LiveStock / CategoryRow_Fishery / CategoryRow_Equine / CategoryRow_Mysterious).
///
/// Fixed, scene-placed instance — the category it represents is set once in the
/// Inspector, not spawned/pooled. Mirrors CollectibleCategoryRowUI's pattern.
/// </summary>
public class AnimalCategoryRowUI : MonoBehaviour
{
    [SerializeField] private AnimalStockType category;
    [SerializeField] private Button rowButton;
    [Tooltip("Optional — highlight shown while this row is the selected category.")]
    [SerializeField] private Image selectedHighlight;
    [Tooltip("Optional — this row's icon, also used as the big CollectionLogo when selected.")]
    [SerializeField] private Image logoImage;

    public AnimalStockType Category => category;
    public Sprite Icon => logoImage != null ? logoImage.sprite : null;
    public event Action<AnimalStockType> OnClicked;

    private void Awake()
    {
        if (rowButton != null)
            rowButton.onClick.AddListener(() => OnClicked?.Invoke(category));
    }

    public void SetSelected(bool selected)
    {
        if (selectedHighlight != null)
            selectedHighlight.enabled = selected;
    }
}
