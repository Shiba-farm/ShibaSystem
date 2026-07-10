using System.Collections.Generic;
using UnityEngine;

/// <summary>รวมเควสทั้งหมดในเกม — รูปแบบเดียวกับ ItemDatabases ทุกจุด</summary>
[CreateAssetMenu(menuName = "ShibaFarm/Quest/Quest Database")]
public class QuestDatabaseSO : ScriptableObject
{
    [SerializeField] private List<QuestDefinitionSO> allQuests = new();
    private Dictionary<int, QuestDefinitionSO> _lookup = new();
    private bool _initialized;

    public IReadOnlyList<QuestDefinitionSO> AllQuests => allQuests;

    public void Initialize()
    {
        _lookup.Clear();
        foreach (var q in allQuests)
            if (q != null && !_lookup.ContainsKey(q.questId))
                _lookup.Add(q.questId, q);
        _initialized = true;
    }

    public QuestDefinitionSO GetByID(int id)
    {
        if (!_initialized || _lookup.Count == 0) Initialize();
        return _lookup.TryGetValue(id, out var q) ? q : null;
    }
}
