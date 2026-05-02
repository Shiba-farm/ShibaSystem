using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// ระบบสิ้นวัน — ตี 2 จะ:
/// 1. หยุดเวลา + Lock player
/// 2. แสดง Summary แบ่งตาม Farming / Fishing / Ore / Other + รายการไอเท็ม
/// 3. ตัวเลขเงินจะ count-up จาก 0 → ยอดจริง
/// 4. กด "นอนหลับ" → จ่ายเงินให้ผู้เล่น → ขึ้น Day Banner → วันใหม่ 6:00 AM
///
/// ── Prefab ที่ต้องสร้าง 2 ชิ้น ──────────────────────────────────────
///
/// [CategoryHeaderPrefab]  (height ~55)
///   ├── CatLabel  (TMP)  — "Farming"         anchor ซ้าย
///   └── CatValue  (TMP)  — "¥990"            anchor ขวา
///
/// [ItemRowPrefab]  (height ~45)
///   ├── ItemIcon  (Image)                    anchor ซ้าย, 40x40
///   ├── ItemLabel (TMP)  — "Onion x99 ..."   anchor stretch ตรงกลาง
///   └── ItemValue (TMP)  — "¥990"            anchor ขวา
/// </summary>
public class DayEndSystem : MonoBehaviour
{
    public static DayEndSystem Instance { get; private set; }

    // ─── Config ───────────────────────────────────────────────────────
    [Header("Bedtime / Wake")]
    [Range(0, 5)] public int bedtimeHour = 2;
    [Range(4, 12)] public int wakeHour   = 6;
    [Range(0, 59)] public int wakeMinute = 0;

    // ─── Summary Panel ────────────────────────────────────────────────
    [Header("Summary Panel")]
    public GameObject summaryPanel;
    public TextMeshProUGUI summaryTitleText;

    [Tooltip("Parent ที่ spawn rows ทั้งหมด (Vertical Layout Group)")]
    public Transform rowsParent;

    [Tooltip("Prefab หัว category — ต้องมี TMP ชื่อ 'CatLabel' และ 'CatValue'")]
    public GameObject categoryHeaderPrefab;

    [Tooltip("Prefab แถวไอเท็ม — ต้องมี Image 'ItemIcon', TMP 'ItemLabel', TMP 'ItemValue'")]
    public GameObject itemRowPrefab;

    public TextMeshProUGUI totalText;
    public Button sleepButton;

    // ─── Screen Fade ──────────────────────────────────────────────────
    [Header("Screen Fade")]
    [Tooltip("Image สีดำเต็มจอ (CanvasGroup) — ใส่ใน Canvas ชั้นบนสุด")]
    public CanvasGroup fadePanel;
    [Tooltip("ความเร็ว Fade-Out ก่อนเข้า Summary (วินาที)")]
    public float fadeOutDuration = 0.8f;
    [Tooltip("ความเร็ว Fade-In หลังเปิด Summary (วินาที)")]
    public float fadeInDuration  = 0.6f;

    // ─── Count-Up Animation ───────────────────────────────────────────
    [Header("Count-Up Animation")]
    [Tooltip("ระยะเวลา animation ตัวเลข (วินาที)")]
    public float countUpDuration = 1.8f;

    // ─── Divider (optional) ───────────────────────────────────────────
    [Tooltip("เส้นคั่น (Prefab Image บางๆ) — ใส่ไว้ระหว่าง category (optional)")]
    public GameObject dividerPrefab;

    // ─── Day Banner ───────────────────────────────────────────────────
    [Header("Day Banner")]
    public GameObject bannerPanel;
    public TextMeshProUGUI bannerText;
    public float bannerDuration = 2.5f;
    public float bannerFadeTime = 0.5f;

    [Header("SFX")]
    public AudioSource audioSource;
    public AudioClip sleepSound;
    public AudioClip morningSound;

    // ─── Runtime ──────────────────────────────────────────────────────
    bool _triggeredToday;
    bool _isSummaryOpen;

    // เก็บ TMP + target value สำหรับ count-up animation
    readonly List<(TextMeshProUGUI tmp, int target)> _animTargets
        = new List<(TextMeshProUGUI, int)>();

    static readonly SellCategory[] ALL_CATEGORIES =
    {
        SellCategory.Farming,
        SellCategory.Fishing,
        SellCategory.Ore,
        SellCategory.Other,
    };

    static readonly string[] CATEGORY_NAMES =
    {
        "Farming",
        "Fishing",
        "Ore",
        "Other",
    };

    // ──────────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    void Start()
    {
        if (summaryPanel) summaryPanel.SetActive(false);
        if (bannerPanel)  bannerPanel.SetActive(false);

        // ซ่อน FadePanel ตั้งต้น
        if (fadePanel)
        {
            fadePanel.alpha          = 0f;
            fadePanel.blocksRaycasts = false;
            fadePanel.gameObject.SetActive(true);
        }

        if (sleepButton) sleepButton.onClick.AddListener(OnSleepPressed);

        if (CalendarSystem.Instance != null)
            CalendarSystem.Instance.OnDayEnded += _ => _triggeredToday = false;
    }

    void Update()
    {
        if (_triggeredToday || _isSummaryOpen) return;
        if (TimeOfDaySystem.Instance == null) return;

        if (TimeOfDaySystem.Instance.Hour == bedtimeHour)
        {
            _triggeredToday = true;
            StartCoroutine(TriggerDayEnd());
        }
    }

    // ─── Flow ─────────────────────────────────────────────────────────

    IEnumerator TriggerDayEnd()
    {
        if (TimeOfDaySystem.Instance) TimeOfDaySystem.Instance.IsPaused = true;

        var player = FindObjectOfType<PlayerController>();
        player?.SetBusy(true);

        if (audioSource && sleepSound) audioSource.PlayOneShot(sleepSound);

        // ── Fade Out (จอมืด) ─────────────────────────────────────────
        yield return StartCoroutine(Fade(0f, 1f, fadeOutDuration));

        // เปิด Summary ขณะจอมืด
        ShowSummary();

        yield return new WaitForSeconds(0.1f);

        // ── Fade In (จอสว่าง เห็น Summary) ──────────────────────────
        yield return StartCoroutine(Fade(1f, 0f, fadeInDuration));
    }

    // ─── Screen Fade ──────────────────────────────────────────────────

    IEnumerator Fade(float from, float to, float duration)
    {
        if (fadePanel == null) yield break;

        fadePanel.blocksRaycasts = true;
        fadePanel.alpha = from;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed     += Time.deltaTime;
            fadePanel.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }

        fadePanel.alpha = to;

        // ถ้า fade ออกจนโปร่งใส → หยุด block raycasts
        if (to <= 0f) fadePanel.blocksRaycasts = false;
    }

    // ─── Summary ──────────────────────────────────────────────────────

    void ShowSummary()
    {
        _isSummaryOpen = true;
        _animTargets.Clear();

        // ลบ rows เก่า
        foreach (Transform child in rowsParent)
            Destroy(child.gameObject);

        // Title
        if (summaryTitleText && CalendarSystem.Instance != null)
        {
            var d = CalendarSystem.Instance.date;
            summaryTitleText.text = $"Day {d.day}  —  Year {d.year}";
        }

        var tracker = DailyEconomyTracker.Instance;

        // ─── แสดงทุก category ────────────────────────────────────────
        for (int i = 0; i < ALL_CATEGORIES.Length; i++)
        {
            var cat      = ALL_CATEGORIES[i];
            var catName  = CATEGORY_NAMES[i];
            int catTotal = tracker != null ? tracker.GetCategoryTotal(cat) : 0;
            var records  = tracker != null ? tracker.GetRecordsByCategory(cat) : null;

            // ── หัว category ─────────────────────────────────────────
            SpawnCategoryHeader(catName, catTotal);

            // ── รายการไอเท็มใน category ──────────────────────────────
            if (records != null && records.Count > 0)
            {
                foreach (var rec in records)
                    SpawnItemRow(rec);
            }
            else
            {
                SpawnItemRow(null, "(ไม่มีการขาย)", 0, 0);
            }

            // เส้นคั่น (ถ้ามี prefab)
            if (dividerPrefab && i < ALL_CATEGORIES.Length - 1)
                Instantiate(dividerPrefab, rowsParent);
        }

        // ─── Total (เริ่มที่ ¥0 รอ animation) ───────────────────────
        int grandTotal = tracker != null ? tracker.TotalEarnedToday : 0;
        if (totalText) totalText.text = "Total   ¥0";

        if (summaryPanel) summaryPanel.SetActive(true);

        // เริ่ม count-up animation
        StartCoroutine(AnimateCountUp(grandTotal));
    }

    // ─── Count-Up Animation ───────────────────────────────────────────

    IEnumerator AnimateCountUp(int grandTotal)
    {
        // รอ 1 frame ให้ UI render ก่อน
        yield return null;

        float elapsed  = 0f;
        float duration = Mathf.Max(0.1f, countUpDuration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t    = Mathf.Clamp01(elapsed / duration);
            float ease = 1f - Mathf.Pow(1f - t, 3f); // Ease-Out Cubic

            // อัปเดตทุก TMP ที่ register ไว้
            foreach (var (tmp, target) in _animTargets)
            {
                if (tmp == null) continue;
                int current = Mathf.RoundToInt(target * ease);
                tmp.text = $"¥{current:N0}";
            }

            // อัปเดต Total
            if (totalText)
                totalText.text = $"Total   ¥{Mathf.RoundToInt(grandTotal * ease):N0}";

            yield return null;
        }

        // Snap ไปค่าจริงเมื่อ animation จบ
        foreach (var (tmp, target) in _animTargets)
        {
            if (tmp == null) continue;
            tmp.text = $"¥{target:N0}";
        }
        if (totalText)
            totalText.text = $"Total   ¥{grandTotal:N0}";
    }

    // ─── Spawn Helpers ────────────────────────────────────────────────

    void SpawnCategoryHeader(string catName, int catTotal)
    {
        if (!categoryHeaderPrefab || !rowsParent) return;

        var obj = Instantiate(categoryHeaderPrefab, rowsParent);

        var label = obj.transform.Find("CatLabel")?.GetComponent<TextMeshProUGUI>();
        var value = obj.transform.Find("CatValue")?.GetComponent<TextMeshProUGUI>();

        if (label) label.text = catName;
        if (value)
        {
            value.text  = "¥0"; // เริ่มที่ 0 รอ animation
            value.color = catTotal > 0 ? new Color(0.2f, 0.7f, 0.2f) : Color.gray;

            // register เฉพาะที่มีเงิน
            if (catTotal > 0)
                _animTargets.Add((value, catTotal));
            else
                value.text = "¥0"; // ไม่มีการขาย ไม่ต้อง animate
        }
    }

    void SpawnItemRow(DailyEconomyTracker.SoldItemRecord rec)
    {
        if (rec == null) return;
        SpawnItemRow(rec.icon, rec.itemName, rec.amount, rec.totalPrice);
    }

    void SpawnItemRow(Sprite icon, string name, int amount, int price)
    {
        if (!itemRowPrefab || !rowsParent) return;

        var obj = Instantiate(itemRowPrefab, rowsParent);

        // Icon
        var iconImg = obj.transform.Find("ItemIcon")?.GetComponent<Image>();
        if (iconImg)
        {
            if (icon != null) { iconImg.sprite = icon; iconImg.enabled = true; }
            else iconImg.enabled = false;
        }

        // Label
        var labelTmp = obj.transform.Find("ItemLabel")?.GetComponent<TextMeshProUGUI>();
        if (labelTmp)
        {
            labelTmp.text  = amount > 0 ? $"{name}  x{amount}" : name;
            labelTmp.color = amount > 0 ? Color.white : Color.gray;
        }

        // Value — animate ถ้ามีราคา
        var valueTmp = obj.transform.Find("ItemValue")?.GetComponent<TextMeshProUGUI>();
        if (valueTmp)
        {
            if (price > 0)
            {
                valueTmp.text  = "¥0"; // เริ่มที่ 0 รอ animation
                valueTmp.color = new Color(1f, 0.9f, 0.3f);
                _animTargets.Add((valueTmp, price));
            }
            else
            {
                valueTmp.text  = "-";
                valueTmp.color = Color.gray;
            }
        }
    }

    // ─── Sleep ────────────────────────────────────────────────────────

    void OnSleepPressed()
    {
        if (!_isSummaryOpen) return;
        StartCoroutine(FinishDay());
    }

    IEnumerator FinishDay()
    {
        // 1. Fade Out — จอมืด (player ยังขยับไม่ได้ — SetBusy ยังเป็น true อยู่)
        yield return StartCoroutine(Fade(0f, 1f, fadeOutDuration));

        if (summaryPanel) summaryPanel.SetActive(false);
        _isSummaryOpen = false;

        // 2. จ่ายเงิน + รีเซ็ต + เปลี่ยนวัน (ทำขณะจอดำ)
        int earned = DailyEconomyTracker.Instance != null
            ? DailyEconomyTracker.Instance.TotalEarnedToday
            : 0;
        PlayerWallet.Instance?.Add(earned);

        DailyEconomyTracker.Instance?.ResetDaily();

        if (CalendarSystem.Instance != null)
            CalendarSystem.Instance.NextDay();

        if (TimeOfDaySystem.Instance)
            TimeOfDaySystem.Instance.SetTime(wakeHour, wakeMinute);

        if (TimeOfDaySystem.Instance) TimeOfDaySystem.Instance.IsPaused = false;

        // 3. เปิด Morning SFX
        if (audioSource && morningSound) audioSource.PlayOneShot(morningSound);

        // 4. Day Banner ขึ้นบนจอดำ — ยังไม่ Fade In กลับโลก
        yield return StartCoroutine(ShowDayBanner());

        // 5. Fade In กลับมาเห็นโลก — หลัง Banner หายแล้ว
        yield return StartCoroutine(Fade(1f, 0f, fadeInDuration));

        // 6. ปลดล็อก player หลัง Fade In เสร็จ
        var player = FindObjectOfType<PlayerController>();
        player?.SetBusy(false);
    }

    IEnumerator ShowDayBanner()
    {
        if (bannerPanel == null || bannerText == null) yield break;

        if (CalendarSystem.Instance != null)
        {
            var d = CalendarSystem.Instance.date;
            bannerText.text = $"Day {d.day}";
        }

        bannerPanel.SetActive(true);

        var cg = bannerPanel.GetComponent<CanvasGroup>();
        if (cg == null) cg = bannerPanel.AddComponent<CanvasGroup>();

        // Fade in
        cg.alpha = 0f;
        for (float t = 0; t < bannerFadeTime; t += Time.deltaTime)
        {
            cg.alpha = t / bannerFadeTime;
            yield return null;
        }
        cg.alpha = 1f;

        yield return new WaitForSeconds(bannerDuration);

        // Fade out
        for (float t = 0; t < bannerFadeTime; t += Time.deltaTime)
        {
            cg.alpha = 1f - t / bannerFadeTime;
            yield return null;
        }

        bannerPanel.SetActive(false);
    }

    /// <summary>
    /// เรียกจาก BedInteraction — นอนได้ทุกเวลา
    /// </summary>
    public void ForceSleep()
    {
        if (_isSummaryOpen) return;
        if (_triggeredToday) return; // กำลัง process อยู่แล้ว
        _triggeredToday = true;
        StartCoroutine(TriggerDayEnd());
    }
}
