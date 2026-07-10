using Unity.Netcode;
using UnityEngine;

/// <summary>
/// เก็บ progress เควสของผู้เล่นคนเดียว — เก็บเฉพาะเควสที่ Active หรือ Completed
/// แล้วเท่านั้น (sparse list) เควสที่ NotStarted จะไม่อยู่ใน NetworkList เลย ทำให้
/// รองรับเควสนิยามหลักร้อยใน QuestDatabaseSO โดย NetworkList ไม่บวมตาม
///
/// วางบน Player prefab จุดเดียวกับ InventoryData/StatManager/EquipmentData
/// ระบบ gameplay อื่น (NPC, trigger, dialogue) เรียก StartQuest/AddProgress/CompleteQuest
/// ได้ตรง ๆ จาก server เท่านั้น — ฝั่ง client ขอแค่ "เลิกเควสเสริม" ผ่าน Rpc
/// </summary>
public class QuestManager : NetworkSaveableBehaviour
{
    [SerializeField] private QuestDataSignal connectionSignal;
    [SerializeField] private QuestDatabaseSO questDatabase;

    public NetworkList<NetworkQuestEntry> Quests;
    public override bool IsPlayerSaveable => true;

    private void Awake()
    {
        Quests = new NetworkList<NetworkQuestEntry>();
        // หมายเหตุ: ไม่ใช้ GetComponent<InventoryData>() เพราะ InventoryData อยู่บน
        // child GO คนละอัน — ใช้ InventoryDataRegistry.GetByOwnerAndID แทนตอน GrantRewards
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer) SaveLoadManager.Instance?.Register(this);
        if (IsOwner) connectionSignal.UpdateQuestData(this);
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        if (IsServer) SaveLoadManager.Instance?.Unregister(this);
    }

    // ── Queries ──────────────────────────────────────────────────────────────
    public QuestStatus GetStatus(int questId)
    {
        foreach (var q in Quests)
            if (q.QuestId == questId) return q.Status;
        return QuestStatus.NotStarted;
    }

    public int GetProgress(int questId)
    {
        foreach (var q in Quests)
            if (q.QuestId == questId) return q.Progress;
        return 0;
    }

    /// <summary>เควสนี้เริ่มได้หรือยัง — ต้องไม่ Active/Completed อยู่ และเควสนำหน้าต้องจบหมดแล้ว</summary>
    public bool CanStart(QuestDefinitionSO definition)
    {
        if (definition == null) return false;
        if (GetStatus(definition.questId) != QuestStatus.NotStarted) return false;

        foreach (var prereq in definition.prerequisiteQuests)
            if (prereq != null && GetStatus(prereq.questId) != QuestStatus.Completed) return false;

        return true;
    }

    // ── Server-only mutation API — เรียกจาก gameplay code (NPC, trigger, dialogue) ──
    public void StartQuest(int questId)
    {
        if (!IsServer) return;
        if (GetStatus(questId) != QuestStatus.NotStarted) return;

        Quests.Add(new NetworkQuestEntry { QuestId = questId, Status = QuestStatus.Active, Progress = 0 });
    }

    public void AddProgress(int questId, int amount = 1)
    {
        if (!IsServer) return;

        for (int i = 0; i < Quests.Count; i++)
        {
            if (Quests[i].QuestId != questId || Quests[i].Status != QuestStatus.Active) continue;

            var entry = Quests[i];
            entry.Progress += amount;
            Quests[i] = entry;

            QuestDefinitionSO def = questDatabase.GetByID(questId);
            if (def != null && entry.Progress >= def.targetProgress)
                CompleteQuest(questId);
            return;
        }
    }

    public void CompleteQuest(int questId) => CompleteQuestInternal(questId, grantRewards: true);

    /// <summary>
    /// Complete quest แต่ไม่ grant rewards อีกรอบ
    /// ใช้เมื่อ player รับของทีละชิ้นผ่าน UI ไปแล้ว (ป้องกัน double-grant)
    /// </summary>
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    public void DebugCompleteQuestNoRewards(int questId) => CompleteQuestInternal(questId, grantRewards: false);
#endif

    private void CompleteQuestInternal(int questId, bool grantRewards)
    {
        if (!IsServer) return;

        for (int i = 0; i < Quests.Count; i++)
        {
            if (Quests[i].QuestId != questId) continue;

            var entry = Quests[i];
            entry.Status = QuestStatus.Completed;
            Quests[i] = entry;

            if (grantRewards) GrantRewards(questId);
            return;
        }
    }

    private void GrantRewards(int questId)
    {
        QuestDefinitionSO def = questDatabase.GetByID(questId);
        if (def == null) return;

        // GetComponent<InventoryData>() ไม่ทำงาน — InventoryData อยู่บน child GO คนละตัว
        // ใช้ Registry ค้นหา main inventory (ID=0) ของ owner คนนี้แทน
        InventoryData inv = InventoryDataRegistry.GetByOwnerAndID(OwnerClientId, 0);
        if (inv == null)
        {
            Debug.LogError($"[QuestManager] GrantRewards: InventoryData (ID=0) ไม่พบสำหรับ owner {OwnerClientId}");
            return;
        }

        Debug.Log($"[QuestManager] GrantRewards: เริ่มมอบรางวัล {def.rewards.Count} ชิ้น จาก '{def.title}'");
        foreach (var reward in def.rewards)
        {
            if (reward.item == null)
            {
                Debug.LogWarning($"[QuestManager] GrantRewards: reward.item เป็น null — ข้าม");
                continue;
            }
            Debug.Log($"[QuestManager] GrantRewards: AddItem '{reward.item.name}' (itemID={reward.item.itemID}) x{reward.amount}");
            inv.AddItem(reward.item.itemID, reward.amount);
        }
        Debug.Log($"[QuestManager] GrantRewards: เสร็จสิ้น — ตรวจ log 'Stacked' หรือ 'Filled empty slot' ด้านบน");

        // มอบรางวัลเงิน
        if (def.moneyReward > 0)
        {
            if (CurrencyManager.Instance != null)
            {
                CurrencyManager.Instance.AddCurrencyOnServer(def.moneyReward);
                Debug.Log($"[QuestManager] GrantRewards: มอบเงิน {def.moneyReward} จากเควส '{def.title}'");
            }
            else
            {
                Debug.LogWarning("[QuestManager] GrantRewards: CurrencyManager.Instance เป็น null — ไม่สามารถมอบเงินได้");
            }
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestAbandonSideQuestServerRpc(int questId, RpcParams rpcParams = default)
    {
        // กัน client เรียก Rpc นี้บน QuestManager ของผู้เล่นคนอื่น
        if (OwnerClientId != rpcParams.Receive.SenderClientId) return;

        QuestDefinitionSO def = questDatabase.GetByID(questId);
        if (def == null || def.category != QuestCategory.Side) return;

        for (int i = 0; i < Quests.Count; i++)
        {
            if (Quests[i].QuestId != questId) continue;
            if (Quests[i].Status == QuestStatus.Active) Quests.RemoveAt(i);
            return;
        }
    }

    // ── Save / Load ───────────────────────────────────────────────────────────
    public override void CaptureState(GameSaveData save, ulong clientId = 0)
    {
        var playerData = save.GetOrCreatePlayer(OwnerClientId);
        playerData.quests.Clear();
        foreach (var q in Quests)
            playerData.quests.Add(new QuestSaveData { questId = q.QuestId, status = q.Status, progress = q.Progress });
    }

    public override void RestoreState(GameSaveData save, ulong clientId = 0)
    {
        if (!IsServer) return;
        var playerData = save.FindPlayer(OwnerClientId);
        if (playerData == null) return;

        Quests.Clear();
        foreach (var saved in playerData.quests)
            Quests.Add(new NetworkQuestEntry { QuestId = saved.questId, Status = saved.status, Progress = saved.progress });
    }
}
