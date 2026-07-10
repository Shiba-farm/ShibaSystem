using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// แท็บ Achievements — หมวดคงที่ทางซ้าย (Fish/Ore & Gems/Crops/Crafted Items)
/// กริดของสะสมทางขวา พร้อม progress bar รวม — รองรับของสะสมจำนวนมากเพราะกริด
/// ใช้ UIListPool รีไซเคิล cell และข้อมูลที่ sync จริงมีแค่ "ที่ค้นพบแล้ว" (sparse)
/// </summary>
public class AchievementTabView : MonoBehaviour, IMenuTabView
{
    [Header("Data")]
    [SerializeField] private AchievementDataSignal connectionSignal;
    [SerializeField] private CollectibleDatabaseSO collectibleDatabase;

    [Header("Categories (คงที่)")]
    [SerializeField] private List<CollectibleCategoryRowUI> categoryRows;

    [Header("Collection Grid")]
    [SerializeField] private Image collectionLogoImage;
    [SerializeField] private TextMeshProUGUI collectionTitleText;
    [SerializeField] private TextMeshProUGUI collectionProgressText;
    [SerializeField] private Slider progressBar;
    [SerializeField] private CollectibleCellUI cellPrefab;
    [SerializeField] private Transform gridContainer;

    public MenuTabId TabId => MenuTabId.Achievements;
    public bool IsInitialized { get; private set; }

    private UIListPool<CollectibleCellUI> _cellPool;
    private AchievementManager _activeManager;
    private bool _needsRefresh;
    private CollectibleCategory _selectedCategory = CollectibleCategory.Fish;

    public void InitializeTab()
    {
        if (IsInitialized) return;
        IsInitialized = true;

        _cellPool = new UIListPool<CollectibleCellUI>(cellPrefab, gridContainer);

        foreach (var row in categoryRows)
            row.OnClicked += SelectCategory;

        connectionSignal.OnDataUpdate += HandleConnected;
        if (connectionSignal.CurrentData != null) HandleConnected(connectionSignal.CurrentData);
    }

    public void OnTabShown() => _needsRefresh = true;
    public void OnTabHidden() { }

    private void OnDestroy()
    {
        connectionSignal.OnDataUpdate -= HandleConnected;
        if (_activeManager != null) _activeManager.Discoveries.OnListChanged -= HandleListChanged;
    }

    private void HandleConnected(AchievementManager manager)
    {
        if (_activeManager == manager) return;
        if (_activeManager != null) _activeManager.Discoveries.OnListChanged -= HandleListChanged;

        _activeManager = manager;
        if (_activeManager == null) return;

        _activeManager.Discoveries.OnListChanged += HandleListChanged;
        _needsRefresh = true;
    }

    private void HandleListChanged(NetworkListEvent<NetworkDiscoveryEntry> evt) => _needsRefresh = true;

    private void LateUpdate()
    {
        if (!_needsRefresh) return;
        _needsRefresh = false;
        RefreshAll();
    }

    private void SelectCategory(CollectibleCategory category)
    {
        _selectedCategory = category;
        RefreshAll();
    }

    private void RefreshAll()
    {
        if (_activeManager == null || collectibleDatabase == null) return;

        foreach (var row in categoryRows)
        {
            int discovered = 0, total = 0;
            foreach (var def in collectibleDatabase.GetByCategory(row.Category))
            {
                total++;
                if (_activeManager.IsDiscovered(def.collectibleId)) discovered++;
            }
            row.Refresh(discovered, total);
            row.SetSelected(row.Category == _selectedCategory);

            // แถวไหนตรงกับหมวดที่เลือกอยู่ตอนนี้ เอาโลโก้ของแถวนั้นไปโชว์ที่หัวข้อฝั่งขวาด้วย
            if (row.Category == _selectedCategory && collectionLogoImage != null)
                collectionLogoImage.sprite = row.Icon;
        }

        int selectedDiscovered = 0, selectedTotal = 0, cellIndex = 0;
        foreach (var def in collectibleDatabase.GetByCategory(_selectedCategory))
        {
            selectedTotal++;
            bool discovered = _activeManager.IsDiscovered(def.collectibleId);
            if (discovered) selectedDiscovered++;

            _cellPool.GetOrCreate(cellIndex++).Setup(def, discovered);
        }
        _cellPool.ReleaseExtra(cellIndex);

        if (collectionTitleText != null) collectionTitleText.text = $"{_selectedCategory} collection";
        if (collectionProgressText != null) collectionProgressText.text = $"{selectedDiscovered} of {selectedTotal} discovered";
        if (progressBar != null)
        {
            progressBar.minValue = 0;
            progressBar.maxValue = Mathf.Max(1, selectedTotal);
            progressBar.value = selectedDiscovered;
        }
    }
}
