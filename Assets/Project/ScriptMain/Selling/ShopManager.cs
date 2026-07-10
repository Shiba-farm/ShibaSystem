using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class ShopManager : MonoBehaviour
{
    [SerializeField] private PlayerShopInventoryMainUIs playerPanel;
    [SerializeField] private SellboxInventoryMainUIs sellboxPanel;
    [SerializeField] private CartDataSignal sharedCartSignal;
    [SerializeField] private Button sellAllButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button clearButton;
    [SerializeField] private TextMeshProUGUI totalValueText;
    private SharedCartData sharedCart;

    private void OnEnable()
    {
        if (sharedCartSignal.Current != null)
            OnCartReady(sharedCartSignal.Current);
        else
            sharedCartSignal.OnCartReady += OnCartReady;

        sellAllButton.onClick.AddListener(OnSellAll);
        closeButton.onClick.AddListener(OnCloseButtonClicked);
        clearButton.onClick.AddListener(OnClearAllButtonClicked);
    }

    private void OnDisable()
    {
        sharedCartSignal.OnCartReady -= OnCartReady;
        if (sharedCart != null)
        {
            sharedCart.cartInventory.InventoryItems.OnListChanged -= RefreshTotalValue;
        }
        sellAllButton.onClick.RemoveListener(OnSellAll);
        closeButton.onClick.RemoveListener(OnCloseButtonClicked);
        clearButton.onClick.RemoveListener(OnClearAllButtonClicked);
    }

    private void OnCartReady(SharedCartData cart)
    {
        sharedCart = cart;
        // Safe to access now — cart is fully spawned
        sharedCart.cartInventory.InventoryItems.OnListChanged += RefreshTotalValue;
    }

    private void RefreshTotalValue(NetworkListEvent<NetworkItems> _)
    {
        totalValueText.text = $"Total: {sharedCart.GetTotalValue()}G";
    }

    private void OnSellAll() => sharedCart.SellAllServerRpc();

    public void OnCloseButtonClicked() => InGameUIManager.Instance.TogglePanel(InGamePanel.Selling);
    public void OnClearAllButtonClicked() => sharedCart.RemoveAllItemServerRpc();
}
