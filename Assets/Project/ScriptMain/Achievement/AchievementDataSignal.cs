using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Signals/AchievementDataSignal")]
public class AchievementDataSignal : ScriptableObject
{
    public event Action<AchievementManager> OnDataUpdate;
    public AchievementManager CurrentData { get; private set; }

    public void UpdateAchievementData(AchievementManager data)
    {
        CurrentData = data;
        OnDataUpdate?.Invoke(data);
    }
}
