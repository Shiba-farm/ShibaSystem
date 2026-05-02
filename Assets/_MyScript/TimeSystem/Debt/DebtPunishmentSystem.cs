using System;
using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// ระบบลงโทษเมื่อผู้เล่นไม่จ่ายหนี้ติดต่อกัน
///
/// กฎหลัก:
/// - ไม่จ่ายหนี้ 1 ครั้ง       → คำเตือน (Warning) + ค่าปรับปกติ
/// - ไม่จ่ายหนี้ 2 ครั้งติด     → ลูกน้องเจ้าหนี้ทำลายตัวช่วย 1 ตัว
/// - ไม่จ่ายหนี้ 3 ครั้งติด     → ทำลายตัวช่วย 2 ตัว + หนี้เพิ่มพิเศษ
/// - ไม่จ่ายหนี้ 4+ ครั้งติด    → ทำลายตัวช่วยทั้งหมด + Penalty สูง
///
/// "consecutiveMisses" จะ reset เมื่อผู้เล่นจ่ายหนี้ถึงขั้นต่ำ
/// </summary>
public class DebtPunishmentSystem : MonoBehaviour
{
    public static DebtPunishmentSystem Instance { get; private set; }

    [Header("Refs")]
    [Tooltip("อ้างถึง DebtCollectorManager")]
    public DebtCollectorManager debtManager;

    [Header("Punishment Config")]
    [Tooltip("จำนวนครั้งไม่จ่ายติดกันก่อนเริ่มทำลาย")]
    public int missesBeforeDestruction = 2;

    [Tooltip("จำนวนตัวช่วยที่ทำลาย = consecutiveMisses - missesBeforeDestruction + 1")]
    [Min(1)]
    public int baseDestroyCount = 1;

    [Tooltip("หนี้เพิ่มพิเศษต่อครั้งที่ทำลาย (นอกจาก lateFee ปกติ)")]
    public int extraDebtPerDestruction = 1000;

    [Tooltip("ที่ 4+ ครั้ง ทำลายทั้งหมดไหม?")]
    public bool destroyAllAtMax = true;

    [Tooltip("จำนวนครั้งติดที่ถือว่า max (ทำลายทั้งหมด)")]
    public int maxConsecutiveForDestroyAll = 4;

    [Header("UI (Optional)")]
    [Tooltip("Panel แสดง cutscene ลูกน้องเจ้าหนี้มาทำลาย")]
    public GameObject punishmentPanel;
    public TextMeshProUGUI punishmentDialogueText;
    public TextMeshProUGUI punishmentDetailText;

    [Header("Cutscene Timing")]
    public float dialogueDelay = 1.5f;
    public float detailDelay = 2.5f;
    public float closePanelDelay = 5f;

    [Header("Runtime (read-only)")]
    [SerializeField] private int consecutiveMisses;

    // === Properties ===
    public int ConsecutiveMisses => consecutiveMisses;

    // === Events ===
    /// <summary>ลูกน้องเจ้าหนี้กำลังจะมาทำลาย!</summary>
    public event Action<int> OnPunishmentTriggered;  // count ที่จะทำลาย

    /// <summary>ทำลายเสร็จแล้ว</summary>
    public event Action<int> OnPunishmentComplete;   // count ที่ทำลายจริง

    /// <summary>คำเตือน (ครั้งแรกที่ไม่จ่าย)</summary>
    public event Action OnWarningIssued;

    // ================================================================
    // Lifecycle
    // ================================================================

    void Awake()
    {
        if (Instance != null && Instance != this) { Debug.LogWarning($"[DebtPunishmentSystem] พบ Instance ซ้ำบน '{gameObject.name}' — ลบ Component"); Destroy(this); return; }
        Instance = this;
    }

    void Start()
    {
        if (!debtManager) debtManager = DebtCollectorManager.Instance;

        if (debtManager != null)
        {
            debtManager.OnPaymentResolved += OnPaymentResolved;
        }

        if (punishmentPanel) punishmentPanel.SetActive(false);
    }

    void OnDestroy()
    {
        if (debtManager != null)
        {
            debtManager.OnPaymentResolved -= OnPaymentResolved;
        }
    }

    // ================================================================
    // Payment Resolved Hook
    // ================================================================

    void OnPaymentResolved()
    {
        if (debtManager == null) return;

        // ตรวจสอบว่าจ่ายถึงขั้นต่ำหรือเปล่า
        // เราใช้ missedPayments จาก debtManager (จะเพิ่มใน RefusePayment / FinishVisit)
        // เทียบกับค่าเดิม: ถ้า missedPayments เพิ่ม = ไม่จ่ายถึงขั้นต่ำ
        //
        // วิธีที่ดีกว่า: เราใช้ flag จาก ResolveVisit
        // สำหรับตอนนี้ ให้ DebtCollectorManager เรียก NotifyPaymentResult() ตรง ๆ
    }

    /// <summary>
    /// เรียกจาก DebtCollectorManager หลังจบ visit
    /// </summary>
    /// <param name="paidMinimum">จ่ายถึงขั้นต่ำหรือไม่</param>
    public void NotifyPaymentResult(bool paidMinimum)
    {
        if (paidMinimum)
        {
            // จ่ายถึงขั้นต่ำ → reset!
            consecutiveMisses = 0;
            Debug.Log("[Punishment] จ่ายถึงขั้นต่ำ — reset consecutive misses!");
            return;
        }

        // ไม่จ่ายถึงขั้นต่ำ
        consecutiveMisses++;
        Debug.Log($"[Punishment] ไม่จ่ายถึงขั้นต่ำ! consecutive={consecutiveMisses}");

        if (consecutiveMisses < missesBeforeDestruction)
        {
            // คำเตือน
            Debug.Log("[Punishment] คำเตือน! จ่ายครั้งหน้านะ ไม่งั้นลูกน้องเจ้าหนี้จะมา...");
            OnWarningIssued?.Invoke();
            ShowWarning();
        }
        else
        {
            // ทำลาย!
            int destroyCount = CalculateDestroyCount();
            ExecutePunishment(destroyCount);
        }
    }

    // ================================================================
    // Punishment Logic
    // ================================================================

    int CalculateDestroyCount()
    {
        if (destroyAllAtMax && consecutiveMisses >= maxConsecutiveForDestroyAll)
            return 999; // ทำลายทั้งหมด

        // Formula: base + (consecutiveMisses - threshold)
        return baseDestroyCount + (consecutiveMisses - missesBeforeDestruction);
    }

    void ExecutePunishment(int targetCount)
    {
        if (FarmHelperManager.Instance == null)
        {
            Debug.Log("[Punishment] ไม่มี FarmHelperManager — ข้ามการทำลาย");
            return;
        }

        int available = FarmHelperManager.Instance.PlacedCount;
        if (available == 0)
        {
            Debug.Log("[Punishment] ไม่มีตัวช่วยให้ทำลาย — เพิ่มหนี้แทน");
            AddExtraDebt(targetCount);
            return;
        }

        int toDestroy = Mathf.Min(targetCount, available);

        Debug.Log($"[Punishment] ลูกน้องเจ้าหนี้มาทำลาย {toDestroy} ตัว!");
        OnPunishmentTriggered?.Invoke(toDestroy);

        // เล่น UI cutscene
        StartCoroutine(PunishmentSequence(toDestroy));
    }

    IEnumerator PunishmentSequence(int count)
    {
        // 1) แสดง Panel
        if (punishmentPanel) punishmentPanel.SetActive(true);

        // 2) Dialogue
        if (punishmentDialogueText)
        {
            string dialogue = GetPunishmentDialogue();
            punishmentDialogueText.text = "";
            yield return StartCoroutine(TypeText(punishmentDialogueText, dialogue, 0.04f));
        }

        yield return new WaitForSeconds(dialogueDelay);

        // 3) ทำลายจริง
        int destroyed = 0;
        if (count >= 999)
            destroyed = FarmHelperManager.Instance.DestroyAllHelpers();
        else
            destroyed = FarmHelperManager.Instance.DestroyHelpers(count);

        // 4) เพิ่มหนี้พิเศษ
        int extraDebt = destroyed * extraDebtPerDestruction;
        if (extraDebt > 0 && debtManager != null)
        {
            debtManager.SetCurrentDebt(debtManager.CurrentDebt + extraDebt);
            Debug.Log($"[Punishment] หนี้เพิ่ม {extraDebt} จากค่าซ่อม/ค่าเสียหาย");
        }

        // 5) แสดงรายละเอียด
        if (punishmentDetailText)
        {
            string detail = $"ลูกน้องเจ้าหนี้ทำลายตัวช่วยฟาร์ม {destroyed} ชิ้น!\n";
            if (extraDebt > 0)
                detail += $"หนี้เพิ่ม ¥{extraDebt:N0} (ค่าเสียหาย)";
            punishmentDetailText.text = detail;
        }

        OnPunishmentComplete?.Invoke(destroyed);

        // 6) รอ แล้วปิด
        yield return new WaitForSeconds(closePanelDelay);
        if (punishmentPanel) punishmentPanel.SetActive(false);
    }

    // ================================================================
    // Warning (ครั้งแรก)
    // ================================================================

    void ShowWarning()
    {
        if (punishmentPanel == null || punishmentDialogueText == null) return;

        StartCoroutine(WarningSequence());
    }

    IEnumerator WarningSequence()
    {
        if (punishmentPanel) punishmentPanel.SetActive(true);
        if (punishmentDetailText) punishmentDetailText.text = "";

        if (punishmentDialogueText)
        {
            string warning = "เจ้าหนี้: \"เตือนนะ... ถ้าเดือนหน้าไม่จ่ายอีก จะส่งลูกน้องมาจัดการฟาร์มเอง!\"";
            punishmentDialogueText.text = "";
            yield return StartCoroutine(TypeText(punishmentDialogueText, warning, 0.04f));
        }

        yield return new WaitForSeconds(4f);
        if (punishmentPanel) punishmentPanel.SetActive(false);
    }

    // ================================================================
    // Dialogue Text
    // ================================================================

    string GetPunishmentDialogue()
    {
        if (consecutiveMisses >= maxConsecutiveForDestroyAll)
            return "ลูกน้องเจ้าหนี้: \"นายไม่จ่ายหนี้มาหลายเดือนแล้ว... วันนี้เราจะเอาทุกอย่างคืน!\"";

        if (consecutiveMisses == missesBeforeDestruction)
            return "ลูกน้องเจ้าหนี้: \"เจ้านายสั่งมา... ไม่จ่ายก็ต้องยึดของ!\"";

        return "ลูกน้องเจ้าหนี้: \"อีกแล้วเหรอ? วันนี้เราจะทำลายให้มากกว่าเดิม!\"";
    }

    // ================================================================
    // Typewriter Effect
    // ================================================================

    IEnumerator TypeText(TextMeshProUGUI textComp, string fullText, float charDelay)
    {
        textComp.text = "";
        foreach (char c in fullText)
        {
            textComp.text += c;
            yield return new WaitForSeconds(charDelay);
        }
    }

    // ================================================================
    // Extra Debt (ถ้าไม่มีตัวช่วยให้ทำลาย)
    // ================================================================

    void AddExtraDebt(int targetCount)
    {
        if (debtManager == null) return;

        int penalty = targetCount * extraDebtPerDestruction;
        debtManager.SetCurrentDebt(debtManager.CurrentDebt + penalty);

        Debug.Log($"[Punishment] ไม่มีตัวช่วยให้ทำลาย — เพิ่มหนี้โทษ ¥{penalty}");

        // แสดง UI แจ้ง
        if (punishmentPanel != null)
        {
            StartCoroutine(NoPropsPunishmentSequence(penalty));
        }
    }

    IEnumerator NoPropsPunishmentSequence(int penalty)
    {
        if (punishmentPanel) punishmentPanel.SetActive(true);

        if (punishmentDialogueText)
        {
            string text = "ลูกน้องเจ้าหนี้: \"ไม่มีอะไรให้ยึดเลยเหรอ? งั้นเพิ่มหนี้แทน!\"";
            punishmentDialogueText.text = "";
            yield return StartCoroutine(TypeText(punishmentDialogueText, text, 0.04f));
        }

        if (punishmentDetailText)
            punishmentDetailText.text = $"หนี้เพิ่ม ¥{penalty:N0}";

        yield return new WaitForSeconds(4f);
        if (punishmentPanel) punishmentPanel.SetActive(false);
    }

    // ================================================================
    // Save / Load
    // ================================================================

    public int GetConsecutiveMisses() => consecutiveMisses;
    public void SetConsecutiveMisses(int value) => consecutiveMisses = Mathf.Max(0, value);

    // ================================================================
    // Debug
    // ================================================================

    [ContextMenu("Debug/Force Punishment (miss=2)")]
    void DebugForcePunishment()
    {
        consecutiveMisses = missesBeforeDestruction;
        int count = CalculateDestroyCount();
        ExecutePunishment(count);
    }

    [ContextMenu("Debug/Reset Consecutive Misses")]
    void DebugResetMisses()
    {
        consecutiveMisses = 0;
        Debug.Log("[Punishment] Reset consecutive misses to 0");
    }
}
