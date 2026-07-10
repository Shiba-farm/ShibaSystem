using UnityEngine;

/// <summary>
/// Base class for consumable helper items that are used in-world via the action system
/// (aimed at a terrain cell, a player, or an animal) but are NOT tools.
///
/// Subclass this for specific effect categories:
///   • FertilizerItemSO  — boosts crop growth / yield on a tilled cell
///   • StatBoostItemSO   — temporarily buffs player stats
///   • FeedItemSO        — increases livestock productivity
///   • etc.
///
/// Usage path:
///   Player holds item → presses Use → PlayerItemUser.TryUse() detects IUsable
///   → fires the animation → OnActionImpact() → server validates + applies effect
///
/// For now the base class provides the IUsable contract and a flexible
/// EffectType field so the server can switch on what to do.
/// </summary>
[CreateAssetMenu(menuName = "Items/FarmHelper")]
public class FarmHelperItemSO : ItemSO, IUsable
{
    // ── Usage cost ────────────────────────────────────────────────────────────

    [Header("Usage")]
    public float energyCost;
    public float staminaCost;
    [SerializeField] private string animationTriggerName;

    public float EnergyCost  => energyCost;
    public float StaminaCost => staminaCost;
    public int   AnimationHash => Animator.StringToHash(animationTriggerName);

    // ── Effect descriptor ─────────────────────────────────────────────────────

    [Header("Effect")]
    [Tooltip("What this helper item does when used. The server reads this to decide which effect to apply.")]
    public FarmHelperEffect effectType;

    [Tooltip("Magnitude of the effect. Meaning depends on effectType:\n" +
             "  Fertilizer  → bonus days of growth skipped (1 = skip 1 day)\n" +
             "  StatBoost   → amount added to the target stat\n" +
             "  FeedBoost   → production multiplier bonus (e.g. 0.5 = +50 %)")]
    public float effectValue = 1f;

    [Tooltip("How many in-game days the effect lasts (0 = instant / one-shot).")]
    public int effectDuration = 0;

    // ── IUsable ───────────────────────────────────────────────────────────────

    public bool CanUse(StatManager user)
    {
        foreach (var stat in user.AllStats)
            if (stat.Type == StatType.Stamina)
                return stat.CurrentValue >= staminaCost;
        return false;
    }
}

// ── Effect type enum ──────────────────────────────────────────────────────────

/// <summary>
/// Identifies what a <see cref="FarmHelperItemSO"/> does when applied.
/// Add new values here as new helper categories are introduced.
/// </summary>
public enum FarmHelperEffect
{
    Fertilizer,      // Applied to a farm tile — accelerates crop growth / boosts yield
    StatBoost,       // Applied to the player — temporarily raises a stat
    LivestockFeed,   // Applied to a livestock animal — boosts production output
}
