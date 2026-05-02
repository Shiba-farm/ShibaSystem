using UnityEngine;
using TMPro;

public class MoneyUI : MonoBehaviour
{
    public TextMeshProUGUI label;

    private void Start()
    {
        if (PlayerWallet.Instance != null)
        {
            UpdateText(PlayerWallet.Instance.Money);
            PlayerWallet.Instance.OnMoneyChanged += UpdateText;
        }
    }

    private void OnDestroy()
    {
        if (PlayerWallet.Instance != null)
            PlayerWallet.Instance.OnMoneyChanged -= UpdateText;
    }

    void UpdateText(int value)
    {
        if (label) label.text = $"$ {value:n0}";
    }
}
