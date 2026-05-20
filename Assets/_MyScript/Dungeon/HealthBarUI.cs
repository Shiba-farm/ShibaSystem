// HealthBarUI.cs
// แสดงหลอดเลือดใน Dungeon Scene

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HealthBarUI : MonoBehaviour
{
    [Header("UI Refs")]
    public Slider        hpSlider;
    public TextMeshProUGUI hpText;   // "80 / 100"  (optional)
    public Image         fillImage;  // เปลี่ยนสีตาม HP (optional)

    [Header("Colors")]
    public Color colorHigh   = Color.green;
    public Color colorMid    = Color.yellow;
    public Color colorLow    = Color.red;

    // ──────────────────────────────────────────────────────────────────────
    void OnEnable()  => PlayerHealth.OnHPChanged += UpdateBar;
    void OnDisable() => PlayerHealth.OnHPChanged -= UpdateBar;

    void Start()
    {
        if (PlayerHealth.Instance)
            UpdateBar(PlayerHealth.Instance.currentHP, PlayerHealth.Instance.maxHP);
    }

    // ──────────────────────────────────────────────────────────────────────
    private void UpdateBar(int current, int max)
    {
        if (hpSlider)
        {
            hpSlider.maxValue = max;
            hpSlider.value    = current;
        }

        if (hpText)
            hpText.text = $"{current} / {max}";

        if (fillImage)
        {
            float ratio = max > 0 ? (float)current / max : 0f;
            fillImage.color = ratio > 0.5f ? colorHigh
                            : ratio > 0.25f ? colorMid
                            : colorLow;
        }
    }
}
