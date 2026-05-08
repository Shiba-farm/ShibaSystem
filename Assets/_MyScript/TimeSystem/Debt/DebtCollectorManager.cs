using System;
using UnityEngine;

/// <summary>
/// ระบบหนี้แบบ "เจ้าหนี้มาเก็บ":
/// - เจ้าหนี้จะโผล่มาตอนสิ้นเดือน (หรือ dueDay ที่กำหนด)
/// - ไม่ตัดเงินอัตโนมัติ → เปิด UI ให้ผู้เล่นเลือกจ่าย
/// - ถ้าไม่จ่ายหรือจ่ายไม่ครบ → โดนค่าปรับ (lateFee)
/// - ถ้าจ่ายหมด → ชนะเกม!
/// </summary>
public class DebtCollectorManager : MonoBehaviour
{
    public static DebtCollectorManager Instance { get; private set; }

    [Header("Refs")]
    public CalendarSystem calendar;
    public PlayerWallet wallet;

    [Header("Debt Config")]
    [Tooltip("หนี้ตั้งต้น (จากพินัยกรรม)")]
    public int startingDebt = 100000;

    [Tooltip("ยอดขั้นต่ำที่ต้องจ่ายต่อเดือน")]
    public int minimumPayment = 2000;

    [Tooltip("ค่าปรับถ้าจ่ายไม่ถึงขั้นต่ำ")]
    public int lateFee = 500;

    [Tooltip("กำหนดวันที่เก็บหนี้ (1..daysPerMonth), -1 = วันสุดท้ายของเดือน")]
    public int dueDay = -1;

    [Tooltip("จำนวนเดือนทั้งหมดที่ต้องใช้หนี้ (0 = ไม่จำกัด)")]
    public int totalMonths = 12;

    [Header("Runtime (read-only)")]
    [SerializeField] private int currentDebt;
    [SerializeField] private int missedPayments;
    [SerializeField] private int monthsPassed;
    [SerializeField] private bool collectorVisiting;

    // === Public Properties ===
    public int CurrentDebt => currentDebt;
    public int MissedPayments => missedPayments;
    public int MonthsPassed => monthsPassed;
    public int MonthsRemaining => totalMonths > 0 ? Mathf.Max(0, totalMonths - monthsPassed) : -1;
    public int MinimumPayment => Mathf.Min(minimumPayment, currentDebt);
    public bool IsCollectorVisiting => collectorVisiting;
    public bool IsDebtPaid => currentDebt <= 0;

    /// <summary>วันครบกำหนดจ่ายถัดไป (คำนวณจาก calendar)</summary>
    // public Date NextDueDate
    // {
    //     get
    //     {
    //         if (calendar == null) return new Date(1, 1, 1);
    //         // var d = calendar.date;
    //         // int due = (dueDay < 1) ? calendar.daysPerMonth : dueDay;

    //         // ถ้ายังไม่ถึงวัน due ของเดือนนี้
    //         if (d.day < due)
    //             return new Date(d.year, d.month, due);

    //         // ถ้าผ่านวัน due แล้ว → เดือนหน้า
    //         int nextMonth = d.month + 1;
    //         int nextYear = d.year;
    //         if (nextMonth > 12) { nextMonth = 1; nextYear++; }
    //         return new Date(nextYear, nextMonth, due);
    //     }
    // }

    // === Events ===
    /// <summary>เจ้าหนี้มาแล้ว! UI ควร subscribe เพื่อเปิดหน้าจ่ายหนี้</summary>
    public event Action OnCollectorArrived;

    /// <summary>การจ่ายหนี้เสร็จสิ้น (จ่ายแล้วหรือปฏิเสธ)</summary>
    public event Action OnPaymentResolved;

    /// <summary>หนี้เปลี่ยนแปลง (UI ควร refresh)</summary>
    public event Action<int> OnDebtChanged;

    /// <summary>หนี้หมด! ชนะเกม!</summary>
    public event Action OnDebtFullyPaid;

    // === Setter สำหรับ Save/Load ===
    public void SetCurrentDebt(int value)
    {
        currentDebt = Mathf.Max(0, value);
        OnDebtChanged?.Invoke(currentDebt);
    }

    public void SetMissedPayments(int value) => missedPayments = Mathf.Max(0, value);
    public void SetMonthsPassed(int value) => monthsPassed = Mathf.Max(0, value);

    // ================================================================
    // Lifecycle
    // ================================================================

    void Awake()
    {
        if (Instance != null && Instance != this) { Debug.LogWarning($"[DebtCollectorManager] พบ Instance ซ้ำบน '{gameObject.name}' — ลบ Component"); Destroy(this); return; }
        Instance = this;

        if (!calendar) calendar = FindObjectOfType<CalendarSystem>();
        if (!wallet) wallet = FindObjectOfType<PlayerWallet>();
        if (currentDebt <= 0) currentDebt = startingDebt;
    }

    void OnEnable()
    {
        // if (calendar) calendar.OnDayEnded += OnDayEnded;
    }

    void OnDisable()
    {
        // if (calendar) calendar.OnDayEnded -= OnDayEnded;
    }

    // ================================================================
    // Calendar Hook — เจ้าหนี้มาตอนสิ้นเดือน
    // ================================================================

    void OnDayEnded(Date d)
    {
        if (currentDebt <= 0) return;
        if (collectorVisiting) return; // กำลังคุยอยู่

        // bool isDueDay = (dueDay < 1)
        //     ? calendar.IsLastDayOfMonth(d.day)
        //     : (d.day == dueDay);

        // if (!isDueDay) return;

        // เจ้าหนี้มาเก็บเงิน!
        monthsPassed++;
        collectorVisiting = true;

        Debug.Log($"[Debt] เจ้าหนี้มาแล้ว! เดือนที่ {monthsPassed}, หนี้คงเหลือ {currentDebt}");
        OnCollectorArrived?.Invoke();
    }

    // ================================================================
    // Payment API — เรียกจาก DebtPaymentUI
    // ================================================================

    /// <summary>
    /// ผู้เล่นจ่ายเงิน (เรียกจาก UI)
    /// </summary>
    /// <param name="amount">จำนวนเงินที่จ่าย</param>
    /// <returns>ผลลัพธ์การจ่าย</returns>
    public PaymentResult MakePayment(int amount)
    {
        if (!collectorVisiting) return PaymentResult.NotVisiting;
        if (amount <= 0) return PaymentResult.InvalidAmount;
        if (wallet == null) return PaymentResult.NoWallet;

        int canPay = Mathf.Min(amount, wallet.Money);
        canPay = Mathf.Min(canPay, currentDebt); // อย่าจ่ายเกินหนี้

        if (canPay <= 0) return PaymentResult.NotEnoughMoney;

        wallet.TrySpend(canPay);
        currentDebt -= canPay;

        bool paidMinimum = canPay >= MinimumPayment;

        Debug.Log($"[Debt] จ่าย {canPay}, หนี้เหลือ {currentDebt}, จ่ายถึงขั้นต่ำ: {paidMinimum}");

        OnDebtChanged?.Invoke(currentDebt);

        if (currentDebt <= 0)
        {
            ResolveVisit(true);
            OnDebtFullyPaid?.Invoke();
            return PaymentResult.DebtCleared;
        }

        return paidMinimum ? PaymentResult.PaidEnough : PaymentResult.PaidButBelowMinimum;
    }

    /// <summary>
    /// ผู้เล่นเลือก "ไม่จ่าย" → โดนค่าปรับ
    /// </summary>
    public void RefusePayment()
    {
        if (!collectorVisiting) return;

        currentDebt += lateFee;
        missedPayments++;

        Debug.Log($"[Debt] ไม่จ่าย! +ค่าปรับ {lateFee}, หนี้เพิ่มเป็น {currentDebt}, missed={missedPayments}");

        OnDebtChanged?.Invoke(currentDebt);
        ResolveVisit(false);
    }

    /// <summary>
    /// ผู้เล่นจ่ายบางส่วนแล้วกดยืนยัน (จ่ายไม่ถึงขั้นต่ำ → โดนค่าปรับ)
    /// </summary>
    public void FinishVisit(bool paidMinimum)
    {
        if (!collectorVisiting) return;

        if (!paidMinimum)
        {
            currentDebt += lateFee;
            missedPayments++;
            Debug.Log($"[Debt] จ่ายไม่ถึงขั้นต่ำ! +ค่าปรับ {lateFee}, หนี้={currentDebt}");
            OnDebtChanged?.Invoke(currentDebt);
        }

        ResolveVisit(paidMinimum);
    }

    void ResolveVisit(bool success)
    {
        collectorVisiting = false;

        // [NEW] แจ้ง DebtPunishmentSystem ว่าจ่ายถึงขั้นต่ำหรือเปล่า
        if (DebtPunishmentSystem.Instance != null)
            DebtPunishmentSystem.Instance.NotifyPaymentResult(success);

        OnPaymentResolved?.Invoke();
    }

    // ================================================================
    // Debug
    // ================================================================

    [ContextMenu("Debug/Pay All")]
    void DebugPayAll()
    {
        if (!wallet) return;
        int pay = Mathf.Min(wallet.Money, currentDebt);
        wallet.TrySpend(pay);
        currentDebt -= pay;
        OnDebtChanged?.Invoke(currentDebt);
        if (currentDebt <= 0) OnDebtFullyPaid?.Invoke();
        Debug.Log($"[Debt] PayAll {pay}, left={currentDebt}");
    }

    [ContextMenu("Debug/Force Collector Visit")]
    void DebugForceVisit()
    {
        monthsPassed++;
        collectorVisiting = true;
        OnCollectorArrived?.Invoke();
    }
}

/// <summary>ผลลัพธ์จากการจ่ายหนี้</summary>
public enum PaymentResult
{
    PaidEnough,         // จ่ายถึงขั้นต่ำ
    PaidButBelowMinimum,// จ่ายแต่ไม่ถึงขั้นต่ำ
    DebtCleared,        // หนี้หมดแล้ว!
    NotEnoughMoney,     // เงินไม่พอ
    InvalidAmount,      // จำนวนไม่ถูกต้อง
    NotVisiting,        // เจ้าหนี้ไม่ได้มา
    NoWallet            // ไม่มี PlayerWallet
}
