using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class DebtPayUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DebtDetailPanelUI detailPanelUI;
    [SerializeField] private PunishmentPanelUI punishmentPanelUI;
    [SerializeField] private DebtSummaryPanelUI summaryPanelUI;

    private PunishmentResult _currentResult;
    public bool isBeginPayment = true;

    private void Start()
    {
        _currentResult = new PunishmentResult();

        punishmentPanelUI.Close();
        detailPanelUI.Open();
        summaryPanelUI.Close();
        
    }

    public void OnPunishClicked()
    {
        punishmentPanelUI.Open();
        detailPanelUI.Close();
        summaryPanelUI.Close();
        isBeginPayment = false;

        Debug.Log($"Is begin : {isBeginPayment}");
    }

    public void HideAll()
    {
        punishmentPanelUI.Close();
        detailPanelUI.Close();
        summaryPanelUI.Close();
        InGameUIManager.Instance.TogglePanel(InGamePanel.Debt);
        // gameObject.SetActive(false);
    }

    public void OnTradeConfirmed(List<(ItemSO, int)> tradedItems)
    {
        if (tradedItems != null)
            foreach (var item in tradedItems)
                _currentResult.LostItems.Add(item);

        int remaining = DebtManager.Instance.RemainingDueThisMonth;
        Debug.Log($"Remaining < 0 : {remaining <=0}");
        Debug.Log($"[OnTrade] => Current lost item exists? : {_currentResult.LostItems.Count}");

        if (remaining <= 0)
        {
            // Scenario 1: fully paid via trade
            GoToSummary();
        }
        // else: still has items to trade, stay on punishment panel
        // PunishmentPanelUI handles empty inventory → calls OnInventoryEmpty()
    }

    public void OnInventoryEmpty(List<(ItemSO, int)> tradedItems)
    {
        if (isBeginPayment) return;
        if (tradedItems != null)
            foreach (var item in tradedItems)
                _currentResult.LostItems.Add(item);

        Debug.Log("Empty");

        // Scenario 2: traded everything, still in debt
        // ApplyDebtPunishment();
        Debug.Log($"[OnEmpt] => Current lost item exists? : {_currentResult.LostItems.Count}");
        DebtManager.Instance.ApplyPunishment(_currentResult);
        GoToSummary();
    }

    public void OnRefuseClicked(List<(ItemSO, int)> tradedSoFar)
    {
        if (tradedSoFar != null)
            foreach (var item in tradedSoFar)
                _currentResult.LostItems.Add(item);

        _currentResult.WasRefused = true;

        int remaining = DebtManager.Instance.RemainingDueThisMonth;
        if (remaining > 0)
        {
            // Scenario 3 & 4: still owes debt → add punishment debt

            // ApplyDebtPunishment();
            DebtManager.Instance.ApplyPunishment(_currentResult);
        }

        GoToSummary();
    }

    public void OnPaymentSubmitted(int payAmount)
    {
        _currentResult = new PunishmentResult();
        int remaining = DebtManager.Instance.RemainingDueThisMonth;

        DebtManager.Instance.MakePaymentServerRpc(payAmount);

        if (payAmount >= remaining)
        {
            // Fully paid — skip punishment, go straight to summary
            GoToSummary();
        }
    }

    private void GoToSummary()
    {
        _currentResult.GoldPaid = DebtManager.Instance.PaidThisMonth;
        _currentResult.TradePaid = DebtManager.Instance.TradePaidThisMonth;
        _currentResult.FinalDebt = DebtManager.Instance.CurrentDebt;

        DebtManager.Instance.SetMonthSettledServerRpc();

        punishmentPanelUI.Close();
        detailPanelUI.Close();
        summaryPanelUI.Open(_currentResult);
        isBeginPayment = false;
    }
}
