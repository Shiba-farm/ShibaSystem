using Unity.Netcode;

public struct NetworkSkillEntry : INetworkSerializable, System.IEquatable<NetworkSkillEntry>
{
    public int SkillId;
    public int Level; // 0 = ยังไม่ปลดล็อก (entry นี้จะไม่ถูกเก็บถ้า Level == 0 ดู SkillManager)

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref SkillId);
        serializer.SerializeValue(ref Level);
    }

    public bool Equals(NetworkSkillEntry other) => SkillId == other.SkillId && Level == other.Level;
}
