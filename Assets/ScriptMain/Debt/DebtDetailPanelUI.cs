using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DebtDetailPanelUI : MonoBehaviour
{
    [Header("Info")]
    [SerializeField] private TextMeshProUGUI monthText;
    [SerializeField] private TextMeshProUGUI minimumPayText;   // "To pay : 10500"
    [SerializeField] private TextMeshProUGUI currentDebtText;  // "Debt : 90000"
    [SerializeField] private TextMeshProUGUI currentGoldText;  // "Current : 20000"
    [Header("Payment")]
    [SerializeField] private TMP_InputField customPayInput;    // "Pay : 15000"
    [SerializeField] private Button payButton;
    [SerializeField] private Button punishmentButton;

    [Header("Dialogue")]
    [SerializeField] private TextMeshProUGUI dialogueText;
    private void Start()
    {
        punishmentButton.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        if (DebtManager.Instance == null) return;

        DebtManager.Instance.OnDebtChanged += PopulateUI;
        PopulateUI();
        payButton.onClick.AddListener(OnPayClicked);
    }

    private void OnDisable()
    {
        if (DebtManager.Instance == null) return;

        payButton.onClick.RemoveListener(OnPayClicked);
        DebtManager.Instance.OnDebtChanged -= PopulateUI;
    }

    private void PopulateUI()
    {
        int remaining = DebtManager.Instance.RemainingDueThisMonth;
        int debt = DebtManager.Instance.CurrentDebt;
        long playerGold = CurrencyManager.Instance.CurrentGold;

        monthText.text = $"Month {WorldTimeManager.Instance.CurrentMonth}";
        minimumPayText.text = $"To pay : {remaining}";
        currentDebtText.text = $"Debt : {debt}";
        currentGoldText.text = $"Current : {playerGold}";

        // Default input to minimum due
        customPayInput.text = playerGold < remaining ? playerGold.ToString() : remaining.ToString();

        if (playerGold <= 0)
        {
            punishmentButton.gameObject.SetActive(true);
            payButton.gameObject.SetActive(false);
            dialogueText.text = "You have nothing. Hand over your goods.";
        }
        else
        {
            punishmentButton.gameObject.SetActive(false);
            payButton.gameObject.SetActive(true);
            dialogueText.text = "Hmm... you managed to scrape it together.";
        }

        // Set dialogue based on player's situation
        dialogueText.text = playerGold >= remaining
            ? "Hmm... you managed to scrape it together. Don't make me wait next time."
            : "You don't have enough. This will cost you.";
    }

    private void OnPayClicked()
    {
        long playerGold = CurrencyManager.Instance.CurrentGold;
        int remaining = DebtManager.Instance.RemainingDueThisMonth;

        if (!int.TryParse(customPayInput.text, out int payAmount)) return;

        // Can't pay less than minimum
        if (payAmount < remaining)
        {
            dialogueText.text = "That's not enough. But will expand your time for a while.";
            DebtManager.Instance.MakePaymentServerRpc(payAmount);
            return;
        }

        // Can't pay more than player has
        if (payAmount > playerGold)
        {
            dialogueText.text = "You don't have that much gold.";
            return;
        }

        if (playerGold <= 0)
        {
            return;
        }

        DebtManager.Instance.MakePaymentServerRpc(payAmount);
        // InGameUIManager.Instance.OpenExclusivePanel("DebtPay"); // close panel
    }
}
