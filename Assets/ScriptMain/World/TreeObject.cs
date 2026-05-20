using Unity.Netcode;
using UnityEngine;

public class TreeObject : DestructibleObject
{
    [SerializeField] private int woodItemID;
    [SerializeField] private int woodAmount = 3;
    // [SerializeField] private GameObject shakeVFX;

    // runs on all clients — just visuals
    protected override void OnHealthChanged(int prev, int next)
    {
        // play shake/hit vfx locally on every client
        // if (shakeVFX != null)
        //     shakeVFX.SetActive(true);
    }

    // runs on server only
    protected override void OnDepleted()
    {
        // spawn loot
        NetworkItemSpawner.Instance.SpawnItem(
            woodItemID, woodAmount, transform.position);

        // despawn the tree for all clients
        GetComponent<NetworkObject>().Despawn();
    }
}
