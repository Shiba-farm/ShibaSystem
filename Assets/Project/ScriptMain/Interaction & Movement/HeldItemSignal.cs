using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Signals/HeldItemSignal")]
public class HeldItemSignal : ScriptableObject
{
    public event Action<ItemSO> OnChanged;
    public ItemSO Current { get; private set; }
    public int CurrentSlotIndex { get; private set; }  // ← add this

    public void Set(ItemSO item, int slotIndex)         // ← add slotIndex
    {
        Current = item;
        CurrentSlotIndex = slotIndex;
        OnChanged?.Invoke(item);
    }
}
