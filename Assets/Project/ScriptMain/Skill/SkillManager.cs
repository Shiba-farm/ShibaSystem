using Unity.Netcode;
using UnityEngine;

/// <summary>
/// เก็บสกิลที่ปลดล็อกแล้วของผู้เล่นคนเดียว — เก็บเฉพาะสกิลที่ Level > 0 เท่านั้น
/// (sparse) สกิลที่ยังไม่ปลดล็อกจะไม่อยู่ใน NetworkList เลย รองรับสกิลจำนวนมาก
/// ในอนาคตได้โดย NetworkList ไม่บวมตาม
/// </summary>
public class SkillManager : NetworkSaveableBehaviour
{
    [SerializeField] private SkillDataSignal connectionSignal;
    [SerializeField] private SkillDatabaseSO skillDatabase;

    public NetworkList<NetworkSkillEntry> Skills;
    public NetworkVariable<int> SkillPoints = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public override bool IsPlayerSaveable => true;

    private void Awake() => Skills = new NetworkList<NetworkSkillEntry>();

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer) SaveLoadManager.Instance?.Register(this);
        if (IsOwner) connectionSignal.UpdateSkillData(this);
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        if (IsServer) SaveLoadManager.Instance?.Unregister(this);
    }

    public int GetLevel(int skillId)
    {
        foreach (var s in Skills)
            if (s.SkillId == skillId) return s.Level;
        return 0;
    }

    public int GetCategoryLevel(SkillCategory category)
    {
        int total = 0;
        if (skillDatabase == null) return 0;
        foreach (var def in skillDatabase.GetByCategory(category))
            total += GetLevel(def.skillId);
        return total;
    }

    public bool CanUpgrade(SkillDefinitionSO def)
    {
        if (def == null) return false;
        int currentLevel = GetLevel(def.skillId);
        if (currentLevel >= def.maxLevel) return false;
        if (SkillPoints.Value < def.skillPointCostPerLevel) return false;
        if (def.requiredSkill != null && GetLevel(def.requiredSkill.skillId) < def.requiredSkillLevel) return false;
        return true;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestUpgradeSkillServerRpc(int skillId, RpcParams rpcParams = default)
    {
        // กัน client เรียก Rpc นี้บน SkillManager ของผู้เล่นคนอื่น
        if (OwnerClientId != rpcParams.Receive.SenderClientId) return;

        SkillDefinitionSO def = skillDatabase.GetByID(skillId);
        if (!CanUpgrade(def)) return;

        SkillPoints.Value -= def.skillPointCostPerLevel;

        for (int i = 0; i < Skills.Count; i++)
        {
            if (Skills[i].SkillId != skillId) continue;
            var entry = Skills[i];
            entry.Level++;
            Skills[i] = entry;
            return;
        }

        Skills.Add(new NetworkSkillEntry { SkillId = skillId, Level = 1 });
    }

    /// <summary>เรียกจาก gameplay (ขึ้นเลเวลตัวละคร, ทำเควสจบ ฯลฯ) เพื่อให้แต้มสกิล</summary>
    public void GrantSkillPoints(int amount)
    {
        if (!IsServer) return;
        SkillPoints.Value += amount;
    }

    // DEBUG ชั่วคราว — ยังไม่มีระบบเกมจริงที่ให้ Skill Point (เควส/เลเวลอัพ ฯลฯ
    // ยังไม่ถูกผูกเข้ามา) ใช้ทดสอบหน้าจอ Upgrade เท่านั้น: ระหว่าง Play คลิกขวาที่
    // หัวข้อ component "Skill Manager" ใน Inspector ของผู้เล่น (ต้องเป็นเครื่อง
    // Host/Server) แล้วเลือกเมนูนี้ — ลบออกทีหลังเมื่อมีระบบให้แต้มจริงแล้ว
    [ContextMenu("DEBUG: Grant 10 Skill Points")]
    private void DebugGrant10SkillPoints() => GrantSkillPoints(10);

    public override void CaptureState(GameSaveData save, ulong clientId = 0)
    {
        var playerData = save.GetOrCreatePlayer(OwnerClientId);
        playerData.skills.Clear();
        foreach (var s in Skills)
            playerData.skills.Add(new SkillSaveData { skillId = s.SkillId, level = s.Level, unlocked = s.Level > 0 });
        playerData.skillPoints = SkillPoints.Value;
    }

    public override void RestoreState(GameSaveData save, ulong clientId = 0)
    {
        if (!IsServer) return;
        var playerData = save.FindPlayer(OwnerClientId);
        if (playerData == null) return;

        Skills.Clear();
        foreach (var saved in playerData.skills)
            if (saved.level > 0) Skills.Add(new NetworkSkillEntry { SkillId = saved.skillId, Level = saved.level });
        SkillPoints.Value = playerData.skillPoints;
    }
}
