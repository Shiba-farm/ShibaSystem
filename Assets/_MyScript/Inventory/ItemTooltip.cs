using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// [NEW] Tooltip — เมื่อชี้ไอเท็มใน Inventory / Hotbar → แสดงกล่องข้อมูล
/// ข้อมูลที่แสดง: ชื่อ, ราคาขาย (ตามตลาด), แนวโน้มราคา, ประเภท, คำอธิบาย
///
/// วิธี Setup:
/// 1. สร้าง Panel (TooltipPanel) ใน Canvas
/// 2. ใส่ TextMeshPro ตามช่อง
/// 3. ใส่ ItemTooltip component บน Canvas
/// 4. ในแต่ละ InventorySlot/HotbarSlot → ใส่ EventTrigger หรือ ItemTooltipTrigger
/// </summary>
public class ItemTooltip : MonoBehaviour
{
    public static ItemTooltip Instance { get; private set; }

    [Header("Tooltip Panel")]
    public GameObject tooltipPanel;
    public RectTransform tooltipRect;

    [Header("Content")]
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI categoryText;
    public TextMeshProUGUI sellPriceText;
    public TextMeshProUGUI marketTrendText;
    public TextMeshProUGUI descriptionText;
    public Image itemIcon;

    [Header("Config")]
    [Tooltip("ระยะห่างจากเมาส์ (pixels)")]
    public Vector2 offset = new Vector2(20, -20);

    Canvas _parentCanvas;
    RectTransform _canvasRect;

    void Awake()
    {
        Instance = this;
        if (tooltipPanel) tooltipPanel.SetActive(false);

        _parentCanvas = GetComponentInParent<Canvas>();
        if (_parentCanvas) _canvasRect = _parentCanvas.GetComponent<RectTransform>();
    }

    void Update()
    {
        if (tooltipPanel != null && tooltipPanel.activeSelf && tooltipRect != null)
        {
            FollowMouse();
        }
    }

    // ================================================================
    // Show / Hide
    // ================================================================

    /// <summary>แสดง tooltip สำหรับไอเท็ม</summary>
    public void Show(ItemSO item, int amount = 1)
    {
        if (item == null || tooltipPanel == null) return;

        tooltipPanel.SetActive(true);

        // ชื่อ
        if (itemNameText) itemNameText.text = item.itemName;

        // ไอคอน
        if (itemIcon)
        {
            itemIcon.sprite = item.icon;
            itemIcon.enabled = item.icon != null;
        }

        // ประเภท
        if (categoryText)
        {
            string catStr = GetCategoryThai(item.category);
            if (item.toolAction != ToolAction.None)
                catStr += $" ({item.toolAction})";
            categoryText.text = catStr;
        }

        // ราคาขาย — ใช้ตลาดถ้ามี
        if (sellPriceText)
        {
            if (!item.sellable)
            {
                sellPriceText.text = "ขายไม่ได้";
                sellPriceText.color = Color.gray;
            }
            else
            {
                int sellPrice;
                if (MarketPriceSystem.Instance != null)
                    sellPrice = MarketPriceSystem.Instance.GetSellPrice(item);
                else
                    sellPrice = item.sellPrice;

                sellPriceText.text = $"ราคาขาย: ¥{sellPrice:N0}";

                // เทียบกับราคาฐาน
                if (sellPrice > item.sellPrice)
                    sellPriceText.color = new Color(0.2f, 0.8f, 0.2f); // สูงกว่าปกติ = เขียว
                else if (sellPrice < item.sellPrice)
                    sellPriceText.color = new Color(1f, 0.4f, 0.4f); // ต่ำกว่าปกติ = แดง
                else
                    sellPriceText.color = Color.white;

                // แสดงราคา stack ถ้ามากกว่า 1
                if (amount > 1)
                    sellPriceText.text += $" (รวม ¥{sellPrice * amount:N0})";
            }
        }

        // แนวโน้มตลาด
        if (marketTrendText)
        {
            if (MarketPriceSystem.Instance != null && item.sellable)
            {
                var trend = MarketPriceSystem.Instance.GetTrend(item.itemName);
                var mult = MarketPriceSystem.Instance.GetPriceMultiplier(item.itemName);

                switch (trend)
                {
                    case PriceTrend.Up:
                        marketTrendText.text = $"▲ ตลาดต้องการสูง (x{mult:F1})";
                        marketTrendText.color = new Color(0.2f, 0.8f, 0.2f);
                        break;
                    case PriceTrend.Down:
                        marketTrendText.text = $"▼ ล้นตลาด (x{mult:F1})";
                        marketTrendText.color = Color.red;
                        break;
                    default:
                        marketTrendText.text = "— ราคาปกติ";
                        marketTrendText.color = Color.gray;
                        break;
                }
            }
            else
            {
                marketTrendText.text = "";
            }
        }

        // คำอธิบาย — ใช้ข้อมูลจาก category
        if (descriptionText)
        {
            string desc = "";
            if (item.category == ItemCategory.Seed && item.seedCrop != null)
                desc = $"เมล็ดพันธุ์: {item.seedCrop.cropName}";
            else if (item.category == ItemCategory.FarmHelper && item.farmHelperData != null)
                desc = item.farmHelperData.description;
            else if (item.energyCost > 0)
                desc = $"ใช้พลังงาน: {item.energyCost}";
            descriptionText.text = desc;
        }

        FollowMouse();
    }

    /// <summary>ซ่อน tooltip</summary>
    public void Hide()
    {
        if (tooltipPanel) tooltipPanel.SetActive(false);
    }

    // ================================================================
    // Mouse Follow + Screen Clamp
    // ================================================================

    void FollowMouse()
    {
        if (tooltipRect == null || _canvasRect == null) return;

        Vector2 pos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRect, Input.mousePosition, _parentCanvas.worldCamera, out pos);

        pos += offset;

        // Clamp ไม่ให้หลุดจอ
        Vector2 size = tooltipRect.sizeDelta;
        Vector2 canvasSize = _canvasRect.sizeDelta;

        if (pos.x + size.x > canvasSize.x / 2) pos.x = pos.x - size.x - offset.x * 2;
        if (pos.y - size.y < -canvasSize.y / 2) pos.y = pos.y + size.y - offset.y * 2;

        tooltipRect.anchoredPosition = pos;
    }

    // ================================================================
    // Category → Thai
    // ================================================================

    string GetCategoryThai(ItemCategory cat)
    {
        switch (cat)
        {
            case ItemCategory.Tools: return "เครื่องมือ";
            case ItemCategory.Seed: return "เมล็ดพันธุ์";
            case ItemCategory.Food: return "อาหาร";
            case ItemCategory.Resources: return "วัตถุดิบ";
            case ItemCategory.FarmHelper: return "ตัวช่วยฟาร์ม";
            case ItemCategory.Wearables: return "ของแต่งกาย";
            case ItemCategory.Structures: return "โครงสร้าง";
            default: return "อื่น ๆ";
        }
    }
}
