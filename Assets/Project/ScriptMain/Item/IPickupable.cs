using UnityEngine;

public interface IPickupable
{
    int ItemID { get; }
    int Quantity { get; }
    void OnMagnetPull(Transform target);
    void OnPickedUp(ulong clientId);
}
