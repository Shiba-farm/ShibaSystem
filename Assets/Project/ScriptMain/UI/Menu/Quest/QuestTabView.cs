using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// แท็บ Quest — ลิสต์ฝั่งซ้ายแบ่ง 3 กลุ่ม (Main / Side / Completed)
/// ฝั่งขวาโชว์รายละเอียด + ของรางวัลเสมอ
///
/// [DEBUG] กด F2 ขณะ Quest tab เปิดอยู่ → StartQuest + CompleteQuest + GrantRewards ทันที
///         ใช้ทดสอบว่าของรางวัลเข้า Inventory ถูกต้องหรือไม่
///         ทำงานเฉพาะ Editor / Development Build เท่านั้น
/// </summary>
public class QuestTabView : MonoBehaviour, IMenuTabView
{
    [Header("Data")]
    [SerializeField] private QuestDataSignal connectionSignal;
    [SerializeField] private QuestDatabaseSO questDatabase;

    [Header("List — Main / Side / Completed")]
    [SerializeField] private QuestListRowUI rowPrefab;
    [SerializeField] private Transform mainListContainer;
    [SerializeField] private Transform sideListContainer;
    [SerializeField] private Transform completedListContainer;

    [Header("Detail Panel")]
    [SerializeField] private TextMeshProUGUI detailTitleText;
    [SerializeField] private Image detailLogoImage;
    [SerializeField] private TextMeshProUGUI detailDescriptionText;
    [SerializeField] private GameObject detailPanelRoot;

    [Header("Rewards")]
    [SerializeField] private QuestRewardItemUI rewardPrefab;
    [SerializeField] private Transform rewardContainer;

    [Header("Money Reward")]
    [Tooltip("GameObject ที่ครอบ UI เงินรางวัล — ซ่อนเมื่อ moneyReward = 0")]
    [SerializeField] private GameObject moneyRewardRoot;
    [Tooltip("Text แสดงจำนวนเงิน เช่น '$ 1,000'")]
    [SerializeField] private TextMeshProUGUI moneyRewardText;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    [Header("Debug — ติ๊กเปิดเพื่อทดสอบการรับรางวัลทีละชิ้น")]
    [Tooltip("เมื่อเปิด: คลิกที่ Reward Slot → ของเข้า Inventory ทีละชิ้น\n" +
             "F2 ยังใช้ได้เสมอ: Complete เควสที่เลือกทันที")]
    [SerializeField] private bool debugRewardClick = false;
#endif

    public MenuTabId TabId => MenuTabId.Quest;
    public bool IsInitialized { get; private set; }

    private UIListPool<QuestListRowUI>    _mainPool;
    private UIListPool<QuestListRowUI>    _sidePool;
    private UIListPool<QuestListRowUI>    _completedPool;
    private UIListPool<QuestRewardItemUI> _rewardPool;

    private QuestManager _activeManager;
    private bool _needsRefresh;
    private int  _selectedQuestId = -1;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    // เก็บ (questId, slotIndex) ที่ debug-click รับไปแล้ว — ป้องกัน icon กลับมาหลัง switch tab
    private readonly HashSet<(int questId, int slot)> _debugGrantedSlots = new();
#endif

    // ── IMenuTabView ─────────────────────────────────────────────────────────
    public void InitializeTab()
    {
        if (IsInitialized) return;
        IsInitialized = true;

        _mainPool      = new UIListPool<QuestListRowUI>(rowPrefab, mainListContainer);
        _sidePool      = new UIListPool<QuestListRowUI>(rowPrefab, sideListContainer);
        _completedPool = new UIListPool<QuestListRowUI>(rowPrefab, completedListContainer);
        _rewardPool    = new UIListPool<QuestRewardItemUI>(rewardPrefab, rewardContainer);

        connectionSignal.OnDataUpdate += HandleConnected;
        if (connectionSignal.CurrentData != null) HandleConnected(connectionSignal.CurrentData);
    }

    public void OnTabShown()  => _needsRefresh = true;
    public void OnTabHidden() { }

    private void OnDestroy()
    {
        connectionSignal.OnDataUpdate -= HandleConnected;
        if (_activeManager != null) _activeManager.Quests.OnListChanged -= HandleListChanged;
    }

    // ── Data binding ─────────────────────────────────────────────────────────
    private void HandleConnected(QuestManager manager)
    {
        if (_activeManager == manager) return;
        if (_activeManager != null) _activeManager.Quests.OnListChanged -= HandleListChanged;

        _activeManager = manager;
        if (_activeManager == null) return;

        _activeManager.Quests.OnListChanged += HandleListChanged;
        _needsRefresh = true;
    }

    private void HandleListChanged(NetworkListEvent<NetworkQuestEntry> evt) => _needsRefresh = true;

    private void LateUpdate()
    {
        if (!_needsRefresh) return;
        _needsRefresh = false;
        RefreshLists();
    }

    // ── List build ───────────────────────────────────────────────────────────
    private void RefreshLists()
    {
        if (_activeManager == null || questDatabase == null) return;

        int mainCount = 0, sideCount = 0, completedCount = 0;
        bool selectedStillVisible = false;

        foreach (var def in questDatabase.AllQuests)
        {
            if (def == null) continue;
            QuestStatus status = _activeManager.GetStatus(def.questId);

            if (status == QuestStatus.Completed)
            {
                BindRow(_completedPool.GetOrCreate(completedCount++), def);
            }
            else if (status == QuestStatus.Active || _activeManager.CanStart(def))
            {
                if (def.category == QuestCategory.Main)
                    BindRow(_mainPool.GetOrCreate(mainCount++), def);
                else
                    BindRow(_sidePool.GetOrCreate(sideCount++), def);
            }
            else continue;

            if (def.questId == _selectedQuestId) selectedStillVisible = true;
        }

        _mainPool.ReleaseExtra(mainCount);
        _sidePool.ReleaseExtra(sideCount);
        _completedPool.ReleaseExtra(completedCount);

        if (!selectedStillVisible)
        {
            QuestDefinitionSO fallback = questDatabase.AllQuests.Count > 0 ? questDatabase.AllQuests[0] : null;
            _selectedQuestId = fallback != null ? fallback.questId : -1;
        }

        UpdateSelectionHighlight();
        ShowDetail(_selectedQuestId);
    }

    private void BindRow(QuestListRowUI row, QuestDefinitionSO def)
    {
        row.Setup(def);
        row.OnClicked -= SelectQuest;
        row.OnClicked += SelectQuest;
    }

    private void SelectQuest(int questId)
    {
        _selectedQuestId = questId;
        UpdateSelectionHighlight();
        ShowDetail(questId);
    }

    private void UpdateSelectionHighlight()
    {
        foreach (var row in _mainPool.Instances)      row.SetSelected(row.QuestId == _selectedQuestId);
        foreach (var row in _sidePool.Instances)      row.SetSelected(row.QuestId == _selectedQuestId);
        foreach (var row in _completedPool.Instances) row.SetSelected(row.QuestId == _selectedQuestId);
    }

    // ── Detail panel ─────────────────────────────────────────────────────────
    private void ShowDetail(int questId)
    {
        QuestDefinitionSO def = questId >= 0 ? questDatabase.GetByID(questId) : null;
        if (detailPanelRoot != null) detailPanelRoot.SetActive(def != null);
        if (def == null) { _rewardPool.ReleaseAll(); return; }

        if (detailTitleText       != null) detailTitleText.text       = def.title;
        if (detailLogoImage       != null) detailLogoImage.sprite     = def.logo;
        if (detailDescriptionText != null) detailDescriptionText.text = def.description;

        // แสดง / ซ่อนรางวัลเงิน
        if (moneyRewardRoot != null)
            moneyRewardRoot.SetActive(def.moneyReward > 0);
        if (moneyRewardText != null && def.moneyReward > 0)
            moneyRewardText.text = $"$ {def.moneyReward:N0}";

        bool isCompleted = _activeManager != null &&
                           _activeManager.GetStatus(questId) == QuestStatus.Completed;

        if (def.rewards != null && def.rewards.Count > 0)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // เควส Complete แล้ว → ล้าง granted set (ไม่ต้องจำอีก)
            if (isCompleted) _debugGrantedSlots.RemoveWhere(s => s.questId == questId);
#endif
            int i = 0;
            foreach (var reward in def.rewards)
            {
                int slotIndex = i;               // capture ก่อน i++
                var rewardUI  = _rewardPool.GetOrCreate(i++);
                rewardUI.Setup(reward);

                if (isCompleted)
                {
                    // เควสจบแล้ว — slot background คงอยู่, icon หาย = "รับแล้ว"
                    rewardUI.SetGranted();
                }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                else if (_debugGrantedSlots.Contains((questId, slotIndex)))
                {
                    // slot นี้รับไปแล้วใน session นี้ — คงสถานะข้าม tab switch
                    rewardUI.SetGranted();
                }
                else if (debugRewardClick)
                {
                    // ติ๊ก debugRewardClick ใน Inspector → คลิกรับของทีละชิ้น
                    // เมื่อรับครบทุก slot → quest auto-complete (ไม่ grant ซ้ำ)
                    var capturedReward = reward;
                    var capturedUI     = rewardUI;
                    int capturedSlot   = slotIndex;
                    int capturedQId    = questId;
                    int totalRewards   = def.rewards.Count;
                    rewardUI.OnDebugClicked = () =>
                    {
                        // 1. Auto-start quest ถ้ายังไม่เริ่ม
                        if (_activeManager.GetStatus(capturedQId) == QuestStatus.NotStarted)
                            _activeManager.StartQuest(capturedQId);

                        // 2. ให้ของ + mark slot
                        DebugGrantSingleReward(capturedReward);
                        capturedUI.SetGranted();
                        _debugGrantedSlots.Add((capturedQId, capturedSlot));

                        // 3. ตรวจว่ารับครบทุก slot หรือยัง
                        bool allClaimed = true;
                        for (int s = 0; s < totalRewards; s++)
                        {
                            if (!_debugGrantedSlots.Contains((capturedQId, s)))
                            {
                                allClaimed = false;
                                break;
                            }
                        }

                        // 4. รับครบ → Complete quest (ไม่ grant item rewards ซ้ำ เพราะรับไปแล้วทีละชิ้น)
                        //    แต่ยังต้อง grant เงินแยกต่างหาก เพราะ DebugCompleteQuestNoRewards ข้าม GrantRewards
                        if (allClaimed && _activeManager.GetStatus(capturedQId) != QuestStatus.Completed)
                        {
                            _activeManager.DebugCompleteQuestNoRewards(capturedQId);

                            if (def.moneyReward > 0 && CurrencyManager.Instance != null)
                            {
                                CurrencyManager.Instance.AddCurrencyServerRpc(def.moneyReward);
                                Debug.Log($"[QuestDebug] Grant money: {def.moneyReward} จากเควส '{def.title}'");
                            }

                            Debug.Log($"[QuestDebug] รับรางวัลครบ → เควส '{def.title}' Completed");
                        }
                    };
                }
                else
                {
                    rewardUI.OnDebugClicked = null;
                }
#endif
            }
            _rewardPool.ReleaseExtra(i);
        }
        else
        {
            _rewardPool.ReleaseAll();
        }
    }

    // ── Debug ─────────────────────────────────────────────────────────────────
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    /// <summary>
    /// Grant ไอเทมจาก reward entry เข้า Inventory โดยตรง
    /// เรียกจาก ShowDetail เมื่อผู้เล่นคลิก reward slot ใน debug mode
    /// </summary>
    private void DebugGrantSingleReward(QuestRewardEntry reward)
    {
        if (_activeManager == null || reward.item == null)
        {
            Debug.LogWarning("[QuestDebug] DebugGrantSingleReward: ไม่มี QuestManager หรือ item เป็น null");
            return;
        }

        // หา main inventory (ID=0) ตาม owner — เหมือน QuestManager.GrantRewards
        InventoryData inv = InventoryDataRegistry.GetByOwnerAndID(_activeManager.OwnerClientId, 0);
        if (inv == null)
        {
            Debug.LogWarning($"[QuestDebug] DebugGrantSingleReward: ไม่พบ InventoryData สำหรับ owner {_activeManager.OwnerClientId}");
            return;
        }

        inv.AddItem(reward.item.itemID, reward.amount);
        Debug.Log($"[QuestDebug] Grant single reward: {reward.item.name} x{reward.amount} → Inventory (เควสยังไม่เสร็จ)");
    }
#endif
}
