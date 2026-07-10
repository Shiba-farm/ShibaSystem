using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CategoryRowUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI categoryText;
    [SerializeField] Transform itemContainer;
    [SerializeField] GameObject itemShowPrefab;
    [SerializeField] TextMeshProUGUI totalText;
    [SerializeField] Button rowButton;

    public void Setup(CategorySellRecord record, Action<CategorySellRecord> onClick)
    {
        categoryText.text = record.Category.ToString();
        totalText.text = $"Total : {record.TotalGold}";
        rowButton.onClick.AddListener(() => onClick(record));

        foreach (var item in record.Items)
        {
            var show = Instantiate(itemShowPrefab, itemContainer);
            show.GetComponent<ItemShowUI>().Setup(item);
        }
    }
}
