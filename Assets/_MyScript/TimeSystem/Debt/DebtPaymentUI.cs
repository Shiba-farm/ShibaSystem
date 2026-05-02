using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// หน้าต่าง UI ที่เปิดขึ้นเมื่อเจ้าหนี้มาเก็บเงินสิ้นเดือน
///
/// โครงสร้าง Hierarchy ที่ต้องสร้างใน Canvas:
/// ─ DebtPaymentPanel (Panel, FullScreen Overlay + สีดำโปร่ง)
///   ├─ DialogueBox (Panel ด้านล่าง)
///   │  ├─ PortraitImage (Image — รูปเจ้าหนี้)
///   │  ├─ CollectorNameText (TMP — "ทานูกิ เจ้าหนี้")
///   │  └─ DialogueText (TMP — คำพูดเจ้าหนี้)
///   ├─ InfoBox (Panel กลาง)
///   │  ├─ DebtAmountText (TMP — "หนี้คงเหลือ: ¥100,000")
///   │  ├─ MinPaymentText (TMP — "ขั้นต่ำ: ¥2,000")
///   │  ├─ YourMoneyText (TMP — "เงินของคุณ: ¥5,000")
///   │  └─ LateFeeWarning (TMP — "* ไม่จ่ายถึงขั้นต่ำ → ค่าปรับ ¥500")
///   └─ ButtonGroup (Horizontal Layout)
///      ├─ PayFullBtn (Button — "จ่ายขั้นต่ำ ¥2,000")
///      ├─ PayAllBtn (Button — "จ่ายหมดเลย ¥100,000")
///      ├─ PayCustomBtn (Button — "จ่ายเอง...")
///      └─ RefuseBtn (Button — "ไม่จ่าย")
/// </summary>
public class DebtPaymentUI : MonoBehaviour
{
    [Header("Panels")]
    [Tooltip("Panel หลักทั้งหน้า (ตอนเจ้าหนี้มา)")]
    public GameObject paymentPanel;

    [Tooltip("Panel สำหรับกรอกจำนวนเงิน (ซ่อนไว้ปกติ)")]
    public GameObject customInputPanel;

    [Header("Dialogue")]
    public Image portraitImage;
    public Sprite collectorPortrait;
    public TextMeshProUGUI collectorNameText;
    public TextMeshProUGUI dialogueText;

    [Header("Info Display")]
    public TextMeshProUGUI debtAmountText;
    public TextMeshProUGUI minPaymentText;
    public TextMeshProUGUI yourMoneyText;
    public TextMeshProUGUI lateFeeWarningText;

    [Header("Buttons")]
    public Button payMinimumBtn;
    public Button payAllBtn;
    public Button payCustomBtn;
    public Button refuseBtn;

    [Header("Custom Input")]
    public TMP_InputField customAmountInput;
    public Button confirmCustomBtn;
    public Button cancelCustomBtn;

    [Header("Result")]
    [Tooltip("Panel แสดงผลลัพธ์หลังจ่าย (optional)")]
    public GameObject resultPanel;
    public TextMeshProUGUI resultText;
    public Button resultOkBtn;

    [Header("Settings")]
    public string collectorName = "ทานูกิ";
    public float typeSpeed = 0.03f;

    [Header("Collector Lines")]
    [TextArea(2, 4)]
    public string arrivalLine = "ถึงเวลาจ่ายหนี้แล้วนะ! เจ้าหนี้ไม่ชอบรอหรอกนะ...";
    [TextArea(2, 4)]
    public string paidFullLine = "ดีมาก! เจ้าจ่ายครบ ข้าพอใจ ไว้เดือนหน้าเจอกัน!";
    [TextArea(2, 4)]
    public string paidPartialLine = "อืม... จ่ายไม่ถึงขั้นต่ำนะ เสียค่าปรับด้วย!";
    [TextArea(2, 4)]
    public string refusedLine = "ไม่จ่ายเหรอ!? โดนค่าปรับแน่ๆ เดือนหน้าเตรียมตัวไว้!";
    [TextArea(2, 4)]
    public string debtClearedLine = "เหลือเชื่อ!! เจ้าจ่ายหนี้หมดแล้ว! อิสระแล้วนะ!";

    private DebtCollectorManager debt;
    private int totalPaidThisVisit;
    private bool isTyping;
    private Coroutine typeCoroutine;

    void Start()
    {
        debt = DebtCollectorManager.Instance;
        if (debt == null) debt = FindObjectOfType<DebtCollectorManager>();

        // Subscribe เจ้าหนี้มา
        if (debt != null)
            debt.OnCollectorArrived += ShowPaymentPanel;

        // Wire ปุ่ม
        if (payMinimumBtn) payMinimumBtn.onClick.AddListener(OnPayMinimum);
        if (payAllBtn) payAllBtn.onClick.AddListener(OnPayAll);
        if (payCustomBtn) payCustomBtn.onClick.AddListener(OnPayCustomOpen);
        if (refuseBtn) refuseBtn.onClick.AddListener(OnRefuse);
        if (confirmCustomBtn) confirmCustomBtn.onClick.AddListener(OnConfirmCustom);
        if (cancelCustomBtn) cancelCustomBtn.onClick.AddListener(OnCancelCustom);
        if (resultOkBtn) resultOkBtn.onClick.AddListener(OnResultOk);

        // ซ่อนทุกอย่าง
        if (paymentPanel) paymentPanel.SetActive(false);
        if (customInputPanel) customInputPanel.SetActive(false);
        if (resultPanel) resultPanel.SetActive(false);
    }

    void OnDestroy()
    {
        if (debt != null)
            debt.OnCollectorArrived -= ShowPaymentPanel;
    }

    // ================================================================
    // แสดง Panel เจ้าหนี้มา
    // ================================================================

    void ShowPaymentPanel()
    {
        totalPaidThisVisit = 0;

        // [FIX] เปิด Panel ก่อนเสมอ — Coroutine จะ Start ไม่ได้ถ้า GameObject inactive
        if (paymentPanel) paymentPanel.SetActive(true);
        if (customInputPanel) customInputPanel.SetActive(false);
        if (resultPanel) resultPanel.SetActive(false);

        // [FIX] Reset ปุ่มทุกครั้งที่เจ้าหนี้มาใหม่
        SetButtonsActive(true);
        if (refuseBtn)
        {
            refuseBtn.onClick.RemoveAllListeners();
            refuseBtn.onClick.AddListener(OnRefuse);
            UpdateRefuseButtonText("ไม่จ่าย");
        }

        // Portrait + ชื่อ
        if (portraitImage && collectorPortrait)
        {
            portraitImage.sprite = collectorPortrait;
            portraitImage.gameObject.SetActive(true);
        }
        if (collectorNameText) collectorNameText.text = collectorName;

        // Dialogue (เรียกหลัง SetActive แล้ว)
        TypeDialogue(arrivalLine);

        // อัปเดตข้อมูล
        RefreshInfo();
    }

    void RefreshInfo()
    {
        if (debt == null) return;

        int remaining = debt.CurrentDebt;
        int minPay = debt.MinimumPayment;
        int myMoney = debt.wallet != null ? debt.wallet.Money : 0;

        if (debtAmountText)
            debtAmountText.text = $"หนี้คงเหลือ: <color=#FF6666>¥{remaining:N0}</color>";

        if (minPaymentText)
            minPaymentText.text = $"ขั้นต่ำเดือนนี้: ¥{minPay:N0}";

        if (yourMoneyText)
            yourMoneyText.text = $"เงินของคุณ: <color=#FFDD44>¥{myMoney:N0}</color>";

        if (lateFeeWarningText)
            lateFeeWarningText.text = $"จ่ายไม่ถึงขั้นต่ำ → ค่าปรับ ¥{debt.lateFee:N0}";

        // อัปเดตปุ่ม
        if (payMinimumBtn)
        {
            payMinimumBtn.interactable = myMoney >= minPay;
            var txt = payMinimumBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (txt) txt.text = $"จ่ายขั้นต่ำ\n¥{minPay:N0}";
        }

        if (payAllBtn)
        {
            payAllBtn.interactable = myMoney >= remaining;
            var txt = payAllBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (txt) txt.text = $"จ่ายหมด\n¥{remaining:N0}";
        }

        if (payCustomBtn)
        {
            payCustomBtn.interactable = myMoney > 0 && remaining > 0;
        }

        if (refuseBtn)
        {
            refuseBtn.interactable = true;
        }
    }

    // ================================================================
    // Button Handlers
    // ================================================================

    void OnPayMinimum()
    {
        int amount = debt.MinimumPayment;
        var result = debt.MakePayment(amount);
        totalPaidThisVisit += amount;
        HandleResult(result);
    }

    void OnPayAll()
    {
        int amount = debt.CurrentDebt;
        var result = debt.MakePayment(amount);
        totalPaidThisVisit += amount;
        HandleResult(result);
    }

    void OnPayCustomOpen()
    {
        if (customInputPanel) customInputPanel.SetActive(true);
        if (customAmountInput)
        {
            customAmountInput.text = "";
            customAmountInput.Select();
        }
    }

    void OnConfirmCustom()
    {
        if (customAmountInput == null) return;

        if (int.TryParse(customAmountInput.text, out int amount) && amount > 0)
        {
            var result = debt.MakePayment(amount);
            totalPaidThisVisit += Mathf.Min(amount, debt.wallet != null ? debt.wallet.Money + amount : amount);
            if (customInputPanel) customInputPanel.SetActive(false);
            HandleResult(result);
        }
        else
        {
            // กรอกไม่ถูกต้อง — shake หรือแสดง warning
            TypeDialogue("กรอกจำนวนเงินให้ถูกต้องด้วยนะ!");
        }
    }

    void OnCancelCustom()
    {
        if (customInputPanel) customInputPanel.SetActive(false);
    }

    void OnRefuse()
    {
        bool paidSomething = totalPaidThisVisit > 0;
        bool paidMinimum = totalPaidThisVisit >= debt.minimumPayment;

        if (paidSomething && !paidMinimum)
        {
            // จ่ายบ้างแต่ไม่ถึงขั้นต่ำ
            debt.FinishVisit(false);
            ShowResult(paidPartialLine);
        }
        else if (paidSomething && paidMinimum)
        {
            // จ่ายถึงขั้นต่ำแล้ว → OK!
            debt.FinishVisit(true);
            ShowResult(paidFullLine);
        }
        else
        {
            // ไม่จ่ายเลย
            debt.RefusePayment();
            ShowResult(refusedLine);
        }
    }

    // ================================================================
    // ผลลัพธ์
    // ================================================================

    void HandleResult(PaymentResult result)
    {
        switch (result)
        {
            case PaymentResult.DebtCleared:
                ShowResult(debtClearedLine);
                break;

            case PaymentResult.PaidEnough:
                // จ่ายครบขั้นต่ำ — อัปเดต info แล้วให้จ่ายต่อหรือออก
                TypeDialogue(paidFullLine);
                RefreshInfo();
                // ถ้าจ่ายถึงขั้นต่ำแล้ว เปลี่ยนปุ่ม "ไม่จ่าย" เป็น "เสร็จสิ้น"
                UpdateRefuseButtonText("เสร็จสิ้น");
                break;

            case PaymentResult.PaidButBelowMinimum:
                TypeDialogue("อืม... ยังไม่ถึงขั้นต่ำนะ จ่ายเพิ่มอีกไหม?");
                RefreshInfo();
                break;

            case PaymentResult.NotEnoughMoney:
                TypeDialogue("เจ้าไม่มีเงินนะ! จะทำยังไงล่ะ?");
                break;

            default:
                TypeDialogue("อะไรนะ?");
                break;
        }
    }

    void ShowResult(string line)
    {
        if (resultPanel != null && resultText != null)
        {
            // ใช้ result panel แยก
            resultText.text = line;
            resultPanel.SetActive(true);
            // ซ่อนปุ่มจ่าย
            SetButtonsActive(false);
        }
        else
        {
            // ไม่มี result panel → แสดงใน dialogue แล้วปิด
            TypeDialogue(line);
            // ซ่อนปุ่มจ่าย เหลือแค่ปุ่ม OK
            SetButtonsActive(false);
            if (refuseBtn)
            {
                refuseBtn.gameObject.SetActive(true);
                refuseBtn.interactable = true;
                UpdateRefuseButtonText("ปิด");
                refuseBtn.onClick.RemoveAllListeners();
                refuseBtn.onClick.AddListener(ClosePanel);
            }
        }
    }

    void OnResultOk()
    {
        ClosePanel();
    }

    void ClosePanel()
    {
        if (paymentPanel) paymentPanel.SetActive(false);
        if (resultPanel) resultPanel.SetActive(false);
        if (customInputPanel) customInputPanel.SetActive(false);

        // [FIX] คืนปุ่มทุกอัน + re-wire ปุ่ม refuse กลับเสมอ
        SetButtonsActive(true);

        if (refuseBtn)
        {
            refuseBtn.onClick.RemoveAllListeners();
            refuseBtn.onClick.AddListener(OnRefuse);
            UpdateRefuseButtonText("ไม่จ่าย");
        }
    }

    // ================================================================
    // Helpers
    // ================================================================

    void SetButtonsActive(bool active)
    {
        if (payMinimumBtn) payMinimumBtn.gameObject.SetActive(active);
        if (payAllBtn) payAllBtn.gameObject.SetActive(active);
        if (payCustomBtn) payCustomBtn.gameObject.SetActive(active);
        if (refuseBtn) refuseBtn.gameObject.SetActive(active);
    }

    void UpdateRefuseButtonText(string text)
    {
        if (refuseBtn == null) return;
        var tmp = refuseBtn.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp) tmp.text = text;
    }

    void TypeDialogue(string text)
    {
        if (typeCoroutine != null) StopCoroutine(typeCoroutine);

        if (dialogueText == null) return;

        // [FIX] ถ้า GameObject inactive อยู่ → แสดงข้อความทันทีโดยไม่ใช้ Coroutine
        if (!gameObject.activeInHierarchy)
        {
            dialogueText.text = text;
            return;
        }

        typeCoroutine = StartCoroutine(TypeRoutine(text));
    }

    IEnumerator TypeRoutine(string text)
    {
        isTyping = true;
        dialogueText.text = "";
        foreach (char c in text)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(typeSpeed);
        }
        isTyping = false;
    }
}
