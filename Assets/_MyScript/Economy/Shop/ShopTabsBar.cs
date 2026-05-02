using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// จัดตำแหน่งปุ่มแท็บแบบกำหนดเอง (ไม่ใช้ LayoutGroup)
/// - จัดเรียงซ้าย -> ขวา ตาม Spacing/Padding
/// - เลือกได้: ปุ่มกว้างคงที่ หรือคำนวณจากข้อความ (AutoWidth)
/// - เลือกได้: ขึ้นบรรทัดใหม่เมื่อเต็มความกว้าง (Wrap)
/// </summary>
[ExecuteAlways]
public class ShopTabsBar : MonoBehaviour
{
    [Header("Padding (px)")]
    public float paddingLeft = 10f;
    public float paddingRight = 10f;
    public float paddingTop = 8f;
    public float paddingBottom = 8f;

    [Header("Spacing (px)")]
    public float spacingX = 8f;
    public float spacingY = 6f;

    [Header("Size")]
    public bool autoWidthFromText = false;
    public float fixedWidth = 120f;      // ใช้เมื่อติก autoWidthFromText = false
    public float height = 40f;
    public float extraTextWidth = 24f;   // เผื่อ padding ของปุ่มเมื่อคำนวณจากข้อความ

    [Header("Behavior")]
    public bool wrapToNextLine = false;  // ถ้าเกินความกว้างให้ตกล่าง
    public bool alignTop = true;         // ปักพิกัดจากบนซ้าย

    RectTransform RT => transform as RectTransform;

    void OnEnable() { LayoutChildren(); }
    void OnTransformChildrenChanged() { LayoutChildren(); }
#if UNITY_EDITOR
    void OnValidate() { LayoutChildren(); }
#endif
    void OnRectTransformDimensionsChange() { LayoutChildren(); }

    public void LayoutChildren()
    {
        if (RT == null) return;

        // ใช้พิกัดจาก "บนซ้าย" จะทำให้วางตำแหน่งง่าย
        Vector2 pivot = new Vector2(0f, 1f);
        float contentWidth = Mathf.Max(0f, RT.rect.width - paddingLeft - paddingRight);

        float x = paddingLeft;
        float y = -paddingTop; // ค่าติดลบเพราะ anchoredPosition.y บนลงล่างเป็นค่าลบ

        for (int i = 0; i < RT.childCount; i++)
        {
            var child = RT.GetChild(i) as RectTransform;
            if (child == null || !child.gameObject.activeSelf) continue;

            // บังคับ anchor/pivot ให้เป็นบนซ้าย
            child.anchorMin = new Vector2(0f, 1f);
            child.anchorMax = new Vector2(0f, 1f);
            child.pivot = pivot;
            child.localScale = Vector3.one;

            // คำนวณความกว้าง
            float w = fixedWidth;
            if (autoWidthFromText)
            {
                // พยายามหา TMP บนปุ่มเพื่อดู preferredWidth
                float textW = 60f;
                var tmp = child.GetComponentInChildren<TextMeshProUGUI>(true);
                if (tmp != null)
                {
#if UNITY_EDITOR
                    // ในโหมดแก้ไข ให้ UpdateGeometry เพื่อคำนวณ preferred ใหม่
                    tmp.ForceMeshUpdate();
#endif
                    textW = tmp.preferredWidth;
                }
                w = Mathf.Ceil(textW + extraTextWidth);
            }

            float h = height;

            // wrap ลงบรรทัดถ้าเกิน
            if (wrapToNextLine && (x - paddingLeft + w > contentWidth) && (x > paddingLeft))
            {
                x = paddingLeft;
                y -= (h + spacingY);
            }

            child.sizeDelta = new Vector2(w, h);
            child.anchoredPosition = new Vector2(x, y);

            x += w + spacingX;
        }

        // ปรับความสูงของแถบ (ถ้าต้องการให้สูงพอดีคอนเทนต์)
        // หมายเหตุ: ถ้าอยาก “สูงคงที่” ก็ไม่ต้องแตะ sizeDelta
        float usedRows = 1f;
        if (wrapToNextLine)
        {
            // ประมาณจำนวนแถวจากระยะ y สุดท้าย
            float totalUsedHeight = paddingTop + (-y) + hLast(RT) + paddingBottom;
            var sd = RT.sizeDelta;
            RT.sizeDelta = new Vector2(sd.x, totalUsedHeight);
        }

        float hLast(RectTransform t)
        {
            // คืนค่าความสูงปุ่มสุดท้ายที่เราใช้ (หรือ fallback เป็น height)
            return height;
        }
    }
}
