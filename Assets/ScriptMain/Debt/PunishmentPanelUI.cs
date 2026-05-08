using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PunishmentPanelUI : MonoBehaviour
{
    [Header("Info")]
    [SerializeField] private TextMeshProUGUI leftToPayText;
    [SerializeField] private TextMeshProUGUI totalValueText;   // "To pay : 10500"
    [SerializeField] private Button refuseButton;
    [SerializeField] private Button tradeButton;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void OnEnable()
    {
        if (DebtManager.Instance == null) return;

        DebtManager.Instance.OnTradeValueChanged += PopulateUI;
        tradeButton.onClick.AddListener(OnTradeClick);
        PopulateUI();
    }
    private void OnDisable()
    {
        if (DebtManager.Instance == null) return;

        DebtManager.Instance.OnTradeValueChanged -= PopulateUI;
        tradeButton.onClick.RemoveListener(OnTradeClick);
    }

    private void PopulateUI()
    {
        int remaining =  DebtManager.Instance.RemainingDueThisMonth;
        int tradeValue = DebtManager.Instance.CurrentTradeValue;
        leftToPayText.text = $"Left to Pay : {remaining}";
        totalValueText.text = $"Total value : {tradeValue}";
    }

    public void OnTradeClick()
    {
        Debug.Log("Trading....");
    }
}
