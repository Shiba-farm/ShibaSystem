using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class IngredientUIItem : MonoBehaviour
{
    [Header("Ingredient Detail Item")]
    [SerializeField] private TextMeshProUGUI ingredientName;
    [SerializeField] private TextMeshProUGUI ingredientAmount;
    [SerializeField] private Image ingredientIcon;

    internal void Setup(CraftingIngredient ingredient)
    {
        if (ingredient == null || ingredient.item == null)
        {
            ingredientName.text = "Empty";
            ingredientAmount.text = "";
            ingredientIcon.sprite = null;
            return;
        }

        ingredientName.text = ingredient.item.itemName;
        ingredientAmount.text = $"x{ingredient.amount}";
        ingredientIcon.sprite = ingredient.item.icon;
    }
}
