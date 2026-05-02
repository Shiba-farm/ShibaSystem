using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class CraftingMainUI : MonoBehaviour
{
    [Header("Detail Panel References")]
    [SerializeField] private TextMeshProUGUI detailName;
    [SerializeField] private TextMeshProUGUI detailDesc;
    [SerializeField] private Image detailIcon;
    [SerializeField] private Transform ingredientListParent;
    [SerializeField] private GameObject ingredientChildPrefab;
    [SerializeField] private Transform recipeContainer;
    [SerializeField] private Transform statContainer;
    [SerializeField] private GameObject recipeItemPrefab;
    [SerializeField] private Image perkIcon;
    [SerializeField] private CraftStatUIItem craftStatUIItemPrefab;
    [Header("Buttons")]
    [SerializeField] private Button craftButton;
    [SerializeField] private List<CategoryButton> categoryButtons;
    [Serializable]
    public struct CategoryButton
    {
        public RecipeCategory category;
        public Button button;
    }

    private RecipeCategory currentCategory = RecipeCategory.Tools;
    private CraftingRecipeSO currentSelectedRecipe;
    void Awake()
    {
        if (recipeContainer == null)
        {
            // Find the container by name or tag if it lives in the UI Canvas
            GameObject containerObj = GameObject.Find("RecipeListContent");
            if (containerObj != null) recipeContainer = containerObj.transform;
        }
    }

    void Start()
    {
        CraftingManager.Instance.OnRecipeCrafted -= HandleRecipeCrafted;
        CraftingManager.Instance.OnRecipeLearned -= HandleRecipeLearned;

        CraftingManager.Instance.OnRecipeCrafted += HandleRecipeCrafted;
        CraftingManager.Instance.OnRecipeLearned += HandleRecipeLearned;

        foreach (CategoryButton cb in categoryButtons)
        {
            RecipeCategory captured = cb.category;
            cb.button.onClick.AddListener(() => OnCategoryButtonClicked(captured));
        }
    }

    public void OnRecipeUIIemClicked(CraftingRecipeSO recipe)
    {
        SelectRecipe(recipe);
    }

    public void OnCraftButtonClicked()
    {
        Debug.Log("Craft button clicked for recipe: " + (currentSelectedRecipe != null ? currentSelectedRecipe.recipeName : "None"));
        if (currentSelectedRecipe == null) return;
        CraftingManager.Instance.RequestCraftItemRpc(currentSelectedRecipe.recipeID);
    }

    void OnEnable()
    {
        if (CraftingManager.Instance == null)
        {
            Debug.Log("CraftingManager instance is null when CraftingMainUI enabled. Cannot subscribe to events or request recipes.");
            return;
        }
        else
        {
            Debug.Log("CraftingManager instance found when CraftingMainUI enabled. Subscribing to events.");
        }

        if (!CraftingManager.Instance.IsSpawned)
        {
            Debug.LogWarning("CraftingManager is not spawned yet. Waiting...");
            StartCoroutine(WaitAndRequest());
            return;
        }

        CraftingManager.Instance.OnRecipesUpdated += RefreshDisplay;
        RequestData();
    }
    IEnumerator WaitAndRequest()
    {
        // Wait until the network says this object is officially in the game
        yield return new WaitUntil(() => CraftingManager.Instance.IsSpawned);
        RequestData();
    }

    public void OnCategoryButtonClicked(RecipeCategory category)
    {
        if (currentCategory == category) return;
        currentCategory = category;
        RequestData();
    }

    private void RequestData()
    {
        CraftingManager.Instance.RequestAvailableRecipesRpc(currentCategory);
    }
    void OnDisable()
    {
        if (CraftingManager.Instance != null)
            CraftingManager.Instance.OnRecipesUpdated -= RefreshDisplay;
    }

    private void HandleRecipeLearned(string obj)
    {
        throw new NotImplementedException();
    }

    private void HandleRecipeCrafted(CraftingRecipeSO sO)
    {
        Debug.Log("CraftingMainUI received OnRecipeCrafted event for recipe: " + sO.recipeName);
    }

    public void RefreshDisplay(int[] recipeIds)
    {
        if (recipeItemPrefab == null)
        {
            Debug.Log("recipeItemPrefab is not assigned on " + gameObject.name, gameObject);
            return;
        }
        Debug.Log($"Refreshing recipe display with {recipeIds.Length} recipes. Clearing existing items.");
        if (recipeContainer == null)
        {
            Debug.Log("Recipe container reference is missing in CraftingMainUI. Cannot refresh display.");
            return;
        }
        foreach (Transform child in recipeContainer)
        {
            Destroy(child.gameObject);
        }

        foreach (int id in recipeIds)
        {
            CraftingRecipeSO recipe = GameDataManager.Instance.craftRecipeDatabase.GetRecipeByID(id);
            if (recipe != null)
            {
                GameObject item = Instantiate(recipeItemPrefab, recipeContainer);
                item.GetComponent<RecipeUIItem>().Setup(recipe);
                Button btn = item.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.AddListener(() => OnRecipeUIIemClicked(recipe));
                }
                else
                {
                    Debug.LogWarning("Recipe item prefab does not have a Button component. Cannot add click listener.");
                }
            }
        }

        if (recipeIds.Length > 0)
        {
            CraftingRecipeSO defaultRecipe = GameDataManager.Instance.craftRecipeDatabase.GetRecipeByID(recipeIds[0]);
            if (defaultRecipe != null) SelectRecipe(defaultRecipe);
        }
    }

    public void SelectRecipe(CraftingRecipeSO recipe)
    {
        currentSelectedRecipe = recipe;
        detailName.text = recipe.recipeName;
        detailDesc.text = recipe.description;
        detailIcon.sprite = recipe.icon;

        if (recipe.itemPerk == null)
        {
            perkIcon = null;
        }
        else
        {
            perkIcon = recipe.itemPerk.perkIcon;
        }

        if (recipe.itemStat != null)
        {
            foreach (Transform child in statContainer)
            {
                Destroy(child.gameObject);
            }
            foreach (var stat in recipe.itemStat.itemStats)
            {
                Debug.Log($"Adding stat {stat.Type} with amount {stat.Amount} to craft stat UI.");
                CraftStatUIItem item = Instantiate(craftStatUIItemPrefab, statContainer);
                item.Setup(stat);
            }
        }

        UpdateIngredientDisplay(recipe);
    }

    private void UpdateIngredientDisplay(CraftingRecipeSO recipe)
    {
        foreach (Transform child in ingredientListParent)
        {
            Destroy(child.gameObject);
        }

        foreach (var ingredient in recipe.ingredients)
        {
            GameObject item = Instantiate(ingredientChildPrefab, ingredientListParent);
            IngredientUIItem uiItem = item.GetComponent<IngredientUIItem>();
            uiItem.Setup(ingredient);
        }
    }
}
