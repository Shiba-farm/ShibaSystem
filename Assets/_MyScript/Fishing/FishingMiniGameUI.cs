using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Mini-game ตกปลา — ปลาวิ่งบน bar แสดงตัวอักษรสุ่ม → กด key ให้ถูก
///
/// UI Setup ใน Canvas:
///   FishingMiniGamePanel (Panel)
///     └── Bar (Image — แถบแนวนอน เช่น กว้าง 700 สูง 100)
///           ├── CatchZone (Image สีฟ้าอ่อน — อยู่ทางขวา, anchor right)
///           └── FishContainer (RectTransform — เคลื่อนที่ตามปลา)
///                 ├── FishIcon (Image — รูปปลา)
///                 └── KeyBubble (Image — วงกลมสีเหลือง)
///                       └── KeyText (TextMeshProUGUI — ตัวอักษรที่ต้องกด)
///     └── InstructionText (TMP — "กด A เพื่อจับปลา!")
///     └── ResultText      (TMP — Perfect! / Miss...)
///     └── TimerBar (Image — Filled, แสดงเวลาที่เหลือ)
/// </summary>
public class FishingMiniGameUI : MonoBehaviour
{
    public static FishingMiniGameUI Instance { get; private set; }

    // ─── UI References ────────────────────────────────────────────────
    [Header("Panel")]
    public GameObject panel;

    [Header("Bar")]
    [Tooltip("RectTransform ของ bar ทั้งเส้น (ใช้คำนวณตำแหน่ง)")]
    public RectTransform bar;
    [Tooltip("CatchZone (พื้นที่ขวา ที่ต้องกด key ให้ทัน)")]
    public RectTransform catchZone;
    [Tooltip("FishContainer — RectTransform ที่เคลื่อนที่ (ลูกของ bar)")]
    public RectTransform fishContainer;
    [Tooltip("KeyText — ตัวอักษรบนปลา")]
    public TextMeshProUGUI keyText;

    [Header("Feedback")]
    public TextMeshProUGUI instructionText;
    public TextMeshProUGUI resultText;
    [Tooltip("Image type = Filled (Horizontal) แสดงเวลาที่เหลือ")]
    public Image timerBar;

    // ─── Config ───────────────────────────────────────────────────────
    [Header("Config")]
    [Tooltip("ความเร็วปลา (px/วินาที)")]
    public float fishSpeed = 220f;
    [Tooltip("เวลาที่ให้กด key ก่อนที่ปลาจะหนี (วินาที)")]
    public float timeLimit = 4f;
    [Tooltip("ความกว้าง catch zone (px) — ยิ่งแคบยิ่งยาก")]
    public float catchZoneWidth = 140f;
    [Tooltip("ตัวอักษรที่สุ่มให้กด")]
    public string[] keyPool = new string[] { "A", "S", "D", "F", "J", "K", "L", "Q", "E", "R" };

    [Header("Colors")]
    public Color catchZoneColor   = new Color(0.4f, 0.85f, 1f,  0.55f);
    public Color perfectColor     = new Color(0.1f, 0.9f,  0.1f, 1f);
    public Color goodColor        = new Color(0.9f, 0.85f, 0.1f, 1f);
    public Color missColor        = new Color(0.9f, 0.2f,  0.2f, 1f);
    public Color timerNormal      = new Color(0.2f, 0.75f, 1f,  1f);
    public Color timerWarning     = new Color(1f,   0.4f,  0.1f, 1f);

    // ─── Events ───────────────────────────────────────────────────────
    public event Action<CatchResult> OnResult;
    public enum CatchResult { Perfect, Good, Miss }

    // ─── Runtime ──────────────────────────────────────────────────────
    float _barHalfWidth;    // ครึ่งความกว้าง bar
    float _fishPos;         // anchoredPosition.x ของปลา (-barHalfWidth .. +barHalfWidth)
    float _fishDir = 1f;
    float _elapsed;
    bool  _isPlaying;
    KeyCode _targetKey;
    string  _targetKeyName;
    Coroutine _endRoutine;

    // ──────────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;

        if (panel) panel.SetActive(false);
        if (resultText) resultText.text = "";

        // ตั้งสี catch zone
        if (catchZone)
        {
            var img = catchZone.GetComponent<Image>();
            if (img) img.color = catchZoneColor;
            catchZone.sizeDelta = new Vector2(catchZoneWidth, catchZone.sizeDelta.y);
            // anchor ไปทางขวา
            catchZone.anchorMin = new Vector2(1f, 0f);
            catchZone.anchorMax = new Vector2(1f, 1f);
            catchZone.pivot     = new Vector2(1f, 0.5f);
            catchZone.anchoredPosition = Vector2.zero;
        }
    }

    void Update()
    {
        if (!_isPlaying) return;

        float dt = Time.deltaTime;
        _elapsed += dt;

        // ─── เคลื่อนปลา ─────────────────────────────────────────────
        _fishPos += _fishDir * fishSpeed * dt;
        if (_fishPos >= _barHalfWidth)  { _fishPos = _barHalfWidth;  _fishDir = -1f; }
        if (_fishPos <= -_barHalfWidth) { _fishPos = -_barHalfWidth; _fishDir =  1f; }

        if (fishContainer)
            fishContainer.anchoredPosition = new Vector2(_fishPos, fishContainer.anchoredPosition.y);

        // ─── Timer bar ───────────────────────────────────────────────
        if (timerBar)
        {
            float ratio = 1f - (_elapsed / timeLimit);
            timerBar.fillAmount = Mathf.Clamp01(ratio);
            timerBar.color = ratio > 0.35f ? timerNormal : timerWarning;
        }

        // ─── หมดเวลา → miss ─────────────────────────────────────────
        if (_elapsed >= timeLimit)
        {
            EndGame(CatchResult.Miss, "💨 หมดเวลา...");
            return;
        }

        // ─── ตรวจ Key Input ──────────────────────────────────────────
        if (Input.anyKeyDown)
        {
            if (Input.GetKeyDown(_targetKey))
            {
                // กด key ถูก — ตรวจว่าปลาอยู่ใน catch zone ไหม
                bool inZone = IsFishInCatchZone();
                if (inZone)
                {
                    // perfect = ปลาอยู่ใน 40% กลาง catch zone
                    float catchRight = _barHalfWidth;
                    float catchLeft  = catchRight - catchZoneWidth;
                    float zoneMid    = (catchLeft + catchRight) * 0.5f;
                    float perfectHalf = catchZoneWidth * 0.2f;
                    bool isPerfect   = Mathf.Abs(_fishPos - zoneMid) <= perfectHalf;

                    EndGame(isPerfect ? CatchResult.Perfect : CatchResult.Good,
                            isPerfect ? "✨ Perfect!" : "👍 Good!");
                }
                else
                {
                    // กดถูกตัวอักษรแต่ปลาอยู่นอก zone
                    EndGame(CatchResult.Miss, "💨 เร็วเกินไป!");
                }
            }
            else
            {
                // กดผิดปุ่ม
                EndGame(CatchResult.Miss, "❌ กดผิด!");
            }
        }
    }

    // ──────────────────────────────────────────────────────────────────
    /// <summary>เปิด mini-game — difficulty 1 = ปกติ, สูงขึ้น = เร็วขึ้น + catch zone แคบลง</summary>
    public void Show(float difficulty = 1f)
    {
        if (_endRoutine != null) { StopCoroutine(_endRoutine); _endRoutine = null; }

        // คำนวณ barHalfWidth จาก RectTransform จริง
        _barHalfWidth = bar != null ? bar.rect.width * 0.5f : 350f;

        // ปรับ difficulty
        float speedMult = Mathf.Lerp(1f, 2.2f, Mathf.Clamp01((difficulty - 1f) / 4f));
        float zoneScale = Mathf.Lerp(1f, 0.5f, Mathf.Clamp01((difficulty - 1f) / 4f));

        float adjustedZone = catchZoneWidth * zoneScale;
        if (catchZone) catchZone.sizeDelta = new Vector2(adjustedZone, catchZone.sizeDelta.y);

        // สุ่มปลาเริ่มจากซ้าย
        _fishPos  = -_barHalfWidth;
        _fishDir  = 1f;
        _elapsed  = 0f;
        _isPlaying = true;

        // สุ่ม key
        int idx = UnityEngine.Random.Range(0, keyPool.Length);
        _targetKeyName = keyPool[idx];
        _targetKey     = (KeyCode)System.Enum.Parse(typeof(KeyCode), _targetKeyName);

        // ตั้ง speed
        fishSpeed = 220f * speedMult;

        // UI
        if (keyText) keyText.text = _targetKeyName;
        if (instructionText) instructionText.text = $"กด  <b>{_targetKeyName}</b>  ตอนปลาอยู่ในโซนสีฟ้า!";
        if (resultText) resultText.text = "";
        if (timerBar) timerBar.fillAmount = 1f;

        if (panel) panel.SetActive(true);
    }

    /// <summary>ซ่อน mini-game (FishingSystem เรียก)</summary>
    public void Hide()
    {
        _isPlaying = false;
        if (panel) panel.SetActive(false);
    }

    // ──────────────────────────────────────────────────────────────────

    bool IsFishInCatchZone()
    {
        // catch zone อยู่ทางขวา: ตั้งแต่ (barHalfWidth - catchZoneWidth) ถึง barHalfWidth
        float zoneWidth = catchZone ? catchZone.sizeDelta.x : catchZoneWidth;
        float catchLeft = _barHalfWidth - zoneWidth;
        return _fishPos >= catchLeft && _fishPos <= _barHalfWidth;
    }

    void EndGame(CatchResult result, string msg)
    {
        _isPlaying = false;

        // แสดงผล
        Color col = result == CatchResult.Perfect ? perfectColor
                  : result == CatchResult.Good    ? goodColor
                  :                                 missColor;

        if (resultText)
        {
            resultText.text  = msg;
            resultText.color = col;
        }
        if (instructionText) instructionText.text = "";

        // ปิดหน้าต่างหลังแสดงผลสักครู่
        _endRoutine = StartCoroutine(HideAfterDelay(0.7f));

        OnResult?.Invoke(result);
    }

    IEnumerator HideAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Hide();
        _endRoutine = null;
    }
}
