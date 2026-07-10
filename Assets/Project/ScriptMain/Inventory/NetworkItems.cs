using System;
using Unity.Netcode;
using UnityEngine;
using Unity.Collections;

[Serializable]
public struct NetworkItems : INetworkSerializable, IEquatable<NetworkItems>
{
    
    public int ItemID;
    public int Amount;
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ItemID);
        serializer.SerializeValue(ref Amount);
    }

    public bool Equals(NetworkItems other)
    {
        return ItemID == other.ItemID && Amount == other.Amount;
    }
}
