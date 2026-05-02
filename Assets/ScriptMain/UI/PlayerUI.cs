using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour
{
    public static PlayerUI Instance;
    [SerializeField] private Slider _healthslider;
    [SerializeField] private Slider _energyslider;
    [SerializeField] private Slider _staminaslider;

    void Awake()
    {
        Instance = this;
    }
    public void BindPlayer(StatManager playerStats)
    {
        playerStats.AllStats.OnListChanged -= OnStatsChanged;

        playerStats.AllStats.OnListChanged += OnStatsChanged;

        foreach (var stat in playerStats.AllStats)
        {
            UpdateUIFromStat(stat);
        }
    }

    private void OnStatsChanged(NetworkListEvent<NetworkStat> changeEvent)
    {
        if (changeEvent.Type == NetworkListEvent<NetworkStat>.EventType.Value ||
            changeEvent.Type == NetworkListEvent<NetworkStat>.EventType.Add)
        {
            UpdateUIFromStat(changeEvent.Value);
        }
    }

    private void UpdateUIFromStat(NetworkStat stat)
    {
        switch (stat.Type)
        {
            case StatType.Health:
                _healthslider.maxValue = stat.MaxValue;
                _healthslider.value = stat.CurrentValue;
                break;

            case StatType.Energy:
                _energyslider.maxValue = stat.MaxValue;
                _energyslider.value = stat.CurrentValue;
                break;

            case StatType.Stamina:
                _staminaslider.maxValue = stat.MaxValue;
                _staminaslider.value = stat.CurrentValue;
                break;
        }
    }

    private void UpdateHealthSlider(float previousValue, float newValue)
    {
        _healthslider.value = newValue;
    }
    private void UpdateEnergySlider(float previousValue, float newValue)
    {
        _healthslider.value = newValue;
    }
}
