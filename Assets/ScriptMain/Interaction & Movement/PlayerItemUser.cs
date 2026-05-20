using Unity.Netcode;
using UnityEngine;

public class PlayerItemUser : NetworkBehaviour
{
    [SerializeField] private PlayerHeldItem heldItem;
    [SerializeField] private StatManager stats;
    [SerializeField] private Animator animator;
    [SerializeField] private LayerMask interactMask;

    private bool _isActing = false;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;
        InputHandler.Singleton.OnUseTool += TryUse;
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner) return;
        InputHandler.Singleton.OnUseTool -= TryUse;
    }

    private void TryUse()
    {
        if (_isActing) return;
        // Debug.Log("Using a tool");
        if (!IsOwner) return;
        var data = heldItem.Current;
        // Debug.Log($"Hey I want to use this {heldItem.Current}");

        if (data is not IUsable usable) return;
        if (!usable.CanUse(stats)) return;

        _isActing = true;                          // ← uncomment this
        InputHandler.Singleton.InputLocked = true;
        animator.ResetTrigger(usable.AnimationHash);
        UseItemServerRpc(data.itemID);
        animator.SetTrigger(usable.AnimationHash);

        heldItem.SetHoldState(HoldState.Acting);

        // server handles all game state changes
    }

    public void OnActionImpact()
    {
        if (!IsOwner) return;
        var data = heldItem.Current;
        if (data is not ToolItemSO) return;

        // this is the perfect moment to do the actual hit
        // animation has reached the impact frame
        HitWorldServerRpc(data.itemID);
    }

    public void OnActionAnimationFinished()
    {
        // Debug.Log("Set Back");
        _isActing = false;    // ← fired by animation event at last frame
        InputHandler.Singleton.InputLocked = false;
        heldItem.SetHoldState(HoldState.Idle);
    }

    // public void OnActionAnimationFinished()
    // {
    //     _isActing = false;    // ← fired by animation event at last frame
    //     heldItem.SetHoldState(HoldState.Idle);
    // }

    [ServerRpc]
    private void UseItemServerRpc(int itemID)
    {
        var data = GameDataManager.Instance.itemDatabases.GetItemByID(itemID);
        if (data is not IUsable usable) return;

        // server validates again
        if (!usable.CanUse(stats)) return;

        // consume energy — all usable items
        stats.ConsumeStat(StatType.Stamina, usable.StaminaCost);
        stats.ConsumeStat(StatType.Energy, usable.EnergyCost);

        // food specific
        if (data is FoodItemSO food)
            HealPlayer(food);
    }

    [ServerRpc]
    private void HitWorldServerRpc(int itemID)
    {
        var data = GameDataManager.Instance.itemDatabases.GetItemByID(itemID);
        if (data is not ToolItemSO tool) return;
        HitWorld(tool);
    }

    private void HitWorld(ToolItemSO tool)
    {
        var hits = Physics.OverlapSphere(
            transform.position + transform.forward,
            tool.hitRange, interactMask);

        // Debug.Log("Try to hit");

        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<IDamageable>(out var damageable))
            {
                Debug.Log("Get the damanageable component");
                damageable.TakeDamage(tool.damage, tool.toolTypeAction);
                break;
            }
        }
    }

    private void HealPlayer(FoodItemSO food)
    {
        stats.RegenStat(StatType.Health, food.healthRestore);
        stats.RegenStat(StatType.Energy, food.energyRestore);
    }
}