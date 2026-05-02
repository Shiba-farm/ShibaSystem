using Unity.Netcode;
using UnityEngine;

public class DebugToHost : MonoBehaviour
{
    void Start()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogWarning("NetworkManager not found yet. Waiting...");
            return;
        }
        
        if (Application.isEditor && !NetworkManager.Singleton.IsServer && !NetworkManager.Singleton.IsClient)
        {
            Debug.Log("DEBUG: Automatically starting Host for Shiba testing...");
            NetworkManager.Singleton.StartHost();
        }
    }
}
