using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventoryItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{

    [Header("UI")]
    [SerializeField] public Image image;
    [HideInInspector] public Transform parentAfterdrag;
    [HideInInspector] public ItemData item;

    public void InitialiseItem(ItemData newItem)
    {
        item = newItem;
        image.sprite = newItem.image;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        image.raycastTarget = false;
        parentAfterdrag = transform.parent;
        transform.SetParent(transform.root);
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = InputHandler.Singleton.MousePosition;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        image.raycastTarget = true;
        transform.SetParent(parentAfterdrag);
    }
}
