using UnityEngine;

[CreateAssetMenu(menuName = "Items/Food")]
public class FoodItemSO : ItemSO, IUsable
{
    [Header("Food")]
    public float energyRestore;
    public float healthRestore;

    public float energyCost;
    public float staminaCost;
    [SerializeField] private string animationTriggerName;

    public float EnergyCost  => energyCost;
    public float StaminaCost => staminaCost;
    public int AnimationHash => Animator.StringToHash(animationTriggerName);

    public bool CanUse(StatManager user)
    {
        foreach (var stat in user.AllStats)
            if (stat.Type == StatType.Stamina)
                return stat.CurrentValue >= energyCost;
        return false;
    }
}
