using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MapMarkerUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI labelText;
    private RectTransform _rect;

    private RectTransform Rect => _rect ??= GetComponent<RectTransform>();

    public void Setup(IMapMarkerSource source)
    {
        if (iconImage != null && source.Icon != null) iconImage.sprite = source.Icon;
        if (labelText != null) labelText.text = source.Label;
    }

    /// <summary>uv (0,0)=ล่างซ้าย .. (1,1)=บนขวา ของพื้นที่แผนที่</summary>
    public void SetNormalizedPosition(Vector2 uv, Vector2 mapSize)
    {
        // Marker prefab ใช้ anchor กึ่งกลาง (0.5, 0.5) ดังนั้น anchoredPosition (0,0)
        // คือ "กลาง" ของ mapContent ไม่ใช่มุมล่างซ้าย — ต้องลบ 0.5 ออกจาก uv ก่อน
        // คูณด้วยขนาดแผนที่ ไม่งั้นหมุดทั้งหมดจะเลื่อนไปครึ่งนึงของแผนที่ผิดตำแหน่ง
        // (uv 0,0 ที่ควรอยู่มุมล่างซ้าย จะไปโผล่กลางจอแทน และ uv 1,1 จะหลุดขอบจอไปเลย)
        Rect.anchoredPosition = new Vector2((uv.x - 0.5f) * mapSize.x, (uv.y - 0.5f) * mapSize.y);
    }
}
