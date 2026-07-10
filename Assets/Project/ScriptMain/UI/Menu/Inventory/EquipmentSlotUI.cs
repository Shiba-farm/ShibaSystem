using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// ช่องอุปกรณ์เดี่ยว (Helmet / Ring / Shield / Boots ฯลฯ) — รับ drag จาก inventory
/// slot เพื่อสวม, คลิกเพื่อถอด ตัว slot เองไม่รู้จัก EquipmentData ตรง ๆ
/// คุยผ่าน EquipmentNetworkManager เท่านั้น (server-authoritative เหมือนระบบ inventory)
/// </summary>
public class EquipmentSlotUI : MonoBehaviour, IDropHandler, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI labelText;
    [SerializeField] private int mainInventoryID = 0; // inventoryID ของกระเป๋าหลักที่จะรับไอเทมตอนถอด
    [Tooltip("ต้องเอาเมาส์ชี้ค้างไว้กี่วินาทีก่อน Tooltip จะขึ้น — ค่าเดียวกับ InventoryItems")]
    [SerializeField] private float tooltipHoverDelay = 0.4f;

    public EquipSlot Slot { get; private set; }
    private Sprite _emptyIcon;
    private ItemSO _currentItem;
    private RectTransform _rectTransform;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
    }

    public void Setup(EquipSlot slot, string displayName, Sprite emptyIcon)
    {
        Slot = slot;
        _emptyIcon = emptyIcon;
        if (labelText != null) labelText.text = displayName;
        Refresh(null);
    }

    public void Refresh(ItemSO equippedItem)
    {
        // เก็บไว้ใช้ตอน hover ด้วย — เดิม Refresh() รับค่ามาแค่ตั้ง icon แล้วทิ้ง ไม่มีที่ไหน
        // จำ "ไอเทมที่สวมอยู่ตอนนี้" เป็น field เลย ตอนเมาส์ชี้เลยไม่รู้ว่าจะเอาข้อมูลอะไรไปโชว์ tooltip
        _currentItem = equippedItem;
        if (iconImage == null) return;

        if (equippedItem != null)
        {
            iconImage.sprite = equippedItem.icon;
            iconImage.color = Color.white;
        }
        else
        {
            iconImage.sprite = _emptyIcon;
            iconImage.color = _emptyIcon != null ? Color.white : new Color(1f, 1f, 1f, 0.25f);
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        GameObject dropped = eventData.pointerDrag;
        if (dropped == null) return;

        InventoryItems draggedItem = dropped.GetComponent<InventoryItems>();
        if (draggedItem == null || draggedItem.item == null) return;
        if (draggedItem.item is not WearableItemSO wearable || wearable.Slot != Slot) return;

        draggedItem.wasDroppedSuccessfully = true;
        // ใช้ inventoryIndex (ตำแหน่งใน NetworkList จริง) แทน slotIndex (visual index)
        EquipmentNetworkManager.Instance.RequestEquipServerRpc(
            draggedItem.sourceSlot.inventoryID, draggedItem.sourceSlot.inventoryIndex, Slot);

        Destroy(draggedItem.gameObject);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        EquipmentNetworkManager.Instance.RequestUnequipServerRpc(Slot, mainInventoryID);
    }

    // ── Hover tooltip (เหมือน InventoryItems.cs ทุกจุด) ────────────────────
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_currentItem == null) return;
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
        if (_currentItem == null) return;
        ItemTooltipUI.Instance?.Show(_currentItem, _rectTransform);
    }

    private void OnDisable()
    {
        CancelInvoke(nameof(ShowTooltipNow));
        ItemTooltipUI.Instance?.Hide();
    }
}
