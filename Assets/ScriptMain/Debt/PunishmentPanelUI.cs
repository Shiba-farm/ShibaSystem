using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PunishmentPanelUI : MonoBehaviour
{
    [Header("Info")]
    [SerializeField] private TextMeshProUGUI leftToPayText;
    [SerializeField] private TextMeshProUGUI totalValueText;   // "To pay : 10500"
    [SerializeField] private HoldButton refuseButton;
    [SerializeField] private HoldButton tradeButton;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [Header("References")]
    [SerializeField] private PunishmentInventoryMainUIs punishmentInventory;

    [SerializeField] private DebtPayUI _controller;
    private bool _waitingForEmptyCheckAfterTrade = false;
    private List<(ItemSO, int)> _pendingTradedItems;

    private void OnEnable()
    {
        if (DebtManager.Instance == null) return;

        DebtManager.Instance.OnTradeValueChanged += PopulateUI;
        refuseButton.OnConfirmed += OnRefuseClick;
        tradeButton.OnConfirmed += OnTradeClick;
        InventoryNetworkManager.Instance.OnInventoryConfirmedEmpty += OnEmptyCheckAfterTrade;
        // PopulateUI();
    }

    public void Open()
    {
        punishmentInventory.Init();
        gameObject.SetActive(true);
        PopulateUI();
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }
    private void OnDisable()
    {
        if (DebtManager.Instance == null) return;

        DebtManager.Instance.OnTradeValueChanged -= PopulateUI;
        refuseButton.OnConfirmed -= OnRefuseClick;
        tradeButton.OnConfirmed -= OnTradeClick;
        InventoryNetworkManager.Instance.OnInventoryConfirmedEmpty -= OnEmptyCheckAfterTrade;
    }
    private void PopulateUI()
    {
        int remaining = DebtManager.Instance.RemainingDueThisMonth;
        int tradeValue = DebtManager.Instance.CurrentTradeValue;
        leftToPayText.text = $"Left to Pay : {remaining}";
        totalValueText.text = $"Total value : {tradeValue}";
    }

    private void OnTradeClick()
    {
        DebtManager.Instance.MakeTradePaymentServerRpc();
        var tradedItems = punishmentInventory.GetAndClearSelectedItems();
        Debug.Log($"Is Empty from trade : {tradedItems.Count}");
        _pendingTradedItems = tradedItems;
        _waitingForEmptyCheckAfterTrade = true;
        InventoryNetworkManager.Instance.CheckInventoryEmptyServerRpc(0);
    }

    private void OnRefuseClick()
    {
        var tradedSoFar = punishmentInventory.GetAndClearSelectedItems();
        _controller.OnRefuseClicked(tradedSoFar);
    }

    private void OnEmptyCheckAfterTrade(bool isEmpty)
    {
        if (!_waitingForEmptyCheckAfterTrade) return;
        _waitingForEmptyCheckAfterTrade = false;

        Debug.Log($"[RPC Empt] => Is empty: {isEmpty}");

        if (isEmpty)
            _controller.OnInventoryEmpty(_pendingTradedItems);  // Scenario 2
        else
            _controller.OnTradeConfirmed(_pendingTradedItems);  // Scenario 1 check

        _pendingTradedItems = null;
    }
}
