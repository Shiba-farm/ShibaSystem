
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LostItemRowUI : MonoBehaviour
{
    [SerializeField] private Image itemImageref;
    [SerializeField] private TextMeshProUGUI amountText;
    public void Setup(Sprite icon, int amount)
    {
        amountText.text = amount.ToString();
        itemImageref.sprite = icon;
    }
}
