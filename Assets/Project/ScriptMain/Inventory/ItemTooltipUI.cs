using TMPro;
using UnityEngine;

/// <summary>
/// กล่อง Tooltip กล่องเดียวใช้ร่วมกันทั้งเกม (ไม่ต้องสร้างซ้ำต่อช่อง Inventory) —
/// InventoryItems เรียก Show()/Hide() ตอนเมาส์ชี้/ออกจาก item แต่ละชิ้น
///
/// โชว์ชื่อ + ราคาเสมอ ถ้าไอเทมเป็น WearableItemSO จะโชว์ค่าสถานะ (Defense/Speed/
/// Attack) เฉพาะค่าที่ "ไม่ใช่ 0" เท่านั้น — ไอเทมไหนไม่ได้ตั้งค่าไหนไว้ (ปล่อย 0
/// ตาม default) บรรทัดนั้นจะไม่โชว์ ทำให้ของแต่ละชิ้นโชว์แค่ค่าที่เกี่ยวข้องจริง ๆ
/// (เช่น รองเท้าตั้งแค่ Speed ก็จะเห็นแค่ Speed ไม่เห็น Defense/Attack ว่าง ๆ)
///
/// Setup ใน Inspector (ดูวิธีสร้าง Panel ในคำอธิบายที่แชทตอบกลับมา):
///   Panel Root  → RectTransform ของกล่อง tooltip ทั้งกล่อง (เปิด/ปิดด้วย SetActive)
///   Name Text   → TMP โชว์ชื่อไอเทม
///   Price Text  → TMP โชว์ราคาขาย
///   Stats Text  → TMP โชว์ค่าสถานะ (ซ่อนอัตโนมัติถ้าไม่มีค่าไหนเลย)
/// </summary>
public class ItemTooltipUI : MonoBehaviour
{
    public static ItemTooltipUI Instance { get; private set; }

    [SerializeField] private RectTransform panelRoot;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private TextMeshProUGUI statsText;
    [Tooltip("ระยะห่างเพิ่มเติมจากมุมช่องไอเทม กันไม่ให้ Tooltip ชิดช่องไอเทมเกินไป")]
    [SerializeField] private Vector2 cursorOffset = new Vector2(8f, 8f);

    private void Awake()
    {
        Instance = this;

        // บังคับ pivot ที่ (0,0) เสมอ — PositionNear() เอามุม "ขวาบน" ของช่องไอเทมมาเป็น
        // จุดตั้ง Tooltig ต้องกาง "ออกจากจุดนั้น" ไปทางขวา-บน (ไม่ใช่กางออกรอบจุดนั้นแบบ
        // pivot กลาง) ถึงจะไม่ทับช่องไอเทมที่ชี้อยู่ ไม่ต้องไปตั้งเองใน Inspector
        if (panelRoot != null) panelRoot.pivot = new Vector2(0f, 0f);

        Hide();
    }

    public void Show(ItemSO item, RectTransform sourceRect)
    {
        if (item == null)
        {
            Hide();
            return;
        }

        if (nameText != null) nameText.text = item.itemName;
        if (priceText != null) priceText.text = item.sellable ? $"Sell: {item.sellPrice}" : "Cannot sell";

        if (statsText != null)
        {
            string stats = BuildStatsText(item);
            statsText.text = stats;
            statsText.gameObject.SetActive(!string.IsNullOrEmpty(stats));
        }

        if (panelRoot != null) panelRoot.gameObject.SetActive(true);
        PositionNear(sourceRect);
    }

    /// <summary>รวมบรรทัดค่าสถานะ — เพิ่ม field ใหม่ใน WearableItemSO แล้วเพิ่มเงื่อนไขที่นี่ได้เลย</summary>
    private static string BuildStatsText(ItemSO item)
    {
        if (item is not WearableItemSO wearable) return string.Empty;

        var lines = new System.Collections.Generic.List<string>();
        if (wearable.defenseBonus != 0) lines.Add($"Defense +{wearable.defenseBonus}");
        if (wearable.speedBonus != 0) lines.Add($"Speed +{wearable.speedBonus}");
        if (wearable.attackBonus != 0) lines.Add($"Attack +{wearable.attackBonus}");
        return string.Join("\n", lines);
    }

    /// <summary>
    /// ตั้งตำแหน่ง Tooltip ให้กางออกจากมุมขวาบนของ sourceRect (ช่องไอเทมที่ชี้อยู่) —
    /// ใช้ Transform.position (world space) ตรง ๆ แทนการแปลง screen point ไป-กลับ เพราะ
    /// Unity จัดการเรื่อง parent/scale/canvas ให้ถูกต้องเองเสมอไม่ว่า Tooltip จะถูกวางไว้
    /// ลึกแค่ไหนในลำดับชั้น (บั๊กเดิมที่เจอคือแปลงเทียบผิด parent ตำแหน่งเลยเพี้ยนไปไกล)
    /// </summary>
    private void PositionNear(RectTransform sourceRect)
    {
        if (panelRoot == null || sourceRect == null) return;

        Vector3[] corners = new Vector3[4];
        sourceRect.GetWorldCorners(corners); // 0=bottom-left, 1=top-left, 2=top-right, 3=bottom-right

        panelRoot.position = corners[2];             // ตั้งที่มุมขวาบนของช่องไอเทมตรง ๆ (world space)
        panelRoot.anchoredPosition += cursorOffset;   // ค่อยขยับเพิ่มอีกนิด กันชิดเกินไป
    }

    public void Hide()
    {
        if (panelRoot != null) panelRoot.gameObject.SetActive(false);
    }
}
