using Sirenix.Utilities;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class DebtSummaryPanelUI : MonoBehaviour
{
    [Header("Summary section")]
    [SerializeField] private TextMeshProUGUI payThisMonthText;
    [SerializeField] private TextMeshProUGUI leftToPayText;
    [SerializeField] private Button okayButton;

    [Header("Punishment Section")]
    [SerializeField] private GameObject punishmentSection;
    [SerializeField] private TextMeshProUGUI lostTextHeader;
    [SerializeField] private TextMeshProUGUI addedDebtText;
    [SerializeField] private Transform lostItemContainer;
    [SerializeField] private GameObject lostItemPrefab;  // simple icon + amount row
    [Header("Reference")]
    [SerializeField] private DebtPayUI _controller;

    public void OnEnable()
    {
        okayButton.onClick.AddListener(OnAccept);
        lostTextHeader.gameObject.SetActive(false);
    }

    public void Open(PunishmentResult punishmentResult = null)
    {
        gameObject.SetActive(true);
        PopulateUI(punishmentResult);
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    public void PopulateUI(PunishmentResult punishmentResult = null)
    {
        payThisMonthText.text = $"Pay {DebtManager.Instance.TotalPaidThisMonth} this month";

        // ── null check ก่อนอ่านค่า (fix NullReferenceException) ──────────
        if (punishmentResult == null)
        {
            leftToPayText.text = $"Left to pay : {DebtManager.Instance.CurrentDebt}";
            punishmentSection.SetActive(false);
            return;
        }

        leftToPayText.text = $"Left to pay : {punishmentResult.FinalDebt}";

        bool hasPunishment = punishmentResult.HasLostItems || punishmentResult.HasAddedDebt;
        punishmentSection.SetActive(hasPunishment);
        if (!hasPunishment) return;

        // Added debt line
        addedDebtText.gameObject.SetActive(punishmentResult.HasAddedDebt);
        if (punishmentResult.HasAddedDebt)
            addedDebtText.text = $"Added {DebtManager.Instance.penalty} to debt";

        // Lost items
        foreach (Transform child in lostItemContainer)
            Destroy(child.gameObject);

        foreach (var (item, amount) in punishmentResult.LostItems)
        {
            var row = Instantiate(lostItemPrefab, lostItemContainer);
            row.GetComponent<LostItemRowUI>().Setup(item.icon, amount);
        }
        if (!punishmentResult.LostItems.IsNullOrEmpty()) lostTextHeader.gameObject.SetActive(true);
    }

    private void OnAccept()
    {
        _controller.HideAll();
    }
}
