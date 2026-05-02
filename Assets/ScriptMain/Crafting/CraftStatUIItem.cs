using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CraftStatUIItem : MonoBehaviour
{
    [Header("Craft Stat Item")]
    [SerializeField] private TextMeshProUGUI craftStatName;
    [SerializeField] private Slider craftStatValue;

    internal void Setup(ItemStatDataSO.StatModifier itemStat)
    {
        craftStatName.text = itemStat.Type.ToString();
        craftStatValue.value = itemStat.Amount;
        craftStatValue.maxValue = 100; // Assuming the slider's max value is the same as the stat amount for display purposes
    }
}
