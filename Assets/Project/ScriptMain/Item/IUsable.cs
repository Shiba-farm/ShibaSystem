using UnityEngine;

public interface IUsable
{
    bool CanUse(StatManager user);
    float EnergyCost { get; }       // PlayerItemUser reads this
    float StaminaCost {get;}
    int AnimationHash { get; }      // PlayerItemUser reads this
}
