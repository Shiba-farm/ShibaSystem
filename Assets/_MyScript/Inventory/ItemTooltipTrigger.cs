using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// [NEW] ใส่บน InventorySlot / HotbarSlot
/// เมื่อเมาส์ชี้ → แสดง tooltip ไอเท็มในช่อง
/// </summary>
public class ItemTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Tooltip("อ้างถึง InventorySlot ที่ component นี้ติดอยู่")]
    public InventorySlot slot;

    void Awake()
    {
        if (slot == null) slot = GetComponent<InventorySlot>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (slot == null || slot.IsEmpty) return;
        if (ItemTooltip.Instance != null)
            ItemTooltip.Instance.Show(slot.item, slot.amount);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (ItemTooltip.Instance != null)
            ItemTooltip.Instance.Hide();
    }
}
