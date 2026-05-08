using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(InventoryItems))]
public class DraggableItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private InventoryItems itemUI;
    private Transform originalParent;
    private Vector2 originalPosition;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Awake()
    {
        itemUI = GetComponent<InventoryItems>();
    }
    public void OnBeginDrag(PointerEventData eventData)
    {
        int amount = itemUI.amount;
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
