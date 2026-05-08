using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemShowUI : MonoBehaviour
{
    [SerializeField] Image itemIcon;
    [SerializeField] TextMeshProUGUI amountText;

    public void Setup(SoldItemEntry entry)
    {
        amountText.text = $"x{entry.Amount}";
        itemIcon.sprite = GameDataManager.Instance.itemDatabases.GetItemByID(entry.ItemID).icon;
    }
}
