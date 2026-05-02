using System;
using UnityEngine;

[CreateAssetMenu(fileName = "KnowledgeSignal", menuName = "Signals/KnowledgeDataSignal")]
public class KnowledgeStatDataSignal : ScriptableObject
{
    public event Action<StatManager> OnKnowledgeConnected;
    public StatManager CurrentManager { get; private set; }

    public void UpdateKnowledgeSource(StatManager manager)
    {
        CurrentManager = manager;
        OnKnowledgeConnected?.Invoke(manager);
    }
}
