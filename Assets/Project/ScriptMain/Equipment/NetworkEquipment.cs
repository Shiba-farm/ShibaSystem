using Unity.Netcode;

public struct NetworkEquipment : INetworkSerializable, System.IEquatable<NetworkEquipment>
{
    public EquipSlot Slot;
    public int ItemID; // 0 = ช่องนี้ว่าง

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Slot);
        serializer.SerializeValue(ref ItemID);
    }

    public bool Equals(NetworkEquipment other) => Slot == other.Slot && ItemID == other.ItemID;
}
