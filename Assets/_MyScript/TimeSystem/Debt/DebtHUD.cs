using UnityEngine;
using TMPro;

/// <summary>
/// HUD แสดงหนี้คงเหลือ + วันครบกำหนดจ่ายถัดไป — แสดงบนจอตลอดเวลา
///
/// โครงสร้าง Hierarchy ที่ต้องสร้างใน Canvas:
/// ─ DebtHUD (Panel มุมบนขวา)
///   ├─ DebtLabel (TMP — "หนี้คงเหลือ")
///   ├─ DebtValueText (TMP — "¥100,000")
///   ├─ DeadlineLabel (TMP — "กำหนดจ่าย")
///   ├─ DeadlineText (TMP — "วันที่ 30/02 Y1")
///   ├─ MonthsLeftText (TMP — "เหลืออีก 10 เดือน" — optional)
///   └─ ProgressBar (Slider — optional, แสดงสัดส่วนหนี้ที่จ่ายไปแล้ว)
/// </summary>
public class DebtHUD : MonoBehaviour
{
    [Header("Text Elements")]
    [Tooltip("แสดงจำนวนหนี้คงเหลือ เช่น ¥100,000")]
    public TextMeshProUGUI debtValueText;

    [Tooltip("แสดงวันครบกำหนดจ่ายถัดไป เช่น 30/02 Y1")]
    public TextMeshProUGUI deadlineText;

    [Tooltip("แสดงจำนวนเดือนที่เหลือ (optional)")]
    public TextMeshProUGUI monthsLeftText;

    [Header("Progress Bar (Optional)")]
    [Tooltip("Slider แสดงสัดส่วนหนี้ที่จ่ายไปแล้ว (0 → 1)")]
    public UnityEngine.UI.Slider progressBar;

    [Header("Visual Feedback")]
    [Tooltip("สีปกติของตัวเลขหนี้")]
    public Color normalColor = new Color(1f, 0.85f, 0.3f);   // เหลืองทอง

    [Tooltip("สีเมื่อใกล้ deadline (3 วันสุดท้าย)")]
    public Color urgentColor = new Color(1f, 0.3f, 0.3f);    // แดง

    [Tooltip("สีเมื่อหนี้หมดแล้ว")]
    public Color clearedColor = new Color(0.3f, 1f, 0.5f);   // เขียว

    [Header("Settings")]
    [Tooltip("ซ่อน HUD เมื่อหนี้หมดแล้ว")]
    public bool hideWhenCleared = false;

    [Tooltip("จำนวนวันก่อน deadline ที่จะเปลี่ยนเป็นสีแดง")]
    public int urgentDaysBefore = 3;

    private DebtCollectorManager debt;
    private CalendarSystem calendar;

    void Start()
    {
        debt = DebtCollectorManager.Instance;
        if (debt == null) debt = FindObjectOfType<DebtCollectorManager>();

        calendar = CalendarSystem.Instance;
        if (calendar == null) calendar = FindObjectOfType<CalendarSystem>();

        // Subscribe events
        if (debt != null)
            debt.OnDebtChanged += OnDebtChanged;

        if (calendar != null)
            calendar.OnDateChanged += OnDateChanged;

        // อัปเดตครั้งแรก
        Refresh();
    }

    void OnDestroy()
    {
        if (debt != null)
            debt.OnDebtChanged -= OnDebtChanged;

        if (calendar != null)
            calendar.OnDateChanged -= OnDateChanged;
    }

    void OnDebtChanged(int newDebt) => Refresh();
    void OnDateChanged(Date d) => Refresh();

    /// <summary>อัปเดต HUD ทั้งหมด</summary>
    public void Refresh()
    {
        if (debt == null) return;

        int remaining = debt.CurrentDebt;
        bool cleared = remaining <= 0;

        // === ซ่อน HUD ถ้าหนี้หมด ===
        if (hideWhenCleared && cleared)
        {
            gameObject.SetActive(false);
            return;
        }

        // === หนี้คงเหลือ ===
        if (debtValueText != null)
        {
            if (cleared)
            {
                debtValueText.text = "หนี้หมดแล้ว!";
                debtValueText.color = clearedColor;
            }
            else
            {
                debtValueText.text = $"¥{remaining:N0}";
                debtValueText.color = IsUrgent() ? urgentColor : normalColor;
            }
        }

        // === Deadline ===
        if (deadlineText != null)
        {
            if (cleared)
            {
                deadlineText.text = "---";
            }
            else
            {
                Date due = debt.NextDueDate;
                deadlineText.text = $"{due.day:00}/{due.month:00} Y{due.year}";
                deadlineText.color = IsUrgent() ? urgentColor : Color.white;
            }
        }

        // === เดือนที่เหลือ ===
        if (monthsLeftText != null)
        {
            int monthsRemaining = debt.MonthsRemaining;
            if (cleared)
                monthsLeftText.text = "";
            else if (monthsRemaining < 0)
                monthsLeftText.text = "ไม่จำกัดเวลา";
            else if (monthsRemaining <= 3)
            {
                monthsLeftText.text = $"เหลืออีก {monthsRemaining} เดือน!";
                monthsLeftText.color = urgentColor;
            }
            else
            {
                monthsLeftText.text = $"เหลืออีก {monthsRemaining} เดือน";
                monthsLeftText.color = Color.white;
            }
        }

        // === Progress Bar ===
        if (progressBar != null)
        {
            float progress = cleared ? 1f :
                1f - (float)remaining / Mathf.Max(1, debt.startingDebt);
            progressBar.value = Mathf.Clamp01(progress);
        }
    }

    /// <summary>ใกล้วัน deadline หรือยัง?</summary>
    bool IsUrgent()
    {
        if (calendar == null || debt == null) return false;

        Date now = calendar.date;
        Date due = debt.NextDueDate;

        // คำนวณวันที่เหลือ (แบบง่าย เพราะเดือนยาวเท่ากันหมด)
        int daysLeft;
        if (due.month == now.month && due.year == now.year)
            daysLeft = due.day - now.day;
        else
            daysLeft = (calendar.daysPerMonth - now.day) + due.day;

        return daysLeft <= urgentDaysBefore && daysLeft >= 0;
    }
}
