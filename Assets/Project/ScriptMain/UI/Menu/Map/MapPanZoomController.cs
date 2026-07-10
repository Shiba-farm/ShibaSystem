using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Pan (ลากเมาส์) + Zoom (scroll wheel) ของรูปแผนที่ — แยกเป็น component เดี่ยว
/// ไม่ผูกกับ MapTabView เลย เพื่อให้ใช้ซ้ำกับแผนที่อื่น ๆ ได้ (เช่น mini-map ในอนาคต)
/// แปะไว้ที่ object เดียวกับรูปแผนที่ (content ที่อยู่ใน viewport ที่มี mask)
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class MapPanZoomController : MonoBehaviour, IDragHandler, IScrollHandler
{
    [SerializeField] private float zoomSpeed = 0.1f;
    [SerializeField] private float minZoom = 1f;
    [SerializeField] private float maxZoom = 3f;
    [SerializeField] private RectTransform viewport; // มี mask ครอบ — ใช้ clamp ขอบ

    private RectTransform _content;
    private RectTransform Content => _content ??= GetComponent<RectTransform>();

    public void OnDrag(PointerEventData eventData)
    {
        Content.anchoredPosition += eventData.delta;
        ClampToViewport();
    }

    public void OnScroll(PointerEventData eventData)
    {
        float newScale = Mathf.Clamp(Content.localScale.x + eventData.scrollDelta.y * zoomSpeed, minZoom, maxZoom);
        Content.localScale = new Vector3(newScale, newScale, 1f);
        ClampToViewport();
    }

    private void ClampToViewport()
    {
        if (viewport == null) return;

        Vector2 contentSize = Content.rect.size * Content.localScale.x;
        Vector2 viewportSize = viewport.rect.size;
        Vector2 maxOffset = Vector2.Max((contentSize - viewportSize) * 0.5f, Vector2.zero);

        Vector2 pos = Content.anchoredPosition;
        pos.x = Mathf.Clamp(pos.x, -maxOffset.x, maxOffset.x);
        pos.y = Mathf.Clamp(pos.y, -maxOffset.y, maxOffset.y);
        Content.anchoredPosition = pos;
    }
}
