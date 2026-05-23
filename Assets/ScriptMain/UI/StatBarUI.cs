using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class StatBarUI : MonoBehaviour
{
    [Header("Health Linear")]
    [SerializeField] private Slider healthBar;
    // [SerializeField] private TextMeshProUGUI healthText;

    [Header("Stamina Linear")]
    [SerializeField] private Slider staminaBar;
    // [SerializeField] private TextMeshProUGUI staminaText;

    [Header("Hungry Circular")]
    [SerializeField] private CircularProgress hungryCircular;

    private StatManager _statManager;

    public void BindPlayer(StatManager statManager)
    {
        if (_statManager != null)
            _statManager.AllStats.OnListChanged -= OnStatsChanged;

        _statManager = statManager;
        _statManager.AllStats.OnListChanged += OnStatsChanged;

        // Only refresh if data is ready
        if (_statManager.AllStats.Count > 0)
            RefreshAll();
        else
            Debug.Log("[StatBarUI] Stats not synced yet — waiting for OnListChanged");
    }

    private void OnEnable()
    {
        if (_statManager != null)
        {
            _statManager.AllStats.OnListChanged -= OnStatsChanged;
            _statManager.AllStats.OnListChanged += OnStatsChanged;

            // Only refresh immediately if data already exists
            if (_statManager.AllStats.Count > 0)
                RefreshAll();
            // else: wait for OnListChanged to fire when NGO syncs the data
        }
    }


    private void OnDisable()
    {
        if (_statManager != null)
            _statManager.AllStats.OnListChanged -= OnStatsChanged;
    }

    private void OnStatsChanged(NetworkListEvent<NetworkStat> changeEvent)
    {
        Debug.Log("Stats changed, refreshing UI...");
        RefreshAll();
    }

    private void RefreshAll()
    {
        if (_statManager == null) return;

        foreach (var stat in _statManager.AllStats)
        {
            // Debug.Log($"Updating UI for {stat.Type}: {stat.CurrentValue}/{stat.MaxValue}");
            switch (stat.Type)
            {
                case StatType.Health:
                    SetLinear(healthBar,
                        stat.CurrentValue, stat.MaxValue);
                    break;

                case StatType.Stamina:
                    SetLinear(staminaBar,
                        stat.CurrentValue, stat.MaxValue);
                    break;

                case StatType.Energy:
                    SetCircular(hungryCircular,
                        stat.CurrentValue, stat.MaxValue);
                    break;
            }
        }
    }

    private void SetLinear(Slider slider,
        float current, float max)
    {
        if (slider == null) return;
        slider.minValue = 0;
        slider.maxValue = max;
        slider.value = current;

        // if (label != null)
        //     label.text = $"{Mathf.CeilToInt(current)}/{Mathf.CeilToInt(max)}";
    }

    private void SetCircular(CircularProgress circular,
        float current, float max)
    {
        if (circular == null) return;
        circular.SetProgress(current, max);
    }
}
