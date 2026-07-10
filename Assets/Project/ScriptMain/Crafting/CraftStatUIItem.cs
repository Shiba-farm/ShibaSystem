using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CraftStatUIItem : MonoBehaviour
{
    [Header("Craft Stat Item")]
    [SerializeField] private TextMeshProUGUI craftStatName;
    [SerializeField] private TextMeshProUGUI craftStatValueText; // แสดงตัวเลขจริง
    [SerializeField] private Slider          craftStatSlider;    // visual bar (optional)

    [Tooltip("ค่าสูงสุดของ slider (ตั้งใน Inspector ตาม stat ของเกม)")]
    [SerializeField] private float sliderMaxValue = 100f;

    internal void Setup(ItemStatDataSO.StatModifier itemStat)
    {
        craftStatName.text = itemStat.Type.ToString();

        // แสดงตัวเลขจริงเสมอ
        if (craftStatValueText != null)
            craftStatValueText.text = itemStat.Amount.ToString("0.#");

        // Slider เป็น visual เสริม — clamp ให้อยู่ใน range ที่ตั้งไว้
        if (craftStatSlider != null)
        {
            craftStatSlider.maxValue = sliderMaxValue;
            craftStatSlider.value    = Mathf.Clamp(itemStat.Amount, 0f, sliderMaxValue);
        }
    }
}
