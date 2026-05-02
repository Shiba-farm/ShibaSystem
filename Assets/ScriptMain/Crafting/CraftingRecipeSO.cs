using UnityEngine;

public enum RecipeCategory { Tools, Food, Structures, Resources, FarmHelper, Wearables }

[CreateAssetMenu(menuName = "Crafting/Recipe")]
public class CraftingRecipeSO : ScriptableObject
{
    [Header("Recipe Info")]
    [Tooltip("General Info")]
    public string recipeName;
    public int recipeID;
    public RecipeCategory category;

    [Tooltip("Description")]
    [TextArea(2, 4)]
    public string description;

    [Tooltip("Recipe icon")]
    public Sprite icon;

    [Header("Ingredients")]
    public CraftingIngredient[] ingredients;

    [Header("Result")]
    [Tooltip("Result ItemSO")]
    public ItemSO resultItem;

    [Tooltip("Amount")]
    [Min(1)]
    public int resultAmount = 1;

    [Header("Requirements")]
    [Tooltip("Require learning")]
    public bool requiresLearning = false;

    [Tooltip("Workbench level requirement")]
    [Min(0)]
    public int minWorkbenchLevel = 0;

    [Tooltip("Energy cost")]
    [Min(0f)]
    public float energyCost = 5f;
    public bool CanCraft(System.Func<ItemSO, int> getItemCount)
    {
        if (ingredients == null) return false;
        foreach (var ing in ingredients)
        {
            if (ing.item == null) continue;
            if (getItemCount(ing.item) < ing.amount) return false;
        }
        return true;
    }

    [Tooltip("Item perk")]
    public PerkDataSO itemPerk = null;

    [Tooltip("Craft item stat")]
    public ItemStatDataSO itemStat = null;
}

[System.Serializable]
public class CraftingIngredient
{
    public ItemSO item;
    [Min(1)]
    public int amount = 1;
}
