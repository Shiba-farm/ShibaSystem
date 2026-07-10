using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// แสดง "+amount" pop-up ลอยขึ้นพร้อม fade out เมื่อยอดเงินเพิ่มขึ้น
///
/// วิธีติดตั้ง:
///   1. สร้าง child GameObject ชื่อ "MoneyGainPopup" ใต้ MoneyText (หรือ Money_M)
///   2. เพิ่ม TextMeshProUGUI บน child GameObject นั้น → ตั้งขนาด/สี/font ตามต้องการ
///   3. ติด script นี้บน child GameObject เดียวกัน
///   4. ลาก CurrencySignal และ TextMeshProUGUI ลงใน Inspector
///   5. ใน Start ของ child TMP → ซ่อน (SetActive false) ไว้ก่อน
/// </summary>
public class MoneyGainAnimationUI : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private CurrencySignal currencySignal;

    [Header("Popup Text")]
    [Tooltip("TextMeshProUGUI ที่จะแสดง +amount (ควรเป็น child ของ script นี้)")]
    [SerializeField] private TextMeshProUGUI popupText;

    [Header("Animation")]
    [Tooltip("ระยะเวลา animation ทั้งหมด (วินาที)")]
    [SerializeField] private float duration = 1.4f;

    [Tooltip("ระยะที่ข้อความลอยขึ้น (px ใน Canvas)")]
    [SerializeField] private float riseDistance = 50f;

    [Tooltip("ดีเลย์ก่อนเริ่ม fade out (0–1 เทียบสัดส่วน duration)")]
    [SerializeField] [Range(0f, 0.9f)] private float fadeStartRatio = 0.4f;

    // ── Internal ──────────────────────────────────────────────────────────────
    private long _previousGold;
    private Coroutine _animRoutine;

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    private void OnEnable()
    {
        _previousGold = currencySignal != null ? currencySignal.CurrentGold : 0L;
        if (currencySignal != null)
            currencySignal.OnGoldChanged += HandleGoldChanged;

        if (popupText != null)
            popupText.gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        if (currencySignal != null)
            currencySignal.OnGoldChanged -= HandleGoldChanged;
    }

    // ── Handler ───────────────────────────────────────────────────────────────
    private void HandleGoldChanged(long newGold)
    {
        long delta = newGold - _previousGold;
        _previousGold = newGold;

        if (delta <= 0) return; // ลด / ไม่เปลี่ยน → ไม่ต้องแสดง

        if (_animRoutine != null)
            StopCoroutine(_animRoutine);

        _animRoutine = StartCoroutine(PlayPopup(delta));
    }

    // ── Animation ─────────────────────────────────────────────────────────────
    private IEnumerator PlayPopup(long amount)
    {
        if (popupText == null) yield break;

        // ตั้งค่าข้อความ
        popupText.text = $"+{amount:N0}";
        popupText.gameObject.SetActive(true);

        RectTransform rt = popupText.rectTransform;
        rt.anchoredPosition = Vector2.zero;

        // Reset alpha
        Color c = popupText.color;
        c.a = 1f;
        popupText.color = c;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // ลอยขึ้น (ease-out)
            float eased = 1f - (1f - t) * (1f - t);
            rt.anchoredPosition = new Vector2(0f, eased * riseDistance);

            // Fade out หลังจาก fadeStartRatio ผ่านไป
            float alpha = t < fadeStartRatio
                ? 1f
                : 1f - Mathf.Clamp01((t - fadeStartRatio) / (1f - fadeStartRatio));

            c = popupText.color;
            c.a = alpha;
            popupText.color = c;

            yield return null;
        }

        popupText.gameObject.SetActive(false);
        _animRoutine = null;
    }
}
