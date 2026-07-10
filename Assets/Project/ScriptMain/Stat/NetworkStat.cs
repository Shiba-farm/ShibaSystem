using Unity.Netcode;
using UnityEngine;

public struct NetworkStat : INetworkSerializable, System.IEquatable<NetworkStat>
{
    public float CurrentValue;
    public float MaxValue;
    public StatType Type; 
    public bool Equals(NetworkStat other)
    {
        return CurrentValue == other.CurrentValue && 
               MaxValue == other.MaxValue && 
               Type == other.Type;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref CurrentValue);
        serializer.SerializeValue(ref MaxValue);
        serializer.SerializeValue(ref Type);
    }
}

public enum StatType { Health, Stamina, Energy, Oxygen, Mana }