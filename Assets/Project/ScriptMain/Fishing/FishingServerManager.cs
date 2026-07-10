using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Scene-placed NetworkBehaviour (NOT on the player prefab).
/// Owns the server-side fishing state machine for every player simultaneously.
///
/// Player lookup: uses NetworkManager.ConnectedClients[clientId].PlayerObject
/// to reach the specific player's components by client ID.
///
/// Flow:
///   PlayerItemUser.OnActionAnimationFinished  →  StartFishingServerRpc
///   FishBiteTimer fires                       →  FishingPhase.FishBiting + OpenMinigameRpc
///   Client completes mini-game               →  SubmitFishingResultServerRpc
///   Success                                  →  fish to inventory + FishingPhase.Pulling
///   Pull animation ends (timer)              →  FishingPhase.None
/// </summary>
public class FishingServerManager : NetworkBehaviour
{
    public static FishingServerManager Instance { get; private set; }

    [Header("Timing")]
    [SerializeField] private float minBiteTime      = 3f;
    [SerializeField] private float maxBiteTime      = 10f;
    [SerializeField] private float pullAnimDuration = 3.5f; // match your Animator clip length

    [Header("Catchable Fish")]
    [Tooltip("FishSO assets the server can randomly select when a fish bites. " +
             "Assign these in the Inspector.")]
    [SerializeField] private FishSO[] catchableFish;

    [Header("Mini-game Feel")]
    [Tooltip("Rightward pull strength sent to FishingMiniGameUI while the player holds click.\n" +
             "TODO: replace with a per-player lookup from the Knowledge stat.")]
    [SerializeField] private float defaultPullStrength = 600f;

    // Server-only: one session per fishing player
    private readonly Dictionary<ulong, FishingSession> _sessions = new();

    private class FishingSession
    {
        public FishingPhase Phase;
        public Coroutine     TimerCoroutine;
        public FishSO        CaughtFish;   // chosen when the fish bites
    }

    // ── Unity lifecycle ───────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ── Player lookup helpers (server only) ───────────────────────────────────

    /// <summary>Returns the PlayerController for a given client, or null.</summary>
    private PlayerController GetController(ulong clientId)
    {
        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
            return null;
        return client.PlayerObject?.GetComponent<PlayerController>();
    }

    /// <summary>
    /// Writes FishingPhase to the target player's NetworkVariable (server write)
    /// and mirrors it in the local session record.
    /// </summary>
    private void SetPhase(ulong clientId, FishingPhase phase)
    {
        if (_sessions.TryGetValue(clientId, out var session))
            session.Phase = phase;

        var controller = GetController(clientId);
        if (controller != null)
            controller.CurrentFishingPhase.Value = phase;
    }

    // ── Entry point ───────────────────────────────────────────────────────────

    /// <summary>
    /// Called by PlayerItemUser on the owner client right after the cast animation
    /// finishes (OnActionAnimationFinished detects FishingRodSO).
    /// </summary>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void StartFishingServerRpc(RpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;

        if (_sessions.ContainsKey(clientId))
        {
            Debug.LogWarning($"[FishingServerManager] Client {clientId} already has an active session.");
            return;
        }

        var session = new FishingSession { Phase = FishingPhase.WaitingForBite };
        _sessions[clientId] = session;

        SetPhase(clientId, FishingPhase.WaitingForBite);

        session.TimerCoroutine = StartCoroutine(FishBiteTimer(clientId));
        Debug.Log($"[FishingServerManager] Client {clientId} cast bait. Fish bites in {minBiteTime}–{maxBiteTime}s.");
    }

    // ── State machine coroutines ──────────────────────────────────────────────

    private IEnumerator FishBiteTimer(ulong clientId)
    {
        yield return new WaitForSeconds(Random.Range(minBiteTime, maxBiteTime));

        if (!_sessions.TryGetValue(clientId, out var session)) yield break;

        // Choose a random fish
        if (catchableFish == null || catchableFish.Length == 0)
        {
            Debug.LogWarning("[FishingServerManager] No FishSO assets assigned in Inspector.");
            EndFishing(clientId);
            yield break;
        }

        session.CaughtFish = catchableFish[Random.Range(0, catchableFish.Length)];

        // FishBiting: speed up idle animation on all clients, open mini-game for owner
        SetPhase(clientId, FishingPhase.FishBiting);

        // Send fish configuration to the owner's mini-game UI
        var fish = session.CaughtFish;
        OpenFishingMinigameRpc(
            fish.fishMoveSpeed,
            fish.moveUncertainty,
            fish.catchTimeRequired,
            defaultPullStrength,
            RpcTarget.Single(clientId, RpcTargetUse.Temp));

        Debug.Log($"[FishingServerManager] Fish biting for client {clientId} — fish: {fish.animalName}.");
    }

    private IEnumerator EndFishingAfterPull(ulong clientId)
    {
        yield return new WaitForSeconds(pullAnimDuration);
        EndFishing(clientId);
    }

    private void EndFishing(ulong clientId)
    {
        _sessions.Remove(clientId);
        SetPhase(clientId, FishingPhase.None);
        Debug.Log($"[FishingServerManager] Session ended for client {clientId}.");
    }

    // ── Mini-game result (client → server) ────────────────────────────────────

    /// <summary>
    /// Called by FishingMinigameUI when the player finishes the mini-game.
    /// <paramref name="success"/> true = fish caught, false = fish escaped.
    /// </summary>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void SubmitFishingResultServerRpc(bool success, RpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;

        if (!_sessions.TryGetValue(clientId, out var session))
        {
            Debug.LogWarning($"[FishingServerManager] Result from client {clientId} — no active session.");
            return;
        }

        // Cancel the bite timer if the player somehow submits while it's still running
        if (session.TimerCoroutine != null)
            StopCoroutine(session.TimerCoroutine);

        if (success)
        {
            GiveFishToPlayer(clientId, session.CaughtFish);
            SetPhase(clientId, FishingPhase.Pulling);
            StartCoroutine(EndFishingAfterPull(clientId));
            Debug.Log($"[FishingServerManager] Client {clientId} caught {session.CaughtFish?.animalName}!");
        }
        else
        {
            Debug.Log($"[FishingServerManager] Client {clientId} failed the mini-game. Fish escaped.");
            EndFishing(clientId);
        }
    }

    // ── Inventory ─────────────────────────────────────────────────────────────

    private void GiveFishToPlayer(ulong clientId, FishSO fish)
    {
        if (fish == null || fish.dropItem == null)
        {
            Debug.LogWarning($"[FishingServerManager] FishSO or its dropItem is null — nothing added to inventory.");
            return;
        }

        var inventories = InventoryDataRegistry.GetAllByOwner(clientId);
        if (inventories == null || inventories.Count == 0)
        {
            Debug.LogWarning($"[FishingServerManager] No inventories found for client {clientId}.");
            return;
        }

        // Add to the first available inventory (same approach as FarmingServerManager harvest)
        InventoryData inventory = InventoryDataRegistry.GetByOwnerAndID(clientId, 0);
        inventory.AddItem(fish.dropItem.itemID, 1);
        Debug.Log($"[FishingServerManager] Added '{fish.animalName}' (itemID {fish.dropItem.itemID}) to client {clientId}'s inventory.");
    }

    // ── Targeted ClientRpc — owner only ──────────────────────────────────────

    /// <summary>
    /// Fires only on the fishing player's machine when a fish bites.
    /// Passes the fish configuration so the client can set up the mini-game
    /// without needing a direct reference to the FishSO asset.
    ///
    /// Called with RpcTarget.Single so only the owner receives it.
    /// </summary>
    [Rpc(SendTo.ClientsAndHost, AllowTargetOverride = true)]
    private void OpenFishingMinigameRpc(
        float fishMoveSpeed,
        float moveUncertainty,
        float catchTimeRequired,
        float pullStrength,
        RpcParams rpcParams = default)
    {
        // InGameUIManager owns panel visibility and the critical-panel flag.
        // It will call FishingMiniGameUI.Open() internally after fading the panel in.
        InGameUIManager.Instance?.OpenFishingPanel(fishMoveSpeed, moveUncertainty, catchTimeRequired, pullStrength);
    }

    // ── Player-initiated cancel (swap tool, press Cancel key, etc.) ───────────

    /// <summary>
    /// Call from the owner client to abort fishing early.
    /// PlayerHeldItem.SetHeld() should trigger this when the item changes away from FishingRodSO.
    /// </summary>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void CancelFishingServerRpc(RpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        if (!_sessions.TryGetValue(clientId, out var session)) return;

        if (session.TimerCoroutine != null)
            StopCoroutine(session.TimerCoroutine);

        EndFishing(clientId);
        Debug.Log($"[FishingServerManager] Client {clientId} cancelled fishing.");
    }
}
