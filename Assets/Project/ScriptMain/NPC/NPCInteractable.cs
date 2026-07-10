using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ควบคุมการ Interact กับ NPC ในโลก 3D
/// เมื่อผู้เล่นคลิกซ้ายที่ตัว NPC (ผ่าน NPCMouseInteractionUI.RequestTalk) → เริ่ม Dialogue และ MeetNPC ครั้งแรกถ้ายังไม่เคยพบ
///
/// วิธีติดตั้ง:
///   1. ติด script นี้บน GameObject ของ NPC ในฉาก
///   2. ผูก dialogueData (DialogueSO) → บทพูดของ NPC
///   3. ผูก npcDefinition (NPCDefinitionSO) → ข้อมูล NPC สำหรับระบบ Relationship
///   4. ผูก relationshipSignal (RelationshipDataSignal) → Signal เชื่อม RelationshipManager
/// </summary>
public class NPCInteractable : MonoBehaviour
{
    [Header("Dialogue")]
    public string characterName = "NPC";
    public DialogueSO dialogueData;
    public float interactRadius = 3f;

    [Header("Relationship")]
    [Tooltip("NPCDefinitionSO ของ NPC คนนี้ — ใช้สำหรับบันทึกการพบใน RelationshipManager")]
    public NPCDefinitionSO npcDefinition;
    [Tooltip("RelationshipDataSignal (ScriptableObject) — ลาก CurrencySignal ตัวเดียวกับที่ใช้ใน RelationshipTabView")]
    public RelationshipDataSignal relationshipSignal;

    [Header("Visual Prompt (Optional)")]
    public GameObject interactIcon;

    [Header("Greeting Animation")]
    [Tooltip("Animator ของโมเดล NPC — ถ้าไม่ลากผูกไว้ จะพยายามหาอัตโนมัติจากลูกของ GameObject นี้")]
    public Animator npcAnimator;
    [Tooltip("ชื่อ Trigger parameter ใน Animator Controller ที่จะสั่งเล่นตอนผู้เล่นเดินเข้าระยะคุยได้ " +
             "(ต้องไปสร้าง Parameter + State + Transition ใน Animator Controller ของ NPC เองก่อน)")]
    public string greetTrigger = "Greet";

    private bool _wasInRangeForGreet; // rising-edge เฉพาะทริกเกอร์ทักทาย แยกจาก hysteresis ของ IsPlayerInRange()

    [Header("Gift Reaction")]
    [Tooltip("ชื่อ Trigger parameter ที่จะสั่งเล่นตอน NPC ได้รับของขวัญ (ต้องสร้าง State/Transition เองใน Animator Controller เหมือน Greet)")]
    public string thankYouTrigger = "Happy";
    [Tooltip("ข้อความขอบคุณทั่วไป — สุ่มแสดง 1 ประโยคตอนได้รับของขวัญธรรมดา (ไม่อยู่ในลิสต์ชอบ/รัก)")]
    public string[] thankYouLines = { "ขอบคุณนะ!" };
    [Tooltip("ข้อความขอบคุณระดับ \"ชอบ\" — สุ่มแสดง 1 ประโยคตอนได้รับของที่อยู่ใน favoriteGifts")]
    public string[] favoriteThankYouLines = { "โห นี่ของที่ฉันชอบเลย ขอบคุณนะ!" };
    [Tooltip("ข้อความขอบคุณระดับ \"รัก\" (สูงสุด) — สุ่มแสดง 1 ประโยคตอนได้รับของที่อยู่ใน lovedGifts")]
    public string[] lovedThankYouLines = { "โอ้โห! นี่คือของที่ฉันรักที่สุดเลย ขอบคุณมากๆ จริงๆ นะ!" };

    [Header("Range Hysteresis")]
    [Tooltip("ระยะกันชนตอนออกจากระยะ — ป้องกัน prompt/cursor กระพริบตอนผู้เล่นยืนขอบ Interact Radius พอดี " +
             "(เข้าระยะที่ interactRadius ปกติ แต่ต้องออกไปไกลกว่า interactRadius + ค่านี้ ถึงจะนับว่าออกจากระยะ)")]
    public float rangeHysteresis = 0.5f;

    /// <summary>Npc Id ของ NPC คนนี้ — ใช้โดยระบบคลิกเมาส์/hover ภายนอก</summary>
    public int NpcId => npcDefinition != null ? npcDefinition.npcId : -1;

    private Transform _player;
    private bool _metRegistered; // กัน double-register ใน session เดียวกัน
    private bool _wasInRange;    // เก็บ state ก่อนหน้าไว้ทำ hysteresis กันกระพริบ

    // ── Registry: หา NPCInteractable ตัวจริงในฉากจาก npcId ────────────────────
    // ใช้โดย RelationshipManager (client-side) ตอนได้รับ ClientRpc แจ้งผลให้ของขวัญ
    // เพื่อโชว์ท่าทาง/คำขอบคุณที่ NPC ตัวที่ถูกให้ของขวัญจริงๆ
    private static readonly Dictionary<int, NPCInteractable> _registry = new();

    public static NPCInteractable FindById(int npcId) =>
        _registry.TryGetValue(npcId, out var npc) ? npc : null;

    private void OnEnable()
    {
        if (npcDefinition != null) _registry[npcDefinition.npcId] = this;
    }

    private void OnDisable()
    {
        if (npcDefinition == null) return;
        if (_registry.TryGetValue(npcDefinition.npcId, out var current) && current == this)
            _registry.Remove(npcDefinition.npcId);
    }

    void Start()
    {
        TryFindPlayer();
        if (interactIcon) interactIcon.SetActive(false);
        if (npcAnimator == null) npcAnimator = GetComponentInChildren<Animator>();
    }

    /// <summary>
    /// พยายามหา Player อีกครั้งถ้ายังไม่เจอ — จำเป็นเพราะเกมนี้เป็น Netcode
    /// ตัวละครผู้เล่นจริงมักถูก spawn ผ่านเครือข่าย "หลัง" Start() ของ NPC ที่อยู่ใน scene ตั้งแต่ต้น
    /// ถ้าเช็คแค่ครั้งเดียวใน Start() จะเจอ _player เป็น null ค้างตลอดไป
    /// </summary>
    private void TryFindPlayer()
    {
        if (_player != null) return;
        _player = LocalPlayerUtil.GetLocalPlayerTransform();
    }

    /// <summary>
    /// Logic คุย/เลื่อนบทพูดจริง — เรียกจากคลิกซ้ายเมาส์ที่ตัว NPC โดยตรง (ผ่าน RequestTalk) เท่านั้น
    /// (ตัดการคุยผ่านปุ่ม E ออกแล้วตามที่ขอ — เหลือช่องทางเดียวคือคลิกซ้าย)
    /// </summary>
    private void TalkStep(bool inRange)
    {
        if (DialogueManager.Instance == null) return;

        if (DialogueManager.Instance.IsDialogueActive)
        {
            // ถ้ากำลังคุยอยู่และผู้เล่นยังอยู่ในระยะ → เลื่อนบทต่อไป
            if (inRange) DialogueManager.Instance.DisplayNextSentence();
        }
        else if (inRange)
        {
            FacePlayer();

            // เริ่ม Dialogue
            DialogueManager.Instance.StartDialogue(dialogueData);

            // บันทึกการพบใน RelationshipManager (ครั้งแรกเท่านั้น)
            TryRegisterMeet();
        }
    }

    // ── External API (เรียกจากระบบคลิกเมาส์/hover ที่ตัว NPC) ─────────────────

    /// <summary>
    /// ผู้เล่นยังอยู่ในระยะคุยกับ NPC คนนี้ไหม (เผื่อ manager ภายนอกอยากรู้ก่อนโชว์ prompt)
    /// มี hysteresis กันกระพริบ: เข้าระยะที่ interactRadius ปกติ แต่ต้องออกไปไกลกว่า
    /// interactRadius + rangeHysteresis ถึงจะนับว่าออกจากระยะจริง (กันเด้งเข้า-ออกตอนยืนขอบพอดี)
    /// </summary>
    public bool IsPlayerInRange()
    {
        TryFindPlayer();
        if (_player == null) { _wasInRange = false; return false; }

        float dist = Vector3.Distance(transform.position, _player.position);
        float threshold = _wasInRange ? interactRadius + rangeHysteresis : interactRadius;
        _wasInRange = dist <= threshold;
        return _wasInRange;
    }

    /// <summary>ไอเทมนี้ให้เป็นของขวัญกับ NPC ได้ไหม — ตอนนี้ให้ได้ทุกหมวดยกเว้น Tools</summary>
    public static bool CanGift(ItemSO heldItem) => heldItem != null && heldItem.category != ItemCategory.Tools;

    /// <summary>เรียกจากคลิกซ้ายเมาส์ที่ตัว NPC — เริ่ม/เลื่อนบทพูด (ช่องทางเดียวที่ใช้คุยกับ NPC ได้)</summary>
    public void RequestTalk()
    {
        TryFindPlayer();
        if (!_player || DialogueManager.Instance == null) return;
        TalkStep(IsPlayerInRange());
    }

    /// <summary>เรียกจากคลิกขวาเมาส์ที่ตัว NPC พร้อมไอเทมที่ถืออยู่ — ส่ง request ให้ของขวัญไปที่ server</summary>
    public void RequestGiveGift(ItemSO heldItem)
    {
        if (!CanGift(heldItem)) return;
        if (npcDefinition == null || relationshipSignal == null) return;
        if (!IsPlayerInRange()) return;

        var manager = relationshipSignal.CurrentData;
        if (manager == null) return;

        manager.RequestGiveGiftServerRpc(npcDefinition.npcId, heldItem.itemID);
    }

    // ── Update: ควบคุม icon + เล่นแอนิเมชันทักทายตอนผู้เล่นเพิ่งเดินเข้าระยะ ──
    void Update()
    {
        TryFindPlayer();
        if (!_player) return;

        bool isTalking = DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive;
        float dist = Vector3.Distance(transform.position, _player.position);
        bool inRangeNow = dist <= interactRadius;

        if (interactIcon != null)
        {
            bool shouldShow = inRangeNow && !isTalking;
            if (interactIcon.activeSelf != shouldShow)
                interactIcon.SetActive(shouldShow);
        }

        // เล่น Trigger ทักทาย + หันหน้าหาผู้เล่น แค่ตอน "เพิ่งเข้าระยะ" (rising edge) ครั้งเดียว ไม่ยิงซ้ำทุกเฟรมที่ยังอยู่ในระยะ
        if (inRangeNow && !_wasInRangeForGreet && !isTalking)
        {
            FacePlayer();
            TriggerGreet();
        }
        _wasInRangeForGreet = inRangeNow;
    }

    /// <summary>หมุนตัว NPC ให้หันหน้าเข้าหาตำแหน่งผู้เล่น (แนวราบเท่านั้น ไม่ก้ม/เงย)</summary>
    private void FacePlayer()
    {
        if (_player == null) return;
        Vector3 dir = (_player.position - transform.position).normalized;
        dir.y = 0;
        if (dir.sqrMagnitude < 0.0001f) return; // กันกรณีผู้เล่นยืนซ้อนตำแหน่งเดียวกันเป๊ะ (LookRotation จะ error ถ้า dir เป็นศูนย์)
        transform.rotation = Quaternion.LookRotation(dir);
    }

    /// <summary>สั่งเล่นแอนิเมชันทักทาย (Trigger) — เรียกอัตโนมัติตอนผู้เล่นเพิ่งเดินเข้าระยะคุยได้</summary>
    private void TriggerGreet()
    {
        if (npcAnimator != null && !string.IsNullOrEmpty(greetTrigger))
            npcAnimator.SetTrigger(greetTrigger);
    }

    /// <summary>
    /// เรียกจาก RelationshipManager (client-side) ตอน server ยืนยันว่าให้ของขวัญสำเร็จ —
    /// หันหน้าหาผู้เล่น เล่นท่าทางดีใจ แล้วพูดขอบคุณสั้นๆ 1 ประโยค (สุ่มจากคลังข้อความตามระดับความชอบ)
    /// </summary>
    public void ReactToGift(GiftReactionTier tier)
    {
        FacePlayer();

        if (npcAnimator != null && !string.IsNullOrEmpty(thankYouTrigger))
            npcAnimator.SetTrigger(thankYouTrigger);

        if (DialogueManager.Instance == null) return;

        string[] pool = tier switch
        {
            GiftReactionTier.Loved when lovedThankYouLines != null && lovedThankYouLines.Length > 0
                => lovedThankYouLines,
            GiftReactionTier.Liked when favoriteThankYouLines != null && favoriteThankYouLines.Length > 0
                => favoriteThankYouLines,
            _ => thankYouLines
        };

        if (pool == null || pool.Length == 0) return;

        string line = pool[Random.Range(0, pool.Length)];
        // ต้องส่ง portrait ของ NPC ไปด้วยเสมอ — ถ้าไม่ส่ง (เดิมไม่ได้ส่ง) แล้วเป็นบทพูดแรกสุดของ
        // session นี้ (เช่น ผู้เล่นตรงมาให้ของขวัญเลยโดยยังไม่เคยกดคุยมาก่อน) DialogueManager
        // จะไม่มีรูปเก่าให้ต่อ (มันออกแบบให้ "คงรูปเดิมไว้" ถ้าบรรทัดนี้ไม่มีรูป กันภาพกระพริบ
        // ตอนคุยหลายประโยคต่อกัน) ผลคือรูป NPC ไม่ขึ้นเลย
        Sprite portrait = npcDefinition != null ? npcDefinition.portrait : null;
        DialogueManager.Instance.StartOneLiner(characterName, line, portrait);

        // สำคัญ: บทขอบคุณนี้เริ่มจาก server (ไม่ได้ผ่านคลิกซ้าย/RequestTalk ตามปกติ)
        // ต้องบอก NPCMouseInteractionUI ด้วยว่ากำลังคุยกับ NPC ตัวนี้อยู่ ไม่งั้นคลิกซ้ายเพื่อ
        // เลื่อน/ข้ามบทจะไม่ทำงาน (เพราะ _activeDialogueTarget ของมันยังเป็น null)
        NPCMouseInteractionUI.Instance?.SetActiveDialogueTarget(this);
    }

    private void TryRegisterMeet()
    {
        if (_metRegistered) return;
        if (npcDefinition == null || relationshipSignal == null) return;

        var manager = relationshipSignal.CurrentData;
        if (manager == null) return;

        if (!manager.HasMet(npcDefinition.npcId))
        {
            manager.RequestMeetNPCServerRpc(npcDefinition.npcId);
            _metRegistered = true;
        }
        else
        {
            _metRegistered = true; // เคยพบแล้ว ไม่ต้อง call อีก
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}
