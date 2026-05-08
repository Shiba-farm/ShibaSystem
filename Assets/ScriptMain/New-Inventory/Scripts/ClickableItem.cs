using System;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(InventoryItems))]
public class ClickableItem : MonoBehaviour, IPointerClickHandler
{
    private InventoryItems itemUI;

    private void Awake()
    {
        itemUI = GetComponent<InventoryItems>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        itemUI.sourceSlot.OnItemClicked(itemUI, eventData);
    }
}
