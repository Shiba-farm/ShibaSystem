using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Signals/HeldItemSignal")]
public class HeldItemSignal : ScriptableObject
{
    public event Action<ItemSO> OnChanged;
    public ItemSO Current { get; private set; }

    public void Set(ItemSO item)
    {
        Current = item;
        OnChanged?.Invoke(item);
    }
}
