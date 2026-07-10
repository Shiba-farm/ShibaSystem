using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Signals/RelationshipDataSignal")]
public class RelationshipDataSignal : ScriptableObject
{
    public event Action<RelationshipManager> OnDataUpdate;
    public RelationshipManager CurrentData { get; private set; }

    public void UpdateRelationshipData(RelationshipManager data)
    {
        CurrentData = data;
        OnDataUpdate?.Invoke(data);
    }
}
