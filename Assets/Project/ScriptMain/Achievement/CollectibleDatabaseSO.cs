using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "ShibaFarm/Achievement/Collectible Database")]
public class CollectibleDatabaseSO : ScriptableObject
{
    [SerializeField] private List<CollectibleDefinitionSO> allCollectibles = new();
    private Dictionary<int, CollectibleDefinitionSO> _byId = new();
    private Dictionary<int, CollectibleDefinitionSO> _byItemId = new();
    private bool _initialized;

    public IReadOnlyList<CollectibleDefinitionSO> AllCollectibles => allCollectibles;

    public void Initialize()
    {
        _byId.Clear();
        _byItemId.Clear();
        foreach (var c in allCollectibles)
        {
            if (c == null) continue;
            if (!_byId.ContainsKey(c.collectibleId)) _byId.Add(c.collectibleId, c);
            if (c.linkedItem != null && !_byItemId.ContainsKey(c.linkedItem.itemID)) _byItemId.Add(c.linkedItem.itemID, c);
        }
        _initialized = true;
    }

    public CollectibleDefinitionSO GetByID(int id)
    {
        if (!_initialized || _byId.Count == 0) Initialize();
        return _byId.TryGetValue(id, out var c) ? c : null;
    }

    public CollectibleDefinitionSO GetByLinkedItemID(int itemId)
    {
        if (!_initialized || _byId.Count == 0) Initialize();
        return _byItemId.TryGetValue(itemId, out var c) ? c : null;
    }

    public IEnumerable<CollectibleDefinitionSO> GetByCategory(CollectibleCategory category)
    {
        foreach (var c in allCollectibles)
            if (c != null && c.category == category) yield return c;
    }
}
