using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "ShibaFarm/Skill/Skill Database")]
public class SkillDatabaseSO : ScriptableObject
{
    [SerializeField] private List<SkillDefinitionSO> allSkills = new();
    private Dictionary<int, SkillDefinitionSO> _lookup = new();
    private bool _initialized;

    public IReadOnlyList<SkillDefinitionSO> AllSkills => allSkills;

    public void Initialize()
    {
        _lookup.Clear();
        foreach (var s in allSkills)
            if (s != null && !_lookup.ContainsKey(s.skillId))
                _lookup.Add(s.skillId, s);
        _initialized = true;
    }

    public SkillDefinitionSO GetByID(int id)
    {
        if (!_initialized || _lookup.Count == 0) Initialize();
        return _lookup.TryGetValue(id, out var s) ? s : null;
    }

    public IEnumerable<SkillDefinitionSO> GetByCategory(SkillCategory category)
    {
        foreach (var s in allSkills)
            if (s != null && s.category == category) yield return s;
    }
}
