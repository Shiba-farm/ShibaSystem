using Unity.Netcode;
using UnityEngine;

public class PlayerMagnet : NetworkBehaviour
{
    [SerializeField] private Transform handAnchor;
    [SerializeField] private float detectionRadius = 4f;
    [SerializeField] private float collectRadius = 0.5f;
    private readonly Collider[] _hitBuffer = new Collider[16]; // avoid alloc

    private void FixedUpdate()
    {
        // only run the magnet logic for the local owner
        if (!IsOwner) return;

        int count = Physics.OverlapSphereNonAlloc(
            transform.position, detectionRadius, _hitBuffer);

        for (int i = 0; i < count; i++)
        {
            if (!_hitBuffer[i].CompareTag("PickupItem")) continue;

            if (_hitBuffer[i].TryGetComponent<IPickupable>(out var pickup))
            {
                pickup.OnMagnetPull(handAnchor);   // local visual pull

                // close enough — ask server to complete the pickup
                // Debug.Log($"Distance = {Vector3.Distance(transform.position,_hitBuffer[i].transform.position)}; Is it close enough = {Vector3.Distance(transform.position, _hitBuffer[i].transform.position) < collectRadius}");
                if (Vector3.Distance(handAnchor.position,
                        _hitBuffer[i].transform.position) < collectRadius)
                {
                    var netObj = _hitBuffer[i].GetComponent<NetworkObject>();
                    RequestPickupServerRpc(netObj.NetworkObjectId);
                }
            }
        }
    }

    [ServerRpc]
    private void RequestPickupServerRpc(ulong networkObjectId)
    {
        // Debug.Log("Start");
        if (!NetworkManager.SpawnManager.SpawnedObjects
                .TryGetValue(networkObjectId, out var netObj)) return;

        // Debug.Log("Pass NetworkObject check");
        if (!netObj.TryGetComponent<IPickupable>(out var pickup)) return;
        // Debug.Log("Pass IPickable check");

        // server-authoritative: add to inventory then destroy
        InventoryNetworkManager.Instance.RequestAddItemServerRpc(0, pickup.ItemID, pickup.Quantity);
        pickup.OnPickedUp(OwnerClientId);
    }
}
