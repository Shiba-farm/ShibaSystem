/// <summary>
/// Server-authoritative fishing state, replicated to all clients via
/// PlayerController.FishingPhase (NetworkVariable).
/// PlayerHeldItem subscribes to it and drives the correct animation trigger
/// from FishingRodSO whenever the phase changes.
/// </summary>
public enum FishingPhase
{
    None,           // rod is held but not in use
    WaitingForBite, // bait is in the water — FishingIdle animation plays
    FishBiting,     // a fish is on the hook — idle anim speed up, mini-game opens
    Pulling,        // mini-game won — pull animation plays, fish goes to inventory
}
