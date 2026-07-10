using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// แสดงเงินปัจจุบันที่มุมขวาบนของหน้าต่างเมนู
/// ตัวเลขนับขึ้นแบบ smooth เมื่อยอดเงินเพิ่ม (sync กับ MoneyGainAnimationUI)
/// </summary>
public class MenuMoneyDisplayUI : MonoBehaviour
{
    [SerializeField] private CurrencySignal currencySignal;
    [SerializeField] private TextMeshProUGUI moneyText;

    [Tooltip("ข้อความนำหน้าตัวเลข เช่น '$ ' หรือ ''")]
    [SerializeField] private string prefix = "$ ";

    [Header("Count-up Animation")]
    [Tooltip("เปิด/ปิด animation นับขึ้น")]
    [SerializeField] private bool animateCountUp = true;
    [Tooltip("ระยะเวลา count-up (วินาที)")]
    [SerializeField] private float countDuration = 0.6f;

    private long _displayedGold;
    private Coroutine _countRoutine;

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    private void OnEnable()
    {
        if (currencySignal == null) return;
        currencySignal.OnGoldChanged += HandleGoldChanged;

        _displayedGold = currencySignal.CurrentGold;
        SetText(_displayedGold);
    }

    private void OnDisable()
    {
        if (currencySignal == null) return;
        currencySignal.OnGoldChanged -= HandleGoldChanged;

        if (_countRoutine != null)
        {
            StopCoroutine(_countRoutine);
            _countRoutine = null;
        }
    }

    // ── Handler ───────────────────────────────────────────────────────────────
    private void HandleGoldChanged(long newGold)
    {
        if (!animateCountUp || newGold == _displayedGold)
        {
            _displayedGold = newGold;
            SetText(newGold);
            return;
        }

        if (_countRoutine != null) StopCoroutine(_countRoutine);
        _countRoutine = StartCoroutine(CountUp(_displayedGold, newGold));
    }

    // ── Animation ─────────────────────────────────────────────────────────────
    private IEnumerator CountUp(long from, long to)
    {
        float elapsed = 0f;
        while (elapsed < countDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / countDuration);
            // ease-out cubic
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            // ใช้ long lerp เพื่อป้องกัน precision loss กับค่าสูง
            _displayedGold = from + (long)((to - from) * eased);
            SetText(_displayedGold);
            yield return null;
        }

        _displayedGold = to;
        SetText(to);
        _countRoutine = null;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private void SetText(long amount)
    {
        if (moneyText != null)
            moneyText.text = prefix + amount.ToString("N0");
    }
}
