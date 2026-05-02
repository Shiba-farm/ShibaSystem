using UnityEngine;
using UnityEngine.UI;

public class InventoryDragHandler : MonoBehaviour
{
    public static InventoryDragHandler Instance { get; private set; }

    [Header("UI Drag Icon")]
    [SerializeField] private Image dragIcon;
    [SerializeField] private Canvas canvas;

    public bool IsDragging { get; private set; }

    public ItemSO dragItem { get; private set; }
    public int dragAmount { get; private set; }

    public InventorySlot draggedFromSlot { get; private set; }
    public HotbarSlot draggedFromHotbar { get; private set; }

    private RectTransform rect;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (dragIcon != null)
        {
            rect = dragIcon.GetComponent<RectTransform>();
            dragIcon.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        // ให้ไอคอนลากตามเมาส์
        if (IsDragging && rect != null)
        {
            rect.position = Input.mousePosition;
        }
    }

    // ----------------- public API -----------------

    public void BeginDrag(InventorySlot slot)
    {
        if (slot == null || slot.item == null)
            return;

        IsDragging = true;
        draggedFromSlot = slot;
        draggedFromHotbar = null;

        dragItem = slot.item;
        dragAmount = slot.amount;

        ShowIcon(dragItem.icon);
    }

    public void BeginDragFromHotbar(HotbarSlot hb)
    {
        if (hb == null || hb.item == null)
            return;

        IsDragging = true;
        draggedFromSlot = null;
        draggedFromHotbar = hb;

        dragItem = hb.item;
        dragAmount = hb.amount;

        ShowIcon(dragItem.icon);
    }

    public void EndDrag()
    {
        IsDragging = false;
        draggedFromSlot = null;
        draggedFromHotbar = null;
        dragItem = null;
        dragAmount = 0;

        HideIcon();
    }

    // ----------------- helper -----------------

    private void ShowIcon(Sprite sprite)
    {
        if (!dragIcon) return;

        dragIcon.sprite = sprite;
        //dragIcon.SetNativeSize();
        dragIcon.gameObject.SetActive(true);

        if (canvas)
            canvas.overrideSorting = true;
    }

    private void HideIcon()
    {
        if (dragIcon)
            dragIcon.gameObject.SetActive(false);

        if (canvas)
            canvas.overrideSorting = false;
    }
}
