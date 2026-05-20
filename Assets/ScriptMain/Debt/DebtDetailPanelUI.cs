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
    [SerializeField] private Button endButton;

    [Header("Dialogue")]
    [SerializeField] private TextMeshProUGUI dialogueText;
    [Header("Reference")]
    [SerializeField] private DebtPayUI _controller;

    private void Start()
    {
        // punishmentButton.gameObject.SetActive(false);
        // endButton.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        if (DebtManager.Instance == null) return;
        DebtManager.Instance.OnDebtChanged += PopulateUI;

        payButton.onClick.AddListener(OnPayClicked);
        punishmentButton.onClick.AddListener(OnPunishmentClicked);
        endButton.onClick.AddListener(OnEndClicked);
        InventoryNetworkManager.Instance.OnInventoryConfirmedEmpty += OnInventoryCheckResult;
        // PopulateUI();
    }

    private void OnDisable()
    {
        if (DebtManager.Instance == null) return;
        DebtManager.Instance.OnDebtChanged -= PopulateUI;

        payButton.onClick.RemoveListener(OnPayClicked);
        punishmentButton.onClick.RemoveListener(OnPunishmentClicked);
        endButton.onClick.RemoveListener(OnEndClicked);
        InventoryNetworkManager.Instance.OnInventoryConfirmedEmpty -= OnInventoryCheckResult;
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

        bool hasGold = playerGold > 0;

        if (hasGold)
        {
            payButton.gameObject.SetActive(true);
            punishmentButton.gameObject.SetActive(false);
            endButton.gameObject.SetActive(false);
            dialogueText.text = playerGold >= remaining
                ? "Hmm... you managed to scrape it together. Don't make me wait next time."
                : "You don't have enough. This will cost you.";
        }
        else
        {
            payButton.gameObject.SetActive(false);
            punishmentButton.gameObject.SetActive(false);
            endButton.gameObject.SetActive(false);
            dialogueText.text = "...";
            // Determine case 2 vs 3 from server
            InventoryNetworkManager.Instance.CheckInventoryEmptyServerRpc(0);
        }
    }

    public void Open()
    {
        gameObject.SetActive(true);
        PopulateUI(); 
    }
    public void Close()
    {
        gameObject.SetActive(false);
    }

    private void OnPayClicked()
    {
        if (!int.TryParse(customPayInput.text, out int payAmount)) return;

        long playerGold = CurrencyManager.Instance.CurrentGold;

        if (payAmount > playerGold)
        {
            dialogueText.text = "You don't have that much gold.";
            return;
        }

        // Report up — controller decides what happens next
        _controller.OnPaymentSubmitted(payAmount);
    }

    private void OnInventoryCheckResult(bool isEmpty)
    {
        payButton.gameObject.SetActive(false);
        if (isEmpty)
        {
            punishmentButton.gameObject.SetActive(false);
            endButton.gameObject.SetActive(true);
            dialogueText.text = "You have nothing at all. This debt will grow.";
        }
        else
        {
            punishmentButton.gameObject.SetActive(true);
            endButton.gameObject.SetActive(false);
            dialogueText.text = "You have no gold. Hand over your goods.";
        }
    }

    private void OnPunishmentClicked()
    {
        _controller.OnPunishClicked();
    }

    private void OnEndClicked()
    {
        _controller.OnRefuseClicked(null); // nothing to trade, apply penalty and end
    }
}
