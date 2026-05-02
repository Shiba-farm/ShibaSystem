using Unity.Netcode;
using UnityEngine;

[System.Serializable]
public struct NetworkKnowledgeStat : INetworkSerializable, System.IEquatable<NetworkKnowledgeStat>
{
    public RecipeCategory Category;
    public int Level;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter {
        serializer.SerializeValue(ref Category);
        serializer.SerializeValue(ref Level);
    }
    
    public bool Equals(NetworkKnowledgeStat other) => Category == other.Category && Level == other.Level;
}
