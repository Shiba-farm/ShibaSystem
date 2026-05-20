using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class RockObject : DestructibleObject
{
    [System.Serializable]
    public class OreDrop
    {
        public int itemID;
        public int minAmount;
        public int maxAmount;
    }

    [Header("Rock Settings")]
    [SerializeField] private List<OreDrop> drops;
    [SerializeField] private GameObject destroyVFX;
    [SerializeField] private AudioClip destroySFX;

    protected override bool CanBeDamagedBy(ToolAction tool)
        => tool == ToolAction.Mine;   // only pickaxe can mine

    protected override void OnDepleted()
    {
        // spawn all drop types
        foreach (var drop in drops)
        {
            int amount = Random.Range(drop.minAmount, drop.maxAmount + 1);
            NetworkItemSpawner.Instance.SpawnItem(
                drop.itemID, amount, transform.position);
        }

        GetComponent<NetworkObject>().Despawn(false);
        gameObject.SetActive(false);
    }
}
