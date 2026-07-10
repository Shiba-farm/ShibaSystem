using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class CraftingManager : NetworkBehaviour
{
    public static CraftingManager Instance { get; private set; }
    public event Action<int[]> OnRecipesUpdated;
    public event Action<CraftingRecipeSO> OnRecipeCrafted;
    public event Action<string> OnRecipeLearned;
    void Awake()
    {
        if(Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestAvailableRecipesRpc(RecipeCategory category, RpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;

        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var networkClient))
        {
            var playerObject = networkClient.PlayerObject;
            var statManager = playerObject.GetComponent<StatManager>();

            int actualLevel = statManager.GetLevelForCategory(category);

            List<CraftingRecipeSO> available = GameDataManager.Instance.craftRecipeDatabase.GetAvailableRecipes(actualLevel, category);

            int[] recipeIds = available.Select(r => r.recipeID).ToArray();

            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } }
            };

            UpdateRecipeListClientRpc(recipeIds, clientRpcParams);
        }
    }
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestCraftItemRpc(int recipeId, RpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        CraftingRecipeSO recipe = GameDataManager.Instance.craftRecipeDatabase.GetRecipeByID(recipeId);

        if (recipe == null) return;

        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var networkClient))
        {
            var playerObject = networkClient.PlayerObject;

            var inventoryData = playerObject.GetComponentInChildren<InventoryData>();
            foreach (var ing in recipe.ingredients)
            {
                if (ing.item == null) continue;
                if (inventoryData.GetItemCount(ing.item.itemID) < ing.amount)
                {
                    Debug.Log($"Client {clientId} does not have enough of item {ing.item.itemName} to craft {recipe.recipeName}");
                    return;
                }
            }

            foreach (var ing in recipe.ingredients)
            {
                inventoryData.RemoveItem(ing.item.itemID, ing.amount);
            }

            Debug.Log($"Crafting recipe {recipe.recipeName} for client {clientId}. Adding item {recipe.resultItem.itemName} x{recipe.resultAmount} to inventory.");

            inventoryData.AddItem(recipe.resultItem.itemID, recipe.resultAmount);

            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { clientId } }
            };
            NotifyCraftSuccessClientRpc(recipeId, clientRpcParams);
        }
    }

    [ClientRpc]
    private void UpdateRecipeListClientRpc(int[] recipeIds, ClientRpcParams clientRpcParams = default)
    {
        OnRecipesUpdated?.Invoke(recipeIds);
    }
    [ClientRpc]
    private void NotifyCraftSuccessClientRpc(int recipeId, ClientRpcParams clientRpcParams = default)
    {
        // Local client only: Update UI, play sound, show "Item Crafted!" popup
        CraftingRecipeSO recipe = GameDataManager.Instance.craftRecipeDatabase.GetRecipeByID(recipeId);
        OnRecipeCrafted?.Invoke(recipe);
    }

}

public enum CraftResult
{
    Success,
    NotEnoughMaterials,
    NotLearned,
    InventoryFull,
    NotEnoughEnergy,
    InvalidRecipe,
    NoInventory,
}
