using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RecipeUIItem : MonoBehaviour
{
    [Header("UI References")]
    public Image iconImage;
    public TextMeshProUGUI nameText;

    private CraftingRecipeSO currentRecipe;

    public void Setup(CraftingRecipeSO recipe)
    {
        currentRecipe = recipe;

        iconImage.sprite = recipe.icon;
        nameText.text = recipe.recipeName;
    }

    private void OnClicked()
    {
        // CraftingMainUI.Instance.SelectRecipe(currentRecipe);
    }
}
