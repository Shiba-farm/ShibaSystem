using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "CraftRecipeDatabase", menuName = "Crafting/CraftRecipeDatabase")]
public class CraftRecipeDatabase : ScriptableObject
{
    [Serializable]
    public struct RecipeAsGroup
    {
        public RecipeCategory category;
        public List<RecipeAsID> recipes;
    }
    [Serializable]
    public struct RecipeAsID
    {
        public int recipeID;
        public CraftingRecipeSO recipe;
    }
    [SerializeField] private List<RecipeAsGroup> allRecipes = new List<RecipeAsGroup>();
    private Dictionary<RecipeCategory, List<RecipeAsID>> recipeLookupCategories = new Dictionary<RecipeCategory, List<RecipeAsID>>();
    private Dictionary<int, CraftingRecipeSO> recipeLookupIDs = new Dictionary<int, CraftingRecipeSO>();
    private bool isInitialized = false;

    public void Initialize()
    {
        recipeLookupCategories.Clear();
        recipeLookupIDs.Clear();
        // For Recipe Categories
        foreach (var group in allRecipes)
        {
            Debug.Log($"Processing recipe group for category {group.category} with {group.recipes?.Count ?? 0} recipes.");
            if (group.recipes == null) continue;

            // 1. Map the category to the list
            recipeLookupCategories[group.category] = group.recipes;

            // 2. Map every individual recipe ID
            foreach (var subRecipe in group.recipes)
            {
                if (subRecipe.recipe == null) continue;

                if (!recipeLookupIDs.TryAdd(subRecipe.recipeID, subRecipe.recipe))
                {
                    Debug.LogError($"Duplicate Recipe ID found: {subRecipe.recipeID} in {group.category}");
                }
            }
        }
        isInitialized = true;
    }
    public CraftingRecipeSO GetRecipeByID(int id)
    {
        if (!isInitialized || recipeLookupCategories.Count == 0)
            Initialize();

        if (recipeLookupIDs.TryGetValue(id, out var recipe))
        {
            return recipe;
        }

        Debug.LogWarning($"Recipe ID {id} not found in database!");
        return null;
    }

    public List<CraftingRecipeSO> GetAvailableRecipes(int workbenchLevel, RecipeCategory category)
    {
        if (!isInitialized) Initialize();

        if (recipeLookupCategories.TryGetValue(category, out var recipeList))
        {
            Debug.Log($"Found {recipeList.Count} recipes in category {category}. Filtering by workbench level {workbenchLevel}.");
            return recipeList
                .Select(r => r.recipe)
                .Where(r => !r.requiresLearning && r.minWorkbenchLevel <= workbenchLevel)
                .ToList();
        }
        Debug.LogWarning($"No recipes found for category {category}!");

        return new List<CraftingRecipeSO>();
    }
}
