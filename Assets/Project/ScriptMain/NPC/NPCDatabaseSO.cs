using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "ShibaFarm/NPC/NPC Database")]
public class NPCDatabaseSO : ScriptableObject
{
    [SerializeField] private List<NPCDefinitionSO> allNpcs = new();
    private Dictionary<int, NPCDefinitionSO> _lookup = new();
    private bool _initialized;

    public IReadOnlyList<NPCDefinitionSO> AllNpcs => allNpcs;

    public void Initialize()
    {
        _lookup.Clear();
        foreach (var npc in allNpcs)
            if (npc != null && !_lookup.ContainsKey(npc.npcId))
                _lookup.Add(npc.npcId, npc);
        _initialized = true;
    }

    public NPCDefinitionSO GetByID(int id)
    {
        if (!_initialized || _lookup.Count == 0) Initialize();
        return _lookup.TryGetValue(id, out var npc) ? npc : null;
    }
}
