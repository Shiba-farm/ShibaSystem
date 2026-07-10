using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Signals/QuestDataSignal")]
public class QuestDataSignal : ScriptableObject
{
    public event Action<QuestManager> OnDataUpdate;
    public QuestManager CurrentData { get; private set; }

    public void UpdateQuestData(QuestManager data)
    {
        CurrentData = data;
        OnDataUpdate?.Invoke(data);
    }
}
