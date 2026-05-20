// ToolItemSO.cs
using UnityEngine;

[CreateAssetMenu(menuName = "Items/Tool")]
public class ToolItemSO : ItemSO, IUsable
{
    [Header("Tool")]
    public float energyCost;
    public float staminaCost;
    public float hitRange = 2f;
    public int damage = 10;
    public ToolAction toolTypeAction;
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