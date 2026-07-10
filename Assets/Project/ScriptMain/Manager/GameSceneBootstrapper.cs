using Unity.Netcode;
using UnityEngine;

public class GameSceneBootstrapper : NetworkBehaviour
{
    [SerializeField] private GameObject inGameManagersPrefab;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        // Spawn the networked in-game managers for all clients
        GameObject managers = Instantiate(inGameManagersPrefab);
        managers.GetComponent<NetworkObject>().Spawn();
    }
}
