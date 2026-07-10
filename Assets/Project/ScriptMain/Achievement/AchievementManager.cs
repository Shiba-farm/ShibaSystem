using Unity.Netcode;
using UnityEngine;

/// <summary>
/// เก็บของสะสมที่ผู้เล่นคนเดียวค้นพบแล้ว — เก็บเฉพาะที่ "ค้นพบแล้ว" เท่านั้น
/// (sparse) ของที่ยังไม่เคยพบจะไม่อยู่ใน NetworkList และ UI จะโชว์เป็น "???"
/// รองรับของสะสมจำนวนมากในอนาคตโดย NetworkList ไม่บวมตาม
///
/// จุดสำคัญ (decoupling): ระบบ gameplay อื่น ๆ (ตกปลา/ขุดเหมือง/ปลูกพืช/คราฟต์)
/// ไม่จำเป็นต้องรู้จัก collectibleId เลย — แค่เรียก ReportItemObtained(itemID)
/// ตอนผู้เล่นเก็บ/คราฟต์ไอเทมได้สำเร็จ ระบบนี้จะ map itemID → collectible เอง
/// ผ่าน CollectibleDatabaseSO (Observer / SOLID — achievement ไม่ผูกกับระบบใดระบบหนึ่ง)
/// </summary>
public class AchievementManager : NetworkSaveableBehaviour
{
    [SerializeField] private AchievementDataSignal connectionSignal;
    [SerializeField] private CollectibleDatabaseSO collectibleDatabase;

    public NetworkList<NetworkDiscoveryEntry> Discoveries;
    public override bool IsPlayerSaveable => true;

    private void Awake() => Discoveries = new NetworkList<NetworkDiscoveryEntry>();

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer) SaveLoadManager.Instance?.Register(this);
        if (IsOwner) connectionSignal.UpdateAchievementData(this);
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        if (IsServer) SaveLoadManager.Instance?.Unregister(this);
    }

    public bool IsDiscovered(int collectibleId)
    {
        foreach (var d in Discoveries)
            if (d.CollectibleId == collectibleId) return true;
        return false;
    }

    /// <summary>เรียกจาก server เมื่อผู้เล่นเก็บ/คราฟต์ ItemSO ได้สำเร็จ — ไม่ต้องรู้จัก collectibleId</summary>
    public void ReportItemObtained(int itemId)
    {
        if (!IsServer || collectibleDatabase == null) return;

        CollectibleDefinitionSO def = collectibleDatabase.GetByLinkedItemID(itemId);
        if (def != null) ReportDiscovery(def.collectibleId);
    }

    public void ReportDiscovery(int collectibleId)
    {
        if (!IsServer || IsDiscovered(collectibleId)) return;
        Discoveries.Add(new NetworkDiscoveryEntry { CollectibleId = collectibleId });
    }

    // DEBUG ชั่วคราว — ยังไม่มีระบบเกมจริงเรียก ReportItemObtained เลย (ตกปลา/ขุดเหมือง/
    // ปลูกพืช/คราฟต์ยังไม่ผูกเข้ามา) ใช้ทดสอบหน้าจอ Achievement เท่านั้น: ระหว่าง Play คลิกขวา
    // ที่หัวข้อ component "Achievement Manager" ใน Inspector ของผู้เล่น (ต้องเป็นเครื่อง
    // Host/Server) แล้วเลือกเมนูที่ต้องการ — ลบออกทีหลังเมื่อระบบเกมเรียก ReportItemObtained จริงแล้ว
    [ContextMenu("DEBUG: Discover Next Collectible")]
    private void DebugDiscoverNext()
    {
        if (collectibleDatabase == null) return;
        foreach (var def in collectibleDatabase.AllCollectibles)
        {
            if (def == null || IsDiscovered(def.collectibleId)) continue;
            ReportDiscovery(def.collectibleId);
            return;
        }
    }

    [ContextMenu("DEBUG: Discover All Collectibles")]
    private void DebugDiscoverAll()
    {
        if (collectibleDatabase == null) return;
        foreach (var def in collectibleDatabase.AllCollectibles)
            if (def != null) ReportDiscovery(def.collectibleId);
    }

    [ContextMenu("DEBUG: Reset All Discoveries")]
    private void DebugResetDiscoveries()
    {
        if (!IsServer) return;
        Discoveries.Clear();
    }

    public override void CaptureState(GameSaveData save, ulong clientId = 0)
    {
        var playerData = save.GetOrCreatePlayer(OwnerClientId);
        playerData.discoveredCollectibleIds.Clear();
        foreach (var d in Discoveries)
            playerData.discoveredCollectibleIds.Add(d.CollectibleId);
    }

    public override void RestoreState(GameSaveData save, ulong clientId = 0)
    {
        if (!IsServer) return;
        var playerData = save.FindPlayer(OwnerClientId);
        if (playerData == null) return;

        Discoveries.Clear();
        foreach (var id in playerData.discoveredCollectibleIds)
            Discoveries.Add(new NetworkDiscoveryEntry { CollectibleId = id });
    }
}
