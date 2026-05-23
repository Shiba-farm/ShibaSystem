using Unity.Netcode;
using UnityEngine;

public class TreeObject : DestructibleObject
{
    [SerializeField] private int woodItemID;
    [SerializeField] private int woodAmount = 3;
    [SerializeField] private float fallDuration = 1.5f; // match your Tree fall clip length

    private Animator _animator;
    private static readonly int DepletedHash = Animator.StringToHash("Depleted");

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        _animator = GetComponent<Animator>();
        Debug.Log($"Tree spawned with health: {currentHealth.Value}, animator found: {_animator != null}");
    }

    protected override void OnDepleted()
    {
        // Play fall animation on all clients before despawning
        PlayFallAnimationClientRpc();

        // Spawn loot immediately on server
        NetworkItemSpawner.Instance.SpawnItem(
            woodItemID, woodAmount, transform.position);

        // Despawn after animation finishes
        StartCoroutine(DespawnAfterFall());
    }

    [ClientRpc]
    private void PlayFallAnimationClientRpc()
    {
        _animator?.SetBool(DepletedHash, true);
    }

    private System.Collections.IEnumerator DespawnAfterFall()
    {
        yield return new WaitForSeconds(fallDuration);
        GetComponent<NetworkObject>().Despawn();
    }
}
