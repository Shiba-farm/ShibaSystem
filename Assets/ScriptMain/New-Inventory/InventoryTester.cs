using UnityEngine;

public class InventoryTester : MonoBehaviour
{
    public InventoryData targetInventory;

    void Update()
    {
        // Press T to inject mock data
        if (Input.GetKeyDown(KeyCode.T))
        {
            if (targetInventory.IsServer) // Still needs to be server
            {
                targetInventory.InventoryItems[0] = new NetworkItems { ItemID = 1, Amount = 1 };
                targetInventory.InventoryItems[1] = new NetworkItems { ItemID = 2, Amount = 5 };
            }
        }
    }
}
