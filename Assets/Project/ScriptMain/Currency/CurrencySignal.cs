using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Signals/CurrencySignal")]
public class CurrencySignal : ScriptableObject
{
    public event Action<long> OnGoldChanged;
    public long CurrentGold { get; private set; }

    public void UpdateGold(long amount)
    {
        CurrentGold = amount;
        OnGoldChanged?.Invoke(amount);
    }
}
