using Unity.Netcode;
using UnityEngine;

public class CurrencyData : NetworkBehaviour
{
    [SerializeField] private CurrencySignal currencySignal;
    private NetworkVariable<long> gold = new NetworkVariable<long>(
        0,
        NetworkVariableReadPermission.Owner,  // only owner sees their balance
        NetworkVariableWritePermission.Server  // only server can change it
    );

    public long Gold => gold.Value;
    public override void OnNetworkSpawn()
    {
        gold.OnValueChanged += OnGoldChanged;

        currencySignal.UpdateGold(gold.Value);
    }
    public override void OnNetworkDespawn()
    {
        gold.OnValueChanged -= OnGoldChanged;
    }
    private void OnGoldChanged(long previousValue, long newValue)
    {
        currencySignal.UpdateGold(newValue);
    }
    public void AddCurrency(long amount)
    {
        if (!IsServer) return;
        gold.Value += amount;
    }
    public void ReduceCurrency(long amount)
    {
        if (!IsServer) return;
        gold.Value -= amount;
    }
    public void SetCurrency(long amount)
    {
        if (!IsServer) return;
        gold.Value = amount;
    }

    public bool TrySpendCurrency(long amount)
    {
        if (!IsServer) return false;
        if (gold.Value < amount) return false;

        gold.Value -= amount;
        return true;
    }
}
