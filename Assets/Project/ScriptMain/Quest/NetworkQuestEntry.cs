using Unity.Netcode;

public struct NetworkQuestEntry : INetworkSerializable, System.IEquatable<NetworkQuestEntry>
{
    public int QuestId;
    public QuestStatus Status;
    public int Progress;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref QuestId);
        serializer.SerializeValue(ref Status);
        serializer.SerializeValue(ref Progress);
    }

    public bool Equals(NetworkQuestEntry other) =>
        QuestId == other.QuestId && Status == other.Status && Progress == other.Progress;
}
