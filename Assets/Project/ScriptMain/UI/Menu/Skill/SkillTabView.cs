using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// แท็บ Skills — หมวดทางซ้าย (Farming/Fishing/Mining/Crafting คงที่ 4 ปุ่ม ไม่ต้อง
/// pool) สกิลในหมวดที่เลือกทางขวา (pool เพราะจำนวนสกิลต่อหมวดจะเพิ่มได้ในอนาคต)
/// สกิลจริงยังไม่ถูกออกแบบ — โครงนี้รองรับการเพิ่ม SkillDefinitionSO ใหม่ได้ทันที
/// โดยไม่ต้องแก้ไฟล์นี้เลย
/// </summary>
public class SkillTabView : MonoBehaviour, IMenuTabView
{
    [Header("Data")]
    [SerializeField] private SkillDataSignal connectionSignal;
    [SerializeField] private SkillDatabaseSO skillDatabase;

    [Header("Categories (คงที่ — ลากปุ่มมาวางใน Inspector)")]
    [SerializeField] private List<SkillCategoryButtonUI> categoryButtons;

    [Header("Skill List")]
    [SerializeField] private SkillEntryRowUI rowPrefab;
    [SerializeField] private Transform listContainer;

    [Header("Header")]
    [SerializeField] private TextMeshProUGUI skillPointsText;

    public MenuTabId TabId => MenuTabId.Skills;
    public bool IsInitialized { get; private set; }

    private UIListPool<SkillEntryRowUI> _rowPool;
    private SkillManager _activeManager;
    private bool _needsRefresh;
    private SkillCategory _selectedCategory = SkillCategory.Farming;

    public void InitializeTab()
    {
        if (IsInitialized) return;
        IsInitialized = true;

        _rowPool = new UIListPool<SkillEntryRowUI>(rowPrefab, listContainer);

        foreach (var btn in categoryButtons)
            btn.OnClicked += SelectCategory;

        connectionSignal.OnDataUpdate += HandleConnected;
        if (connectionSignal.CurrentData != null) HandleConnected(connectionSignal.CurrentData);
    }

    public void OnTabShown() => _needsRefresh = true;
    public void OnTabHidden() { }

    private void OnDestroy()
    {
        connectionSignal.OnDataUpdate -= HandleConnected;
        if (_activeManager != null)
        {
            _activeManager.Skills.OnListChanged -= HandleListChanged;
            _activeManager.SkillPoints.OnValueChanged -= HandleSkillPointsChanged;
        }
    }

    private void HandleConnected(SkillManager manager)
    {
        if (_activeManager == manager) return;
        if (_activeManager != null)
        {
            _activeManager.Skills.OnListChanged -= HandleListChanged;
            _activeManager.SkillPoints.OnValueChanged -= HandleSkillPointsChanged;
        }

        _activeManager = manager;
        if (_activeManager == null) return;

        _activeManager.Skills.OnListChanged += HandleListChanged;
        _activeManager.SkillPoints.OnValueChanged += HandleSkillPointsChanged;
        _needsRefresh = true;
    }

    private void HandleListChanged(NetworkListEvent<NetworkSkillEntry> evt) => _needsRefresh = true;
    private void HandleSkillPointsChanged(int previous, int current) => _needsRefresh = true;

    private void LateUpdate()
    {
        if (!_needsRefresh) return;
        _needsRefresh = false;
        RefreshAll();
    }

    private void SelectCategory(SkillCategory category)
    {
        _selectedCategory = category;
        RefreshAll();
    }

    private void RefreshAll()
    {
        if (_activeManager == null || skillDatabase == null) return;

        if (skillPointsText != null) skillPointsText.text = $"Skill points available: {_activeManager.SkillPoints.Value}";

        foreach (var btn in categoryButtons)
        {
            btn.Refresh(_activeManager.GetCategoryLevel(btn.Category));
            btn.SetSelected(btn.Category == _selectedCategory);
        }

        int count = 0;
        foreach (var def in skillDatabase.GetByCategory(_selectedCategory))
        {
            int level = _activeManager.GetLevel(def.skillId);
            bool meetsPrereq = def.requiredSkill == null || _activeManager.GetLevel(def.requiredSkill.skillId) >= def.requiredSkillLevel;
            bool canUpgrade = _activeManager.CanUpgrade(def);

            SkillEntryRowUI row = _rowPool.GetOrCreate(count++);
            row.Setup(def, level, canUpgrade, meetsPrereq);
            row.OnUpgradeClicked -= HandleUpgradeClicked;
            row.OnUpgradeClicked += HandleUpgradeClicked;
        }
        _rowPool.ReleaseExtra(count);
    }

    private void HandleUpgradeClicked(int skillId) => _activeManager.RequestUpgradeSkillServerRpc(skillId);
}
