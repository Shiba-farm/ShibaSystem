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
        {
            long pending = GameDataManager.Instance?.PendingGold ?? -1L;
            if (pending >= 0L)
            {
                currencyStorage.SetCurrency(pending);
                GameDataManager.Instance.ClearGoldTransition();
                Debug.Log($"[CurrencyManager] Restored gold from scene transition: {pending}");
            }
            SaveLoadManager.Instance?.Register(this);
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        if (IsServer)
            SaveLoadManager.Instance?.Unregister(this);
    }

    /// <summary>เรียกจาก server-side code โดยตรง (เช่น QuestManager.GrantRewards)</summary>
    public void AddCurrencyOnServer(long amount)
    {
        if (!IsServer) return;
        currencyStorage.AddCurrency(amount);
        Debug.Log($"[CurrencyManager] AddCurrencyOnServer: +{amount} → {currencyStorage.Gold}");
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
