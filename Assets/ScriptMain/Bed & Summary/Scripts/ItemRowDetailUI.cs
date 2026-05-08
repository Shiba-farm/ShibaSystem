using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemRowDetailUI : MonoBehaviour
{
    [SerializeField] Image itemIcon;
    [SerializeField] TextMeshProUGUI itemName;
    [SerializeField] TextMeshProUGUI amountText;
    [SerializeField] TextMeshProUGUI goldText;

    public void Setup(SoldItemEntry entry)
    {
        itemName.text = entry.ItemName;
        amountText.text = $"x{entry.Amount}";
        goldText.text = $"${entry.GoldEarned}";
        itemIcon.sprite = GameDataManager.Instance.itemDatabases.GetItemByID(entry.ItemID).icon;
    }
}
