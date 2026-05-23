using Unity.Netcode;
using UnityEngine;

public abstract class NetworkSaveableBehaviour : NetworkBehaviour, ISaveable
{
    public abstract bool IsPlayerSaveable { get; }
    public abstract void CaptureState(GameSaveData save, ulong clientId = 0);
    public abstract void RestoreState(GameSaveData save, ulong clientId = 0);

    public override void OnNetworkSpawn()
    {
        RegisterWithSaveLoad();
        if (IsServer)
            NetworkManager.Singleton.SceneManager.OnLoadComplete += OnSceneLoadComplete;
    }

    public override void OnNetworkDespawn()
    {
        SaveLoadManager.Instance?.Unregister(this);
        if (NetworkManager.Singleton?.SceneManager != null)
            NetworkManager.Singleton.SceneManager.OnLoadComplete -= OnSceneLoadComplete;
    }

    private void OnSceneLoadComplete(
        ulong clientId,
        string sceneName,
        UnityEngine.SceneManagement.LoadSceneMode loadSceneMode)
    {
        RegisterWithSaveLoad();
    }

    private void RegisterWithSaveLoad()
    {
        if (IsServer)
            SaveLoadManager.Instance?.Register(this);
    }
}
