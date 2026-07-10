using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

[RequireComponent(typeof(CanvasGroup))]
public class InventoryItems : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI amountText;
    public ItemSO item;
    public int amount;

    [HideInInspector]

    public InventorySlotUIs sourceSlot;

    public RectTransform rectTransform;
    public CanvasGroup canvasGroup;
    public Transform originalParent;
    public int originalAmount;
    public Vector2 originalPosition;
    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        // amountText.text = amount.ToString();
    }
    public bool wasDroppedSuccessfully;

    public void InitializeItem(ItemSO newItem, int newAmount)
    {
        item = newItem;
        amount = newAmount;
        RefreshUI();
    }

    public void RefreshUI()
    {
        if (item == null || amount <= 0)
        {
            iconImage.enabled = false;
            amountText.text = "";
        }
        else
        {
            iconImage.sprite = item.icon;
            iconImage.preserveAspect = true; // ป้องกัน icon ยืด/หดตาม native size ของแต่ละ sprite
            iconImage.enabled = true;
            amountText.text = amount.ToString();
        }
    }

    public void SetOriginalAmount(int value) => originalAmount = value;
    public int GetOriginalAmount() => originalAmount;
    public bool IsPartialStack() => amount < originalAmount;

    // ── Hover tooltip ──────────────────────────────────────────────────────
    [Header("Tooltip")]
    [Tooltip("ต้องเอาเมาส์ชี้ค้างไว้กี่วินาทีก่อน Tooltip จะขึ้น (กันโชว์วูบวาบตอนกวาดเมาส์ผ่านหลาย ๆ ช่อง)")]
    [SerializeField] private float tooltipHoverDelay = 0.4f;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (item == null || amount <= 0) return;
        CancelInvoke(nameof(ShowTooltipNow));
        Invoke(nameof(ShowTooltipNow), tooltipHoverDelay);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        CancelInvoke(nameof(ShowTooltipNow));
        ItemTooltipUI.Instance?.Hide();
    }

    private void ShowTooltipNow()
    {
        if (item == null) return;
        // ส่ง rectTransform ของช่องไอเทมนี้ไปตรง ๆ ให้ ItemTooltipUI ไปคำนวณตำแหน่งเอง
        // ด้วย world space (Transform.position) — แม่นกว่าแปลง screen point ไป-กลับเยอะ
        ItemTooltipUI.Instance?.Show(item, rectTransform);
    }

    private void OnDisable()
    {
        CancelInvoke(nameof(ShowTooltipNow));
        ItemTooltipUI.Instance?.Hide();
    }
}