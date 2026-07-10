using UnityEngine;

/// <summary>
/// Fishing rod is a multi-phase tool.
///
/// The base ToolItemSO.animationTriggerName acts as the CAST trigger
/// (set it to "FishingStart" in the Inspector).  PlayerItemUser fires it
/// via IUsable.AnimationHash on the frame the player presses Use — no
/// changes to PlayerItemUser's TryUse() path are needed for the cast.
///
/// The two additional triggers below are owned by FishingServerManager,
/// which writes to PlayerController.FishingPhase (NetworkVariable).
/// PlayerHeldItem reads that variable and sets these triggers on every client
/// so every player in the session sees the correct fishing animation.
/// </summary>
[CreateAssetMenu(menuName = "Items/Fishing Rod")]
public class FishingRodSO : ToolItemSO
{
    [Header("Fishing Phase Animations")]
    [Tooltip("Trigger played after a successful mini-game (FishingPhase.Pulling).")]
    [SerializeField] private string pullTriggerName = "FishingPull";

    // The fishing IDLE state is kept alive by the bool parameter "IsFishingIdle"
    // in the Animator (set by PlayerHeldItem) — no trigger needed here because a
    // trigger is consumed in one frame and the Animator exits the state.
    // PullAnimHash stays as a trigger because it's a one-shot animation.
    public int PullAnimHash => Animator.StringToHash(pullTriggerName);
}
