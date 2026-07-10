using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Signals/SkillDataSignal")]
public class SkillDataSignal : ScriptableObject
{
    public event Action<SkillManager> OnDataUpdate;
    public SkillManager CurrentData { get; private set; }

    public void UpdateSkillData(SkillManager data)
    {
        CurrentData = data;
        OnDataUpdate?.Invoke(data);
    }
}
