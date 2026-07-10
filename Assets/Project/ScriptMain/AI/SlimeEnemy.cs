using Unity.Netcode;
using UnityEngine;
using MyGame.Dungeon;


public class SlimeEnemy : DestructibleObject
{
    [Header("Slime Settings")]
    [SerializeField] private int expDrop    = 10;
    [SerializeField] private int itemDropID = 0;    // slime drop item ID
    [SerializeField] private int itemAmount = 1;

    [Header("References")]
    [SerializeField] private Animator animator;

    private static readonly int DeathHash = Animator.StringToHash("Death");

    // Slime can be damaged by any tool or weapon — no tool restriction
    protected override bool CanBeDamagedBy(ToolAction tool) => true;

    protected override void OnHealthChanged(int prev, int next)
    {
        // Visual hit feedback on all clients
        // animator?.SetTrigger("Hit");
        Debug.Log($"Slime health changed: {prev} -> {next}");
    }

    protected override void OnDepleted()
    {
        if (!IsServer) return;

        // Notify DungeonManager this enemy is dead — resolve which player's
        // personal dungeon instance (slot + owner) this slime belongs to via
        // the DungeonInstanceMember tag added at spawn time (Phase B).
        var member = GetComponent<DungeonInstanceMember>();
        if (member != null)
        {
            var gridPos = DungeonManager.Instance?.WorldToGrid(transform.position, member.slot);
            if (gridPos.HasValue)
                DungeonManager.Instance?.OnEnemyKilled(gridPos.Value, member.ownerClientId);
        }

        // Drop loot
        if (itemDropID > 0)
            NetworkItemSpawner.Instance?.SpawnItem(
                itemDropID, itemAmount, transform.position);

        // Play death then despawn
        StartCoroutine(DieCoroutine());
    }

    private System.Collections.IEnumerator DieCoroutine()
    {
        // Disable NavMeshAgent and BehaviorAgent so slime stops moving
        var navAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        var behaviorAgent = GetComponent<Unity.Behavior.BehaviorGraphAgent>();

        if (navAgent != null)      navAgent.enabled      = false;
        if (behaviorAgent != null) behaviorAgent.enabled = false;

        // Play death animation
        PlayDeathAnimationClientRpc();

        // Wait for animation to finish
        yield return new WaitForSeconds(1f);

        GetComponent<NetworkObject>().Despawn();
    }

    [Rpc(SendTo.Everyone)]
    private void PlayDeathAnimationClientRpc()
    {
        Debug.Log("Playing slime death animation on client");
        animator?.SetTrigger(DeathHash);
    }
}
