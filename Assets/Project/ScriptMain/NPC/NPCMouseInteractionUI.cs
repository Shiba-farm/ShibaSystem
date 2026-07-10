using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Manager กลางตัวเดียวในฉาก — คอย raycast ใต้เมาส์ทุกเฟรม ถ้าชี้โดน NPCInteractable
/// ที่อยู่ในระยะคุยได้ (IsPlayerInRange) จะ:
///   1. เปลี่ยนไอคอนเคอร์เซอร์เมาส์เป็นรูปกล่องข้อความ (talkCursorTexture)
///   2. โชว์ prompt UI "พูดคุย" เสมอ และ "ให้ของขวัญ" เพิ่มถ้าไอเทมที่ถืออยู่ให้เป็นของขวัญได้
///   3. คลิกซ้าย  → NPCInteractable.RequestTalk() (เริ่ม/เลื่อนบทพูด)
///      คลิกขวา  → NPCInteractable.RequestGiveGift(ไอเทมที่ถืออยู่)
///
/// หมายเหตุ: ตอนกำลังคุยอยู่ (DialogueManager.IsDialogueActive) จะจำ NPC ที่กำลังคุยด้วยไว้
/// (_activeDialogueTarget) เพื่อให้คลิกซ้ายเลื่อนบทต่อได้ แม้เมาส์จะไม่ได้ชี้โดนตัว NPC พอดี
/// (เช่น ชี้ทับกล่องข้อความสนทนาที่เปิดคลุมอยู่) เหมือนพฤติกรรมปุ่ม E เดิม
///
/// วิธีติดตั้งใน Editor:
///   1. สร้าง GameObject เปล่าชื่อ "NPCMouseInteractionUI" ใต้ Canvas (UINew) แล้วผูก script นี้
///   2. ผูก Cam (เว้นว่างได้ — จะใช้ Camera.main อัตโนมัติ)
///   3. ผูก Held Item Signal — ตัวเดียวกับที่ใช้ใน PlayerHeldItem / TileCursor
///   4. ผูก Talk Cursor Texture — ไฟล์รูป Texture2D ตั้ง Texture Type = Cursor, Read/Write Enabled = true
///   5. สร้าง UI 2 ก้อนใต้ Canvas (Talk Prompt Object / Gift Prompt Object) แล้วลากมาผูก
///
/// สำคัญ: GameObject ที่เป็น background/พื้นหลังเต็มจอ (เช่น BackgroundPanel) ต้องปิด
/// "Raycast Target" ใน Image component ของมัน ไม่งั้นจะบัง raycast เข้าโลก 3D ทั้งหมด
/// </summary>
public class NPCMouseInteractionUI : MonoBehaviour
{
    public static NPCMouseInteractionUI Instance { get; private set; }

    [Header("References")]
    public Camera cam;
    [Tooltip("HeldItemSignal ตัวเดียวกับที่ใช้ใน PlayerHeldItem / TileCursor")]
    public HeldItemSignal heldItemSignal;
    [Tooltip("ระยะ raycast สูงสุดจากกล้องลงไปในโลก")]
    public float maxRayDistance = 100f;

    [Header("Cursor")]
    [Tooltip("Texture2D ไอคอนกล่องข้อความ — ตั้ง Texture Type = Cursor ใน Import Settings")]
    public Texture2D talkCursorTexture;
    public Vector2 cursorHotspot = Vector2.zero;

    [Header("Prompt UI (ลากจาก Canvas)")]
    [Tooltip("ตัวห่อ prompt ทั้งก้อน (optional) — ถ้ามีจะย้ายตำแหน่งก้อนนี้ก้อนเดียว ไม่งั้นย้าย Talk/Gift แยกกัน")]
    public GameObject promptRoot;
    public GameObject talkPromptObject;
    public GameObject giftPromptObject;

    [Header("Reveal Animation")]
    [Tooltip("เวลาที่ใช้เล่น animation โชว์ prompt จากซ้ายไปขวา (วินาที) — " +
             "สำคัญ: RectTransform ของ TalkPrompt/GiftPrompt ต้องตั้ง Pivot X = 0 (ชิดซ้าย) ไม่งั้นจะโตจากตรงกลางแทน")]
    public float revealDuration = 0.15f;

    private bool _cursorIsCustom;
    private NPCInteractable _activeDialogueTarget; // NPC ที่กำลังคุยด้วยอยู่ตอนนี้ (ถ้ามี)
    private Coroutine _talkRevealCo;
    private Coroutine _giftRevealCo;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (!cam) cam = Camera.main;
        SetPromptVisible(false, false);
    }

    /// <summary>
    /// ให้ระบบภายนอก (เช่น NPCInteractable.ReactToGift ตอนพูดขอบคุณของขวัญ) ตั้งว่า
    /// "กำลังคุยกับ NPC ตัวนี้อยู่" ได้ — โดยไม่ต้องผ่าน RequestTalk()/คลิกซ้ายก่อน
    /// ไม่งั้นพอ dialogue เริ่มจากที่อื่น (ไม่ใช่คลิกซ้าย) คลิกซ้ายเพื่อเลื่อนบทจะไม่ทำงาน
    /// เพราะ _activeDialogueTarget ยังเป็น null อยู่ (เจอบั๊กนี้ตอนเพิ่มคำขอบคุณของขวัญ)
    /// </summary>
    public void SetActiveDialogueTarget(NPCInteractable target)
    {
        _activeDialogueTarget = target;
    }

    /// <summary>
    /// NPC ที่เมาส์กำลังชี้อยู่ตอนนี้ (อัปเดตทุกเฟรมใน Update นี้) — เผื่อระบบอื่น (เช่น
    /// PlayerItemUser) อยากรู้ว่า "คลิกนี้กำลังจะไปคุยกับ NPC" เพื่อกันการใช้ไอเทม/เล่น
    /// animation โจมตีชนกับคลิกเริ่มบทสนทนา (เช็คจาก IsDialogueActive อย่างเดียวไม่พอ เพราะ
    /// คลิกแรกที่ "เพิ่งจะ" เริ่มบทสนทนา ตอนนั้น dialogue ยังไม่ active จนกว่าคลิกนี้จะประมวลผลเสร็จ)
    /// </summary>
    public NPCInteractable CurrentHoverTarget { get; private set; }

    void Update()
    {
        if (!cam) cam = Camera.main;
        if (cam == null) return;

        // ไม่ raycast เข้าโลกถ้าเมาส์กำลังชี้ทับ UI อื่นอยู่ (เช่น เปิด inventory คลุมหน้าจออยู่)
        bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        NPCInteractable hoverTarget = null;
        if (!overUI)
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance))
            {
                var npc = hit.collider.GetComponentInParent<NPCInteractable>();
                if (npc != null && npc.IsPlayerInRange()) hoverTarget = npc;
            }
        }

        CurrentHoverTarget = hoverTarget;

        UpdateCursor(hoverTarget != null);
        UpdatePrompt(hoverTarget);
        HandleClicks(hoverTarget);
    }

    private void UpdateCursor(bool showCustom)
    {
        if (showCustom == _cursorIsCustom) return;
        _cursorIsCustom = showCustom;

        if (showCustom && talkCursorTexture != null)
            Cursor.SetCursor(talkCursorTexture, cursorHotspot, CursorMode.Auto);
        else
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }

    private void UpdatePrompt(NPCInteractable target)
    {
        if (target == null)
        {
            SetPromptVisible(false, false);
            return;
        }

        ItemSO held = heldItemSignal != null ? heldItemSignal.Current : null;
        bool canGift = NPCInteractable.CanGift(held);

        // TEMP DEBUG — เอาไว้ดูว่าตอน GiftPrompt โผล่ทั้งที่ไม่ควร heldItemSignal.Current จริงๆ
        // แล้วคืออะไร (null / ไอเทมของจริงแต่ไม่มีไอคอน / อื่นๆ) จะลบทิ้งหลังหาสาเหตุเจอ
        if (canGift)
            Debug.Log($"[TEMP DEBUG] GiftPrompt โชว์ — held={(held != null ? $"{held.itemName} (id={held.itemID}, category={held.category}, icon={(held.icon != null ? held.icon.name : "NULL")})" : "null")}, slotIndex={heldItemSignal?.CurrentSlotIndex}");

        SetPromptVisible(true, canGift);

        // ตำแหน่ง prompt ตามตำแหน่งของ NPC บนจอ (เหมือน InteractPromptUI.cs)
        Vector3 screenPos = cam.WorldToScreenPoint(target.transform.position);
        screenPos.z = 0f;

        if (promptRoot != null)
        {
            promptRoot.transform.position = screenPos;
        }
        else
        {
            if (talkPromptObject != null) talkPromptObject.transform.position = screenPos;
            if (giftPromptObject != null) giftPromptObject.transform.position = screenPos;
        }
    }

    private void SetPromptVisible(bool talk, bool gift)
    {
        // promptRoot (ถ้ามี) ยังคุมการ "ซ่อนทั้งก้อนทันที" เหมือนเดิม (ไม่ต้องมี animation ตอนซ่อน
        // ตามที่ขอแค่ตอนโชว์ขึ้นมาเท่านั้น)
        if (promptRoot != null && !talk) promptRoot.SetActive(false);
        else if (promptRoot != null) promptRoot.SetActive(true);

        SetOnePromptVisible(talkPromptObject, talk, ref _talkRevealCo);
        SetOnePromptVisible(giftPromptObject, talk && gift, ref _giftRevealCo);
    }

    /// <summary>
    /// โชว์/ซ่อน prompt ตัวเดียว — ตอนโชว์จะเล่น animation ขยายจากซ้ายไปขวา (ต้องตั้ง Pivot X = 0
    /// บน RectTransform ของตัวนี้ใน Editor ก่อน ไม่งั้นจะขยายจากกึ่งกลางแทน)
    /// กันสั่งซ้ำด้วย activeSelf check — ไม่งั้น coroutine จะ restart ทุกเฟรมที่ยัง hover อยู่
    /// </summary>
    private void SetOnePromptVisible(GameObject obj, bool visible, ref Coroutine co)
    {
        if (obj == null) return;
        if (obj.activeSelf == visible) return;

        if (co != null) StopCoroutine(co);

        if (visible)
        {
            obj.SetActive(true);
            co = StartCoroutine(RevealLeftToRight(obj.transform));
        }
        else
        {
            obj.SetActive(false);
        }
    }

    private IEnumerator RevealLeftToRight(Transform t)
    {
        float elapsed = 0f;
        t.localScale = new Vector3(0f, 1f, 1f);

        while (elapsed < revealDuration)
        {
            elapsed += Time.deltaTime;
            float pct = Mathf.Clamp01(elapsed / revealDuration);
            t.localScale = new Vector3(pct, 1f, 1f);
            yield return null;
        }

        t.localScale = Vector3.one;
    }

    private void HandleClicks(NPCInteractable hoverTarget)
    {
        // ค่า "ก่อน" ประมวลผลคลิก — ใช้แค่ตัดสินใจว่าคลิกนี้ควรเริ่มบทสนทนาใหม่ หรือเลื่อนบทเดิมต่อ
        bool dialogueActiveBefore = DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive;

        if (Input.GetMouseButtonDown(0)) // ซ้าย = คุย/เลื่อนบทพูด
        {
            if (dialogueActiveBefore && _activeDialogueTarget != null)
            {
                // กำลังคุยอยู่แล้ว — เลื่อนบทต่อได้เลย ไม่ต้องเช็ค hover ซ้ำ
                // (เผื่อกรณีกล่องข้อความคลุมเมาส์อยู่ ทำให้ raycast โดน UI แทน NPC)
                _activeDialogueTarget.RequestTalk();
            }
            else if (hoverTarget != null)
            {
                hoverTarget.RequestTalk();
                _activeDialogueTarget = hoverTarget;
            }
        }
        else if (Input.GetMouseButtonDown(1)) // ขวา = ให้ของขวัญ
        {
            if (hoverTarget == null) return;
            ItemSO held = heldItemSignal != null ? heldItemSignal.Current : null;
            if (NPCInteractable.CanGift(held))
                hoverTarget.RequestGiveGift(held);
        }

        // บั๊กที่เจอ: ถ้าใช้ dialogueActiveBefore (ค่าก่อนคลิก) ตรงนี้ — คลิกแรกที่เพิ่ง "เริ่ม"
        // บทสนทนา (ตอนเข้าฟังก์ชันมา dialogue ยังไม่ active) จะโดนเช็คนี้ล้าง _activeDialogueTarget
        // ทิ้งทันทีหลังเพิ่งตั้งไปหมาดๆ ในบรรทัดข้างบน ทำให้ประโยคแรกต้องคลิกโดนตัว NPC/prompt
        // ซ้ำทุกครั้งกว่าจะข้ามได้ ทั้งที่ตั้งแต่ประโยคที่สองเป็นต้นไปคลิกที่ไหนก็ได้แล้ว
        // แก้โดยเช็คสถานะ "หลัง" ประมวลผลคลิกแล้วแบบสดๆ แทน
        bool dialogueActiveAfter = DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive;
        if (!dialogueActiveAfter) _activeDialogueTarget = null; // จบบทสนทนาแล้วจริงๆ เคลียร์ทิ้ง รอบหน้าต้อง hover ใหม่
    }

    void OnDisable()
    {
        // คืนค่า cursor default ตอนปิด/ทำลาย manager — กันเคอร์เซอร์ค้างเป็นรูปกล่องข้อความตลอดไป
        if (_cursorIsCustom)
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            _cursorIsCustom = false;
        }
    }
}
