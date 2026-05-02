using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// [UPGRADED] แสดงไอเท็มในร้าน พร้อมสต๊อก + sold out state
/// </summary>
public class ShopItemView : MonoBehaviour
{
    [Header("UI Refs")]
    public Image icon;
    public TextMeshProUGUI nameLabel;
    public TextMeshProUGUI priceLabel;
    public TMP_InputField amountInput;
    public Button minusBtn;
    public Button plusBtn;
    public Button buyBtn;

    [Header("Stock Display (optional)")]
    public TextMeshProUGUI stockLabel;
    public GameObject soldOutOverlay;

    [Header("Market Trend (optional)")]
    public TextMeshProUGUI trendLabel;

    ItemSO _item;
    int _priceEach;
    int _maxPerClick = 99;
    int _currentStock = -1;
    Action<ItemSO, int, int> _onBuy;
    const int MIN_AMOUNT = 1;

    void Awake()
    {
        if (amountInput) amountInput.onEndEdit.AddListener(OnAmountEdited);
        if (minusBtn) minusBtn.onClick.AddListener(() => ChangeAmount(-1));
        if (plusBtn) plusBtn.onClick.AddListener(() => ChangeAmount(+1));
        if (buyBtn) buyBtn.onClick.AddListener(BuyNow);
    }

    /// <summary>
    /// [UPGRADED] Setup พร้อมข้อมูลสต๊อก
    /// </summary>
    public void Setup(ItemSO item, int priceEach, int maxPerClick, Action<ItemSO, int, int> onBuy,
                      int currentStock = -1, bool soldOut = false)
    {
        _item = item;
        _priceEach = Mathf.Max(0, priceEach);
        _maxPerClick = Mathf.Max(1, maxPerClick);
        _currentStock = currentStock;
        _onBuy = onBuy;

        if (icon) icon.sprite = item ? item.icon : null;
        if (nameLabel) nameLabel.text = item ? item.itemName : "?";
        if (priceLabel) priceLabel.text = $"¥{_priceEach:N0}";
        if (amountInput) amountInput.text = MIN_AMOUNT.ToString();

        // Stock display
        if (stockLabel)
        {
            if (currentStock < 0)
                stockLabel.text = ""; // ไม่จำกัด — ไม่แสดง
            else if (soldOut)
            {
                stockLabel.text = "หมด";
                stockLabel.color = Color.red;
            }
            else
            {
                stockLabel.text = $"เหลือ {currentStock}";
                stockLabel.color = currentStock <= 3 ? new Color(1f, 0.6f, 0f) : Color.white;
            }
        }

        // Sold out overlay
        if (soldOutOverlay) soldOutOverlay.SetActive(soldOut);

        // Disable buy button if sold out
        if (buyBtn) buyBtn.interactable = !soldOut;
        if (amountInput) amountInput.interactable = !soldOut;
        if (minusBtn) minusBtn.interactable = !soldOut;
        if (plusBtn) plusBtn.interactable = !soldOut;

        // Market trend (optional)
        if (trendLabel && item != null && MarketPriceSystem.Instance != null)
        {
            var trend = MarketPriceSystem.Instance.GetTrend(item.itemName);
            var mult = MarketPriceSystem.Instance.GetPriceMultiplier(item.itemName);
            switch (trend)
            {
                case PriceTrend.Up:
                    trendLabel.text = $"▲ x{mult:F1}";
                    trendLabel.color = new Color(0.2f, 0.8f, 0.2f);
                    break;
                case PriceTrend.Down:
                    trendLabel.text = $"▼ x{mult:F1}";
                    trendLabel.color = Color.red;
                    break;
                default:
                    trendLabel.text = "— x1.0";
                    trendLabel.color = Color.gray;
                    break;
            }
        }

        // Dim icon if sold out
        if (icon && soldOut) icon.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
    }

    // compat: เดิมเรียกแบบไม่มี stock args
    public void Setup(ItemSO item, int priceEach, int maxPerClick, Action<ItemSO, int, int> onBuy)
    {
        Setup(item, priceEach, maxPerClick, onBuy, -1, false);
    }

    void OnAmountEdited(string s)
    {
        if (!int.TryParse(s, out int v)) v = MIN_AMOUNT;
        int max = (_currentStock > 0) ? Mathf.Min(_maxPerClick, _currentStock) : _maxPerClick;
        v = Mathf.Clamp(v, MIN_AMOUNT, max);
        if (amountInput) amountInput.text = v.ToString();
    }

    void ChangeAmount(int delta)
    {
        int v = MIN_AMOUNT;
        if (amountInput && int.TryParse(amountInput.text, out int cur)) v = cur;
        int max = (_currentStock > 0) ? Mathf.Min(_maxPerClick, _currentStock) : _maxPerClick;
        v = Mathf.Clamp(v + delta, MIN_AMOUNT, max);
        if (amountInput) amountInput.text = v.ToString();
    }

    void BuyNow()
    {
        int amt = MIN_AMOUNT;
        if (amountInput && int.TryParse(amountInput.text, out int cur)) amt = cur;
        int max = (_currentStock > 0) ? Mathf.Min(_maxPerClick, _currentStock) : _maxPerClick;
        amt = Mathf.Clamp(amt, MIN_AMOUNT, max);
        _onBuy?.Invoke(_item, _priceEach, amt);
    }
}
