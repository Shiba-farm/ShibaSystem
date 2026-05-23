using Unity.Netcode;
using UnityEngine;

public class CurrencyManager : NetworkSaveableBehaviour
{
    public static CurrencyManager Instance { get; private set; }
    public override bool IsPlayerSaveable => false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private CurrencyData currencyStorage;
    public long CurrentGold => currencyStorage.Gold;
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer)
            SaveLoadManager.Instance?.Register(this);
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        if (IsServer)
            SaveLoadManager.Instance?.Unregister(this);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void AddCurrencyServerRpc(long newAmount, RpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        currencyStorage.AddCurrency(newAmount);
        Debug.Log($"Client {clientId} updated their gold to {newAmount}");

    }
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void DeductCurrencyServerRpc(long newAmount, RpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        currencyStorage.ReduceCurrency(newAmount);
        Debug.Log($"Client {clientId} updated their gold to {newAmount}");

    }

    public override void CaptureState(GameSaveData save, ulong clientId = 0)
    {
        save.world.sharedGold = CurrentGold;
    }

    public override void RestoreState(GameSaveData save, ulong clientId = 0)
    {
        if (!IsServer) return;
        currencyStorage.SetCurrency(save.world.sharedGold);
    }
}
