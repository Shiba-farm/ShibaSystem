using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// แท็บ Relationships — แสดง NPC ทุกคนจาก database ฝั่งซ้ายเสมอ
/// NPC ที่ยังไม่เคยพบ → Biography ถูกล็อค, Portrait/Name/Hearts แสดงตามปกติ
/// NPC ที่พบแล้ว → แสดงข้อมูลครบ
/// </summary>
public class RelationshipTabView : MonoBehaviour, IMenuTabView
{
    [Header("Data")]
    [SerializeField] private RelationshipDataSignal connectionSignal;
    [SerializeField] private NPCDatabaseSO npcDatabase;

    [Header("List")]
    [SerializeField] private NPCListRowUI rowPrefab;
    [SerializeField] private Transform listContainer;

    [Header("Detail Panel")]
    [SerializeField] private GameObject detailPanelRoot;
    [SerializeField] private TextMeshProUGUI detailNameText;
    [SerializeField] private Image detailPortraitImage;
    [SerializeField] private TextMeshProUGUI detailBiographyText;
    [SerializeField] private HeartMeterUI detailHeartMeter;

    [Header("Biography Lock")]
    [Tooltip("ข้อความแสดงแทน Biography เมื่อยังไม่เคยพบ NPC")]
    [SerializeField] private string lockedBiographyText = "[ ยังไม่ได้พบ NPC คนนี้ ]";
    [Tooltip("GameObject ที่ครอบ Biography — จะ dim เมื่อล็อค (optional)")]
    [SerializeField] private GameObject biographyLockOverlay;

    [Header("Gifts — ใช้ prefab เดียวกัน แต่แยก container คนละฝั่ง")]
    [SerializeField] private FavoriteGiftItemUI giftPrefab;
    [Tooltip("Container ฝั่ง \"ของที่ชอบ\" (favoriteGifts) — ต้องมี Grid Layout Group 3x2 = 6 ช่อง")]
    [SerializeField] private Transform likedGiftContainer;
    [Tooltip("Container ฝั่ง \"ของที่รัก\" (lovedGifts) — ต้องมี Grid Layout Group 3x2 = 6 ช่อง")]
    [SerializeField] private Transform lovedGiftContainer;

    /// <summary>จำนวนช่อง gift คงที่ต่อฝั่ง (ตาม Figma: ฝั่งละ 6 ช่อง = 3 คอลัมน์ x 2 แถว)</summary>
    private const int GiftSlotsPerSide = 6;

    public MenuTabId TabId => MenuTabId.Relationships;
    public bool IsInitialized { get; private set; }

    private UIListPool<NPCListRowUI>       _listPool;
    private UIListPool<FavoriteGiftItemUI> _likedGiftPool;
    private UIListPool<FavoriteGiftItemUI> _lovedGiftPool;
    private RelationshipManager            _activeManager;
    private bool                           _needsRefresh;
    private int                            _selectedNpcId = -1;

    // ── IMenuTabView ─────────────────────────────────────────────────────────
    public void InitializeTab()
    {
        if (IsInitialized) return;
        IsInitialized = true;

        _listPool      = new UIListPool<NPCListRowUI>(rowPrefab, listContainer);
        _likedGiftPool = new UIListPool<FavoriteGiftItemUI>(giftPrefab, likedGiftContainer);
        _lovedGiftPool = new UIListPool<FavoriteGiftItemUI>(giftPrefab, lovedGiftContainer);

        connectionSignal.OnDataUpdate += HandleConnected;
        if (connectionSignal.CurrentData != null) HandleConnected(connectionSignal.CurrentData);
    }

    public void OnTabShown()  => _needsRefresh = true;
    public void OnTabHidden() { }

    private void OnDestroy()
    {
        connectionSignal.OnDataUpdate -= HandleConnected;
        if (_activeManager != null)
            _activeManager.Relationships.OnListChanged -= HandleListChanged;
    }

    // ── Data binding ─────────────────────────────────────────────────────────
    private void HandleConnected(RelationshipManager manager)
    {
        if (_activeManager == manager) return;
        if (_activeManager != null)
            _activeManager.Relationships.OnListChanged -= HandleListChanged;

        _activeManager = manager;
        if (_activeManager == null) return;

        _activeManager.Relationships.OnListChanged += HandleListChanged;
        _needsRefresh = true;
    }

    private void HandleListChanged(NetworkListEvent<NetworkRelationshipEntry> evt)
        => _needsRefresh = true;

    private void LateUpdate()
    {
        if (!_needsRefresh) return;
        _needsRefresh = false;
        RefreshList();
    }

    // ── List build ───────────────────────────────────────────────────────────
    private void RefreshList()
    {
        Debug.Log($"[RelationshipTabView] RefreshList — manager={_activeManager != null}, db={npcDatabase != null}, npcCount={npcDatabase?.AllNpcs?.Count ?? -1}");
        if (_activeManager == null || npcDatabase == null) return;

        int count = 0;
        bool selectedStillVisible = false;
        int fallbackId = -1;

        // วนลูปจาก Database ทุกคน ไม่ใช่แค่ที่เคยพบ
        foreach (var def in npcDatabase.AllNpcs)
        {
            if (def == null) continue;

            bool hasMet = _activeManager.HasMet(def.npcId);
            var entry   = hasMet ? _activeManager.GetEntry(def.npcId) : default;

            NPCListRowUI row = _listPool.GetOrCreate(count++);
            row.Setup(def, entry.HeartLevel, hasMet);
            row.OnClicked -= SelectNpc;
            row.OnClicked += SelectNpc;

            if (fallbackId < 0) fallbackId = def.npcId;
            if (def.npcId == _selectedNpcId) selectedStillVisible = true;
        }

        _listPool.ReleaseExtra(count);

        if (!selectedStillVisible) _selectedNpcId = fallbackId;

        UpdateSelectionHighlight();
        ShowDetail(_selectedNpcId);
    }

    private void SelectNpc(int npcId)
    {
        _selectedNpcId = npcId;
        UpdateSelectionHighlight();
        ShowDetail(npcId);
    }

    private void UpdateSelectionHighlight()
    {
        foreach (var row in _listPool.Instances)
            row.SetSelected(row.NpcId == _selectedNpcId);
    }

    // ── Detail panel ─────────────────────────────────────────────────────────
    private void ShowDetail(int npcId)
    {
        NPCDefinitionSO def = npcId >= 0 ? npcDatabase.GetByID(npcId) : null;
        if (detailPanelRoot != null) detailPanelRoot.SetActive(def != null);
        if (def == null) { _likedGiftPool.ReleaseAll(); _lovedGiftPool.ReleaseAll(); return; }

        bool hasMet = _activeManager != null && _activeManager.HasMet(npcId);

        // ชื่อ + รูป — แสดงเสมอ
        if (detailNameText     != null) detailNameText.text       = def.displayName;
        if (detailPortraitImage != null) detailPortraitImage.sprite = def.portrait;

        // Hearts — แสดง max แต่ใส่ 0 ถ้ายังไม่พบ
        var entry = hasMet && _activeManager != null
            ? _activeManager.GetEntry(npcId)
            : default;
        detailHeartMeter?.SetHearts(def.maxHeartLevel, entry.HeartLevel);

        // Biography — ล็อคถ้ายังไม่พบ
        if (detailBiographyText != null)
            detailBiographyText.text = hasMet ? def.biography : lockedBiographyText;

        if (biographyLockOverlay != null)
            biographyLockOverlay.SetActive(!hasMet);

        // Gifts — โชว์ตาราง 6 ช่องคงที่ทั้งสองฝั่งเสมอ (Liked / Loved) ตาม Figma
        // เฉลยไอคอนเฉพาะชิ้นที่ "ค้นพบแล้ว" (เคยให้ของชิ้นนั้นแล้วตรง tier จริง) เท่านั้น —
        // ช่องที่ยังไม่ค้นพบ (หรือยังไม่เคยพบ NPC) = โชว์เป็น placeholder เทาๆ แทนการเฉลยทันที
        // (กันไม่ให้เกมสปอยล์ของที่ชอบ/รักให้ผู้เล่นเห็นก่อนลองผิดลองถูกเอง)
        FillGiftGrid(_likedGiftPool, hasMet ? def.favoriteGifts : null, isLoved: false, hasMet ? entry.DiscoveredLikedMask : (ushort)0);
        FillGiftGrid(_lovedGiftPool, hasMet ? def.lovedGifts    : null, isLoved: true,  hasMet ? entry.DiscoveredLovedMask : (ushort)0);
    }

    /// <summary>เติม slot คงที่ GiftSlotsPerSide ช่องเสมอ — เฉลยไอคอนเฉพาะชิ้นที่ bit ตรงใน discoveredMask ที่เหลือ SetEmpty (placeholder เทาๆ)</summary>
    private void FillGiftGrid(UIListPool<FavoriteGiftItemUI> pool, List<ItemSO> gifts, bool isLoved, int discoveredMask)
    {
        for (int i = 0; i < GiftSlotsPerSide; i++)
        {
            ItemSO item = GetGiftAt(gifts, i);
            bool discovered = (discoveredMask & (1 << i)) != 0;
            var slot = pool.GetOrCreate(i);
            if (item != null && discovered) slot.Setup(item, isLoved);
            else slot.SetEmpty();
        }
        pool.ReleaseExtra(GiftSlotsPerSide);

        if (gifts != null && CountNonNull(gifts) > GiftSlotsPerSide)
            Debug.LogWarning($"[RelationshipTabView] มีของมากกว่า {GiftSlotsPerSide} ชิ้นใน list นี้ — ชิ้นที่เกินจะไม่ถูกแสดง (เพิ่ม slot ใน Figma/Grid ถ้าต้องการโชว์ครบ)");
    }

    /// <summary>หาไอเทมชิ้นที่ index ใน list โดยข้าม entry ที่เป็น null (ช่องว่างใน Inspector)</summary>
    private static ItemSO GetGiftAt(List<ItemSO> gifts, int index)
    {
        if (gifts == null) return null;
        int found = -1;
        foreach (var g in gifts)
        {
            if (g == null) continue;
            found++;
            if (found == index) return g;
        }
        return null;
    }

    private static int CountNonNull(List<ItemSO> gifts)
    {
        int count = 0;
        foreach (var g in gifts)
            if (g != null) count++;
        return count;
    }
}
