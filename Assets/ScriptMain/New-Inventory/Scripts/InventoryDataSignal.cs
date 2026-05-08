using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Signals/InventoryDataSignal")]
public class InventoryDataSignal : ScriptableObject
{
    public event Action<InventoryData> OnDataUpdate;
    public InventoryData CurrentData { get; private set; }

    public void UpdateInventoryData(InventoryData data)
    {
        CurrentData = data;
        OnDataUpdate?.Invoke(data);
    }
}
