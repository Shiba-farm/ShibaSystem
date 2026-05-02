using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopTabButton : MonoBehaviour
{
    public Button button;
    public TextMeshProUGUI label;

    [HideInInspector] public ShopCategory category;

    public void Setup(ShopCategory cat, System.Action<ShopCategory> onClick)
    {
        category = cat;
        if (label) label.text = cat.ToString();
        if (button == null) button = GetComponent<Button>();
        if (button) button.onClick.AddListener(() => onClick?.Invoke(cat));
    }

    public void SetActiveVisual(bool active)
    {
        // วิธีง่ายๆ: ปิดการกดบนแท็บที่เลือกอยู่ และเปลี่ยนสีเล็กน้อย
        if (button)
        {
            button.interactable = !active;
            var colors = button.colors;
            colors.normalColor = active ? new Color(0.9f, 0.9f, 1f) : Color.white;
            button.colors = colors;
        }
    }
}
