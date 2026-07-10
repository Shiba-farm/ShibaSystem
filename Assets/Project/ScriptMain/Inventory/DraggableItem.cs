using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(InventoryItems))]
public class DraggableItem : MonoBehaviour,
    IPointerDownHandler, IPointerUpHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private InventoryItems itemUI;
    private Transform originalParent;
    private Vector2 originalPosition;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Awake()
    {
        itemUI = GetComponent<InventoryItems>();
    }
    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log($"[DraggableItem] OnPointerDown: item={itemUI?.item?.itemName}, btn={eventData.button}, pressObj={eventData.pointerPressRaycast.gameObject?.name}");
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Debug.Log($"[DraggableItem] OnPointerUp: item={itemUI?.item?.itemName}, dragging={eventData.dragging}, clickCount={eventData.clickCount}");
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Null guard — ป้องกัน NullRef ถ้า Awake ยังไม่วิ่งหรือ prefab ขาด component
        if (itemUI == null)
        {
            Debug.LogError($"[DraggableItem] OnBeginDrag: itemUI is NULL on {gameObject.name} — drag aborted");
            return;
        }
        if (itemUI.sourceSlot == null)
        {
            Debug.LogError($"[DraggableItem] OnBeginDrag: sourceSlot is NULL for item={itemUI.item?.itemName} on {gameObject.name} — drag aborted");
            return;
        }

        int amount = itemUI.amount;
        Debug.Log($"[DraggableItem] OnBeginDrag: item={itemUI.item?.itemName}, amount={amount}, slotIdx={itemUI.sourceSlot.slotIndex}, go={gameObject.name}");
        int amountToMove = (eventData.button == PointerEventData.InputButton.Right)
            ? Mathf.Max(1, amount / 2) : amount;

        itemUI.SetOriginalAmount(amount);
        itemUI.amount = amountToMove;
        itemUI.wasDroppedSuccessfully = false;
        itemUI.RefreshUI();

        itemUI.sourceSlot.OnItemDraggedAway(amountToMove);
        originalParent = transform.parent;
        originalPosition = itemUI.rectTransform.anchoredPosition;
        transform.SetParent(transform.root);

        itemUI.canvasGroup.blocksRaycasts = false;
        itemUI.canvasGroup.alpha = 0.7f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        itemUI.rectTransform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        itemUI.canvasGroup.blocksRaycasts = true;
        itemUI.canvasGroup.alpha = 1f;

        if (!itemUI.wasDroppedSuccessfully)
            ReturnToSource();
    }

    private void ReturnToSource()
    {
        itemUI.amount = itemUI.GetOriginalAmount();
        itemUI.sourceSlot.amount = itemUI.GetOriginalAmount();
        itemUI.sourceSlot.ClearSlotVisuals();
        transform.SetParent(originalParent);
        itemUI.rectTransform.anchoredPosition = Vector2.zero;
        itemUI.sourceSlot.LinkItemUI(itemUI);
        itemUI.RefreshUI();
    }
}
