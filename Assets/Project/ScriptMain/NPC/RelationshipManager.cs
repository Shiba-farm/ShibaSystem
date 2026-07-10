using Unity.Netcode;
using UnityEngine;

/// <summary>ระดับความชอบของ NPC ต่อของขวัญที่ได้รับ — ใช้ตัดสิน exp ที่ได้และคำขอบคุณที่พูด</summary>
public enum GiftReactionTier
{
    Normal, // ของทั่วไป ไม่อยู่ใน list ไหนเลย
    Liked,  // อยู่ใน favoriteGifts
    Loved   // อยู่ใน lovedGifts (สูงสุด)
}

/// <summary>
/// เก็บความสัมพันธ์ของผู้เล่นคนเดียวกับ NPC ทุกคนที่เคยพบ — เก็บเฉพาะ NPC ที่
/// "เคยพบแล้ว" เท่านั้น (sparse) NPC ที่ยังไม่เคยพบจะไม่มี entry เลย และ UI จะไม่
/// แสดงใน relationship list (ยังไม่รู้จัก)
/// </summary>
public class RelationshipManager : NetworkSaveableBehaviour
{
    [SerializeField] private RelationshipDataSignal connectionSignal;
    [SerializeField] private NPCDatabaseSO npcDatabase;
    [SerializeField] private int expPerNormalGift = 10;
    [SerializeField] private int expPerFavoriteGift = 30;
    [Tooltip("exp ที่ได้ตอนให้ของที่ NPC \"รัก\" (lovedGifts) — ควรมากกว่า Exp Per Favorite Gift")]
    [SerializeField] private int expPerLovedGift = 50;
    [SerializeField] private int expPerHeartLevel = 100;
    [Tooltip("จำนวนของขวัญสูงสุดที่ให้ NPC คนเดียวกันได้ต่อวัน — กันสแปมให้ของขวัญรัวๆ")]
    [SerializeField] private int maxGiftsPerDay = 3;

    public NetworkList<NetworkRelationshipEntry> Relationships;
    public override bool IsPlayerSaveable => true;

    private void Awake() => Relationships = new NetworkList<NetworkRelationshipEntry>();

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer) SaveLoadManager.Instance?.Register(this);
        if (IsOwner) connectionSignal.UpdateRelationshipData(this);
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        if (IsServer) SaveLoadManager.Instance?.Unregister(this);
    }

    public bool HasMet(int npcId)
    {
        foreach (var r in Relationships)
            if (r.NpcId == npcId) return true;
        return false;
    }

    public NetworkRelationshipEntry GetEntry(int npcId)
    {
        foreach (var r in Relationships)
            if (r.NpcId == npcId) return r;
        return default;
    }

    /// <summary>เรียกจาก server-side code โดยตรง (เช่น NPC trigger ที่รันบน server)</summary>
    public void MeetNPC(int npcId)
    {
        if (!IsServer || HasMet(npcId)) return;
        Relationships.Add(new NetworkRelationshipEntry { NpcId = npcId, HeartLevel = 0, HeartExp = 0 });
    }

    /// <summary>เรียกจาก client ผ่าน NPCInteractable ตอนผู้เล่นคุยกับ NPC ครั้งแรก</summary>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestMeetNPCServerRpc(int npcId, RpcParams rpcParams = default)
    {
        // ป้องกัน client เรียก RPC นี้บน RelationshipManager ของผู้เล่นคนอื่น
        if (OwnerClientId != rpcParams.Receive.SenderClientId) return;
        MeetNPC(npcId);
    }

    /// <summary>
    /// เรียกจาก client ตอนคลิกขวาที่ NPC เพื่อให้ของขวัญด้วยไอเทมที่ถืออยู่
    /// เช็คของจริงในกระเป๋าฝั่ง server ก่อนหักออก 1 ชิ้น กันโกง (ไม่เชื่อ client เฉยๆ)
    /// </summary>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestGiveGiftServerRpc(int npcId, int itemId, RpcParams rpcParams = default)
    {
        // ป้องกัน client เรียก RPC นี้บน RelationshipManager ของผู้เล่นคนอื่น
        if (OwnerClientId != rpcParams.Receive.SenderClientId) return;

        ulong senderClientId = rpcParams.Receive.SenderClientId;

        MeetNPC(npcId); // ต้องมี entry ก่อน ถึงจะเช็ค/บันทึกจำนวนของขวัญรายวันได้

        if (!CanGiveGiftToday(npcId))
        {
            Debug.LogWarning($"[RelationshipManager] RequestGiveGiftServerRpc: NPC {npcId} ได้รับของขวัญครบ {maxGiftsPerDay} ครั้งแล้ววันนี้ — ปฏิเสธ (กันสแปม)");
            return;
        }

        // หาไอเทมจาก "ทุกกระเป๋า" ของผู้เล่นคนนี้ (กระเป๋าหลัก id=0 และ hotbar id=1 เป็นคนละ storage กัน —
        // ถ้าไอเทมที่ถืออยู่ถูกลากไปวางในช่อง hotbar แล้ว มันจะย้ายไปอยู่ที่ inventoryID=1 จริงๆ ไม่ใช่แค่ view เดิม
        // เช็คแค่ id=0 อย่างเดียวเลยหาไม่เจอทั้งๆ ที่ผู้เล่นถืออยู่จริง)
        var ownerInventories = InventoryDataRegistry.GetAllByOwner(OwnerClientId);
        InventoryData inv = ownerInventories?.Find(d => d.GetItemCount(itemId) > 0);

        if (inv == null)
        {
            Debug.LogWarning($"[RelationshipManager] RequestGiveGiftServerRpc: ไม่มีไอเทม itemId={itemId} ในกระเป๋าไหนเลย — ปฏิเสธ");
            return;
        }

        inv.RemoveItem(itemId, 1);
        GiftReactionTier tier = GiveGift(npcId, itemId);
        RecordGiftGiven(npcId);

        // แจ้งกลับไปที่ client ที่ให้ของขวัญเท่านั้น (ไม่ใช่ broadcast ทุกคน) ให้ NPC โชว์ท่าทาง/คำขอบคุณ
        GiftResultClientRpc(npcId, tier, RpcTarget.Single(senderClientId, RpcTargetUse.Temp));
    }

    /// <summary>ให้ของขวัญ NPC — เพิ่ม exp ตามระดับความชอบ (Normal/Liked/Loved) — คืนค่าระดับที่ให้ไป</summary>
    public GiftReactionTier GiveGift(int npcId, int itemId)
    {
        if (!IsServer) return GiftReactionTier.Normal;
        MeetNPC(npcId);

        if (npcDatabase == null)
        {
            Debug.LogWarning("[RelationshipManager] GiveGift: ยังไม่ได้ผูก Npc Database (NPCDatabaseSO) ใน Inspector ของ RelationshipManager component — ลาก Assets/Project/ScriptableObjects/NPC/NPCDatabase.asset ใส่ให้ด้วยครับ");
            return GiftReactionTier.Normal;
        }

        NPCDefinitionSO def = npcDatabase.GetByID(npcId);
        if (def == null) return GiftReactionTier.Normal;

        // เช็ค "รัก" (lovedGifts) ก่อนเสมอ — ถ้าไอเทมเดียวกันอยู่ทั้งสอง list ให้ถือเป็น Loved
        int lovedIndex = def.IndexOfLovedGift(itemId);
        int likedIndex = lovedIndex < 0 ? def.IndexOfFavoriteGift(itemId) : -1;
        bool isLoved = lovedIndex >= 0;
        bool isLiked = !isLoved && likedIndex >= 0;

        GiftReactionTier tier = isLoved ? GiftReactionTier.Loved
                               : isLiked ? GiftReactionTier.Liked
                               : GiftReactionTier.Normal;

        int gain = tier switch
        {
            GiftReactionTier.Loved => expPerLovedGift,
            GiftReactionTier.Liked => expPerFavoriteGift,
            _ => expPerNormalGift
        };

        for (int i = 0; i < Relationships.Count; i++)
        {
            if (Relationships[i].NpcId != npcId) continue;

            var entry = Relationships[i];
            entry.HeartExp += gain;
            while (entry.HeartExp >= expPerHeartLevel && entry.HeartLevel < def.maxHeartLevel)
            {
                entry.HeartExp -= expPerHeartLevel;
                entry.HeartLevel++;
            }
            if (entry.HeartLevel >= def.maxHeartLevel) entry.HeartExp = 0;

            // จำว่าค้นพบของชิ้นนี้แล้ว (ให้ตรง tier จริง) — UI จะเฉลยไอคอนเฉพาะช่องที่ค้นพบแล้วเท่านั้น
            if (isLoved && lovedIndex < 16) entry.DiscoveredLovedMask |= (ushort)(1 << lovedIndex);
            else if (isLiked && likedIndex < 16) entry.DiscoveredLikedMask |= (ushort)(1 << likedIndex);

            Relationships[i] = entry;

            Debug.Log($"[RelationshipManager] GiveGift: NPC {npcId} ได้รับของ tier={tier} (+{gain} exp) → " +
                      $"HeartLevel={entry.HeartLevel}/{def.maxHeartLevel}  HeartExp={entry.HeartExp}/{expPerHeartLevel}");
            break;
        }

        return tier;
    }

    /// <summary>เช็คว่ายังให้ของขวัญ NPC คนนี้ได้อีกไหมในวันนี้ (ไม่เกิน maxGiftsPerDay ครั้ง)</summary>
    private bool CanGiveGiftToday(int npcId)
    {
        int today = WorldTimeManager.Instance != null ? WorldTimeManager.Instance.AbsoluteDayIndex : 0;
        for (int i = 0; i < Relationships.Count; i++)
        {
            if (Relationships[i].NpcId != npcId) continue;
            var e = Relationships[i];
            if (e.LastGiftDay != today) return true; // วันใหม่ นับใหม่
            return e.GiftsGivenToday < maxGiftsPerDay;
        }
        return true; // ไม่ควรเกิด (เรียก MeetNPC ไปก่อนแล้ว) — ถ้าไม่เจอ entry จริงๆ ก็ปล่อยผ่าน
    }

    /// <summary>บันทึกว่าเพิ่งให้ของขวัญ NPC คนนี้ไป 1 ครั้ง — reset ตัวนับถ้าเป็นวันใหม่</summary>
    private void RecordGiftGiven(int npcId)
    {
        int today = WorldTimeManager.Instance != null ? WorldTimeManager.Instance.AbsoluteDayIndex : 0;
        for (int i = 0; i < Relationships.Count; i++)
        {
            if (Relationships[i].NpcId != npcId) continue;

            var e = Relationships[i];
            if (e.LastGiftDay != today)
            {
                e.LastGiftDay = today;
                e.GiftsGivenToday = 0;
            }
            e.GiftsGivenToday++;
            Relationships[i] = e;
            return;
        }
    }

    /// <summary>
    /// แจ้งกลับ client ผู้ให้ของขวัญเท่านั้นว่า NPC ตัวไหนได้รับของขวัญไปแล้ว (สำเร็จเสมอ ณ จุดที่เรียก)
    /// เพื่อให้ NPCInteractable โชว์ท่าทางดีใจ + คำขอบคุณ ฝั่ง client แบบ local (ไม่ต้อง sync ให้ทุกคนเห็น)
    /// </summary>
    [Rpc(SendTo.SpecifiedInParams)]
    private void GiftResultClientRpc(int npcId, GiftReactionTier tier, RpcParams rpcParams = default)
    {
        NPCInteractable npc = NPCInteractable.FindById(npcId);
        npc?.ReactToGift(tier);
    }

    public override void CaptureState(GameSaveData save, ulong clientId = 0)
    {
        var playerData = save.GetOrCreatePlayer(OwnerClientId);
        playerData.relationships.Clear();
        foreach (var r in Relationships)
            playerData.relationships.Add(new RelationshipSaveData
            {
                npcId = r.NpcId,
                heartLevel = r.HeartLevel,
                heartExp = r.HeartExp,
                lastGiftDay = r.LastGiftDay,
                giftsGivenToday = r.GiftsGivenToday,
                discoveredLikedMask = r.DiscoveredLikedMask,
                discoveredLovedMask = r.DiscoveredLovedMask
            });
    }

    public override void RestoreState(GameSaveData save, ulong clientId = 0)
    {
        if (!IsServer) return;
        var playerData = save.FindPlayer(OwnerClientId);
        if (playerData == null) return;

        Relationships.Clear();
        foreach (var saved in playerData.relationships)
            Relationships.Add(new NetworkRelationshipEntry
            {
                NpcId = saved.npcId,
                HeartLevel = saved.heartLevel,
                HeartExp = saved.heartExp,
                LastGiftDay = saved.lastGiftDay,
                GiftsGivenToday = saved.giftsGivenToday,
                DiscoveredLikedMask = (ushort)saved.discoveredLikedMask,
                DiscoveredLovedMask = (ushort)saved.discoveredLovedMask
            });
    }
}
