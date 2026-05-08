using TMPro;
using UnityEngine;

public class GeneralInfoPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private TextMeshProUGUI calendarText;
    [SerializeField] private TextMeshProUGUI goldText;

    [Header("Signals")]
    [SerializeField] private CurrencySignal currencySignal;
    [SerializeField] private WorldTimeSignal timeSignal;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void OnEnable()
    {
        currencySignal.OnGoldChanged += RefreshGoldDisplay;
        timeSignal.OnTimeChanged += RefreshDateTimeDisplay;

        // Show immediately if time already running
        if (timeSignal.CurrentTime.Hour != 0 || timeSignal.CurrentTime.Minute != 0)
        RefreshDateTimeDisplay(timeSignal.CurrentTime);

        RefreshGoldDisplay(currencySignal.CurrentGold);
    }

    void OnDisable()
    {
        currencySignal.OnGoldChanged -= RefreshGoldDisplay;
        timeSignal.OnTimeChanged -= RefreshDateTimeDisplay;
    }

    private void RefreshGoldDisplay(long amount)
    {
        goldText.text = $"$ {amount.ToString("N0")}"; // "1,000,000" formatting
    }

    private void RefreshDateTimeDisplay(WorldTimeData time)
    {
        timeText.text = time.FormattedTime;
        calendarText.text = time.FormattedDate;
    }
}
