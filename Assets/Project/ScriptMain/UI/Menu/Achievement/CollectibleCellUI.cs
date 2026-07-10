using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>ช่องเดี่ยวในกริดของสะสม — โชว์ "???" ถ้ายังไม่ค้นพบ ตามที่ผู้เล่นระบุ</summary>
public class CollectibleCellUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI rarityText;
    [SerializeField] private Sprite defaultUnknownIcon;

    public void Setup(CollectibleDefinitionSO def, bool discovered)
    {
        if (discovered)
        {
            if (iconImage != null) { iconImage.sprite = def.icon; iconImage.color = Color.white; }
            if (nameText != null) nameText.text = def.displayName;
            if (rarityText != null) rarityText.text = def.rarity.ToString().ToLower();
        }
        else
        {
            if (iconImage != null)
            {
                iconImage.sprite = def.unknownIcon != null ? def.unknownIcon : defaultUnknownIcon;
                iconImage.color = new Color(1f, 1f, 1f, 0.5f);
            }
            if (nameText != null) nameText.text = "???";
            if (rarityText != null) rarityText.text = "unknown";
        }
    }
}
