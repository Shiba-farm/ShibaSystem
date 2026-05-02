using Unity.Collections;
using Unity.Netcode;

public class NetworkItem : INetworkSerializable, System.IEquatable<NetworkItem>
{
    public FixedString32Bytes ItemID;
    public int Quantity;
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ItemID);
        serializer.SerializeValue(ref Quantity);
    }

    public bool Equals(NetworkItem other) => ItemID == other.ItemID && Quantity == other.Quantity;
}
