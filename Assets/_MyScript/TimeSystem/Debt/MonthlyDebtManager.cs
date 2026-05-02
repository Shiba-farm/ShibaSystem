using UnityEngine;

/// <summary>
/// ระบบหนี้รายเดือน: หักเงินเมื่อถึงวันกำหนดชำระ (สิ้นเดือนหรือวันตามที่ตั้ง)
/// </summary>
public class MonthlyDebtManager : MonoBehaviour
{
    [Header("Refs")]
    public CalendarSystem calendar;
    public PlayerWallet wallet;

    [Header("Debt")]
    public int startingDebt = 1000;
    public int monthlyPayment = 200;
    public int lateFee = 50;
    [Tooltip("กำหนดวันชำระ (1..daysPerMonth) หรือ -1 = สิ้นเดือน")]
    public int dueDay = -1;


    [Header("Runtime (read-only)")]
    [SerializeField] private int currentDebt;

    public int CurrentDebt => currentDebt;
    public void SetCurrentDebt(int value)
    {
        currentDebt = Mathf.Max(0, value);
    }

    void Awake()
    {
        if (!calendar) calendar = FindObjectOfType<CalendarSystem>();
        if (!wallet) wallet = FindObjectOfType<PlayerWallet>();
        if (currentDebt <= 0) currentDebt = startingDebt;
    }

    void OnEnable()
    {
        if (calendar) calendar.OnDayEnded += OnDayEnded;
    }

    void OnDisable()
    {
        if (calendar) calendar.OnDayEnded -= OnDayEnded;
    }

    void OnDayEnded(Date d)
    {
        if (currentDebt <= 0) return;

        bool lastDay = (dueDay < 1) ? calendar.IsLastDayOfMonth(d.day)
                                    : (d.day == dueDay);
        if (!lastDay) return;

        int needPay = Mathf.Min(monthlyPayment, currentDebt);
        int have = wallet != null ? wallet.Money : 0;

        if (have >= needPay)
        {
            wallet.TrySpend(needPay);
            currentDebt -= needPay;
        }
        else
        {
            // เงินไม่พอ: จ่ายเท่าที่มี + ค่าปรับ
            if (have > 0) wallet.TrySpend(have);
            currentDebt = currentDebt - have + lateFee;
        }

        // (ทางเลือก) ล็อกไว้ดูใน Console
        Debug.Log($"[Debt] Pay day! paid={Mathf.Min(have, needPay)} left={currentDebt}");
    }

    // ------- ช่วยดีบัก -------
    [ContextMenu("Debug/Pay All")]
    void DebugPayAll()
    {
        if (!wallet) return;
        int pay = Mathf.Min(wallet.Money, currentDebt);
        wallet.TrySpend(pay);
        currentDebt -= pay;
        Debug.Log($"[Debt] PayAll {pay}, left={currentDebt}");
    }
}
