using Unity.Netcode;
using UnityEngine;

public abstract class DestructibleObject : NetworkBehaviour, IDamageable
{
    [SerializeField] protected int maxHealth = 100;
    [SerializeField] protected ToolAction requiredTool = ToolAction.None;

    protected NetworkVariable<int> currentHealth = new(
        writePerm: NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        if (IsServer)
            currentHealth.Value = maxHealth;

        // all clients listen for health changes for VFX/SFX
        currentHealth.OnValueChanged += OnHealthChanged;
    }

    public override void OnNetworkDespawn()
    {
        currentHealth.OnValueChanged -= OnHealthChanged;
    }

    // IDamageable — only server calls this (from HitWorldServerRpc)
    public void TakeDamage(int amount, ToolAction toolType)
    {
        if (!IsServer) return;
        if (!CanBeDamagedBy(toolType)) return;

        currentHealth.Value -= amount;

        // Debug.Log($"Take the damage : {amount}");
        
        if (currentHealth.Value <= 0)
            OnDepleted();
    }

    protected virtual bool CanBeDamagedBy(ToolAction tool)
        => tool == requiredTool;

    protected virtual void OnHealthChanged(int prev, int next) { }

    // server only — handle drops and despawn
    protected abstract void OnDepleted();
}
