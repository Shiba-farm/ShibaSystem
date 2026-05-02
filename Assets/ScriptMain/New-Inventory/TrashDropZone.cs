using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TrashDropZone : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Trash Icon")]
    public Image trashImage;
    public Color normalColor = Color.white;
    public Color hoverColor = new Color(1f, 0.25f, 0.25f, 1f);
    public float swallowDuration = 0.4f;

    void Start()
    {
        if (trashImage == null) trashImage = GetComponent<Image>();
        if (trashImage != null) trashImage.color = normalColor;
    }

    // Called when the item is released over the bin
    public void OnDrop(PointerEventData eventData)
    {
        GameObject droppedObject = eventData.pointerDrag;
        if (droppedObject == null) return;

        InventoryItems draggedItem = droppedObject.GetComponent<InventoryItems>();

        // Ensure we have a valid item and access to the network data
        if (draggedItem != null && InventoryMainUIs.Instance.activeData != null)
        {
            // 1. Tell the item it was "caught" so it doesn't ReturnToSource
            draggedItem.wasDroppedSuccessfully = true;

            // 2. Trigger the Network Request to delete the item
            // We can reuse or create a simple Delete RPC
            int slotIdx = draggedItem.sourceSlot.slotIndex;
            InventoryMainUIs.Instance.activeData.RequestDeleteItemServerRpc(slotIdx);

            // 3. Visual Animation: Use the sprite from the dragged item
            if (draggedItem.item != null)
            {
                StartCoroutine(ShrinkAndSwallowAnimation(draggedItem.item.icon, eventData.position));
            }

            // 4. Destroy the dragged icon immediately
            Destroy(draggedItem.gameObject);
        }

        if (trashImage != null) trashImage.color = normalColor;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Only highlight if the user is actually dragging an item
        if (eventData.pointerDrag != null && eventData.pointerDrag.GetComponent<InventoryItems>())
        {
            if (trashImage != null) trashImage.color = hoverColor;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (trashImage != null) trashImage.color = normalColor;
    }

    // Your existing animation logic works fine here!
    IEnumerator ShrinkAndSwallowAnimation(Sprite sprite, Vector2 screenPos)
    {
        GameObject tempGO = new GameObject("_SwallowIcon");
        tempGO.transform.SetParent(InventoryMainUIs.Instance.transform, false); // Parent to Canvas

        RectTransform rect = tempGO.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(50, 50);
        rect.position = screenPos;

        Image img = tempGO.AddComponent<Image>();
        img.sprite = sprite;
        img.raycastTarget = false;

        float elapsed = 0f;
        while (elapsed < swallowDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / swallowDuration;
            rect.position = Vector3.Lerp(screenPos, transform.position, t);
            rect.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, t);
            img.color = new Color(1f, 1f, 1f, 1f - t);
            yield return null;
        }
        Destroy(tempGO);
    }
}