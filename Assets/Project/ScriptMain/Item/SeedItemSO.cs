using UnityEngine;

/// <summary>
/// One growth stage of a planted crop.
/// Keeps the visual prefab and its timing locked together so they can never fall out of sync.
/// </summary>
[System.Serializable]
public struct GrowthStage
{
    [Tooltip("Prefab that becomes visible while in this stage. Must have a CropVisual component on the root.")]
    public GameObject visualPrefab;

    [Tooltip("How many watered days must pass at this stage before the crop advances to the next one.")]
    public int daysToGrow;

    [Tooltip("Does this stage require at least one watering before it can advance?")]
    public bool requiresWater;
}

/// <summary>
/// Item data for a seed.
/// Owns all growth and harvest data directly — no separate CropSO needed.
/// </summary>
[CreateAssetMenu(menuName = "Items/Seed")]
public class SeedItemSO : ItemSO, IUsable
{

    [Header("Usage")]
    public float energyCost;
    public float staminaCost;
    [SerializeField] private string animationTriggerName;

    public float EnergyCost  => energyCost;
    public float StaminaCost => staminaCost;
    public int   AnimationHash => Animator.StringToHash(animationTriggerName);

    // ── Growth ────────────────────────────────────────────────────────────────

    [Header("Growth")]
    [Tooltip("Each entry is one stage: the visual to show and how many watered days it lasts.")]
    public GrowthStage[] stages;

    [Tooltip("Starting hydration level. Also the maximum — watering can never exceed this.\n" +
             "Each unwatered day reduces hydration by 1; reaching 0 kills the crop.\n" +
             "1 = must water every day   |   3 = survives 3 dry days   |   20 = very drought-tolerant.")]
    public int droughtTolerance = 3;

    // ── Harvest ───────────────────────────────────────────────────────────────

    [Header("Harvest")]
    public ItemSO       harvestItem;
    public Vector2Int   yieldRange       = new Vector2Int(1, 1);
    public bool         destroyOnHarvest = true;

    // ── IUsable ───────────────────────────────────────────────────────────────

    public bool CanUse(StatManager user)
    {
        foreach (var stat in user.AllStats)
            if (stat.Type == StatType.Stamina)
                return stat.CurrentValue >= staminaCost;
        return false;
    }
}
