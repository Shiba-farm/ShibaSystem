using Unity.Netcode;

public struct NetworkDiscoveryEntry : INetworkSerializable, System.IEquatable<NetworkDiscoveryEntry>
{
    public int CollectibleId;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref CollectibleId);
    }

    public bool Equals(NetworkDiscoveryEntry other) => CollectibleId == other.CollectibleId;
}
