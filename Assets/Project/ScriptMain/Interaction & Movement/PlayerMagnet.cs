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
                // ── ใช้ระยะ XZ เท่านั้น เพราะ item อยู่บนพื้น (Y ต่างกับ handAnchor) ──
                float xzDist = Vector2.Distance(
                    new Vector2(transform.position.x, transform.position.z),
                    new Vector2(_hitBuffer[i].transform.position.x,
                                _hitBuffer[i].transform.position.z));
                if (xzDist < collectRadius)
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
