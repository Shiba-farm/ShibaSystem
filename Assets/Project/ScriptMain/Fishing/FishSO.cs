using UnityEngine;

/// <summary>
/// ScriptableObject for a specific fish species.
/// Inherits identity, loot, and base stats from AnimalSO.
/// Adds the mini-game configuration that drives FishingMiniGameUI.
///
/// Usage:
///   1. Create via Assets > Create > Animals > Fish
///   2. Assign the drop item (the ItemSO that lands in the player's inventory on catch)
///   3. Tune moveSpeed / moveUncertainty / catchTimeRequired for difficulty
///   4. Add to FishingServerManager.catchableFish array in the scene
/// </summary>
[CreateAssetMenu(menuName = "Animals/Fish")]
public class FishSO : AnimalSO
{
    [Header("Inventory Drop")]
    [Tooltip("ItemSO that is added to the player's inventory on a successful catch.")]
    public ItemSO dropItem;

    [Header("Mini-game — Fish Behaviour")]
    [Tooltip("How fast the fish icon moves horizontally across the bar (units per second).")]
    public float fishMoveSpeed = 120f;

    [Tooltip(
        "How erratically the fish changes direction.\n" +
        "0 = predictable back-and-forth.\n" +
        "1 = moderate randomness (recommended default).\n" +
        "2+ = very chaotic. Affects both frequency and size of direction changes.")]
    [Range(0f, 3f)]
    public float moveUncertainty = 1f;

    [Header("Mini-game — Catch Threshold")]
    [Tooltip("Total seconds the catch zone must overlap the fish to win.")]
    public float catchTimeRequired = 2f;
}
