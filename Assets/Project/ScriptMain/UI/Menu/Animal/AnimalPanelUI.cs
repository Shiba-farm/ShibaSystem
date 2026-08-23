using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Animal Shop panel — left side is a fixed set of category tabs (LiveStock / Fishery /
/// Equine / Mysterious), right side is a "Title" detail header plus a scrollable grid
/// of that category's animals.
///
/// All display data (icon/name/price) comes straight from the local EntityDatabase asset —
/// no networking involved, since the catalog is static and identical on every client build.
/// The only thing that will eventually go over the network from this panel is the purchase
/// itself (AnimalStockServerManager.BuyLiveStockServerRpc), which is intentionally NOT wired
/// up yet — see AnimalDetailUI.BuyButton.
///
/// Implements IInitializableUI so InGameUIManager.InitializePanels() sets it up once at
/// scene start, the same way every other in-game panel initializes.
/// </summary>
public class AnimalPanelUI : MonoBehaviour, IInitializableUI
{
    [Header("Data Source")]
    [Tooltip("Same EntityDatabase asset assigned on AnimalStockServerManager.")]
    [SerializeField] private EntityDatabase entityDatabase;

    [Header("Left — Categories")]
    [Tooltip("CategoryRow_LiveStock / CategoryRow_Fishery / CategoryRow_Equine / CategoryRow_Mysterious, in the order they should be checked as the default selection.")]
    [SerializeField] private List<AnimalCategoryRowUI> categoryRows;

    [Header("Right — Detail Header (\"Title\")")]
    [SerializeField] private AnimalDetailUI detailUI;

    [Header("Right — Grid")]
    [Tooltip("The ScrollRect's Content transform (the child under GridContainer/Viewport that holds the layout group) — cells are instantiated here.")]
    [SerializeField] private Transform gridContent;
    [SerializeField] private AnimalCellUI cellPrefab;
    [Tooltip("Optional — big logo next to the grid; set to the selected category row's icon.")]
    [SerializeField] private Image collectionLogoImage;
    [SerializeField] private Button closeButton;

    public bool IsInitialized { get; private set; }

    private UIListPool<AnimalCellUI> _cellPool;
    private AnimalStockType _selectedCategory;

    public void InitializeUI()
    {
        if (IsInitialized) return;
        IsInitialized = true;

        if (cellPrefab == null || gridContent == null)
        {
            Debug.LogWarning("[AnimalPanelUI] cellPrefab or gridContent is not assigned.");
            return;
        }

        _cellPool = new UIListPool<AnimalCellUI>(cellPrefab, gridContent);

        foreach (var row in categoryRows)
        {
            if (row == null) continue;
            row.OnClicked += SelectCategory;
        }

        detailUI?.Clear();

        // First-open only: land on category[0] with animal[0] already selected so the
        // panel isn't empty the very first time the player sees it. Every later category
        // switch goes through the click path below, which deliberately does NOT
        // auto-select — see SelectCategory(category, autoSelectFirstAnimal).
        if (categoryRows.Count > 0 && categoryRows[0] != null)
            SelectCategory(categoryRows[0].Category, autoSelectFirstAnimal: true);
        if (closeButton != null)
            closeButton.onClick.AddListener(OnCloseButtonClicked);
    }

    private void OnCloseButtonClicked()
    {
        InGameUIManager.Instance.TogglePanel(InGamePanel.LiveStock);
    }

    // ── Category selection ──────────────────────────────────────────────────

    /// <summary>Category row OnClicked handler — normal switches never auto-select an animal.</summary>
    private void SelectCategory(AnimalStockType category) => SelectCategory(category, autoSelectFirstAnimal: false);

    private void SelectCategory(AnimalStockType category, bool autoSelectFirstAnimal)
    {
        _selectedCategory = category;

        AnimalCategoryRowUI selectedRow = null;
        foreach (var row in categoryRows)
        {
            if (row == null) continue;
            bool isSelected = row.Category == category;
            row.SetSelected(isSelected);
            if (isSelected) selectedRow = row;
        }

        if (collectionLogoImage != null && selectedRow != null)
            collectionLogoImage.sprite = selectedRow.Icon;

        // Deliberately NOT clearing the detail header here — the last animal the player
        // selected (in this category or a previous one) stays shown until they click a
        // new cell. Only the very first open (autoSelectFirstAnimal) picks a default.
        List<AnimalSO> animals = PopulateGrid(category);

        if (!autoSelectFirstAnimal) return;

        foreach (var animal in animals)
        {
            if (animal == null) continue;
            SelectAnimal(animal);
            break;
        }
    }

    private List<AnimalSO> PopulateGrid(AnimalStockType category)
    {
        if (entityDatabase == null)
        {
            Debug.LogWarning("[AnimalPanelUI] entityDatabase is not assigned.");
            return new List<AnimalSO>();
        }

        List<AnimalSO> animals = entityDatabase.GetAnimalsByType(category);

        int index = 0;
        foreach (var animal in animals)
        {
            if (animal == null) continue;
            _cellPool.GetOrCreate(index++).Setup(animal, SelectAnimal);
        }
        _cellPool.ReleaseExtra(index);

        return animals;
    }

    // ── Animal selection (fills the "Title" detail block) ───────────────────

    private void SelectAnimal(AnimalSO animal)
    {
        if (animal == null) return;
        detailUI?.Setup(animal);
    }
}
