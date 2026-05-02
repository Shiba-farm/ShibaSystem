using Unity.Netcode;
using UnityEngine;

public class PlayerInventoryController : NetworkBehaviour
{
    [ServerRpc]
    public void RequestClickDebugServerRpc(int index, NetworkObjectReference inventoryRef)
    {
        if (inventoryRef.TryGet(out NetworkObject invNetObj))
        {
            string inventoryName = invNetObj.name;
            
            Debug.Log($"[SERVER] Player {OwnerClientId} clicked Slot {index} in {inventoryName}");
            
            NotifyClientsClientRpc(OwnerClientId, index, inventoryName);
        }
    }

    [ClientRpc]
    void NotifyClientsClientRpc(ulong clientId, int index, string invName)
    {
        Debug.Log($"[NET-SYNC] Player {clientId} interacted with {invName} at Slot {index}");
    }
}
