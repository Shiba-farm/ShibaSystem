using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Tracks which connected clients are currently "in bed" and fires the day-end
/// sequence once every player has slept.
///
/// Setup:
///   • Place as a scene NetworkObject (not spawned at runtime).
///   • No inspector wiring needed — all singletons are resolved at runtime.
///
/// Flow:
///   BedInteraction.Interact()
///     → RequestSleepServerRpc()
///         • All sleeping  → GameDataManager.AdvanceDay() (server-side)
///                         → AllPlayersSleptClientRpc() — opens Summary on all clients
///         • Not all yet   → ShowWaitingPanelClientRpc() — opens WaitingPanel for this client
///                         → UpdateSleepCountClientRpc() — refreshes "N/M sleeping" on all clients
///
///   WaitingPanel "Get out of bed" button
///     → CancelSleepServerRpc()
///         → HideWaitingPanelClientRpc() — closes WaitingPanel for this client
///         → UpdateSleepCountClientRpc() — refreshes count on all clients
///
/// Public read-only state (safe on all clients — kept in sync by ClientRpc):
///   SleepManager.Instance.SleepingCount   — number of players currently in bed
///   SleepManager.Instance.TotalCount      — total connected players
/// </summary>
public class SleepManager : NetworkBehaviour
{
    public static SleepManager Instance { get; private set; }

    // ── Public read-only state (synced to all clients via UpdateSleepCountClientRpc) ─

    /// <summary>How many players are currently in bed. Updated on all clients.</summary>
    public int SleepingCount { get; private set; }

    /// <summary>Total connected players at last count update.</summary>
    public int TotalCount    { get; private set; }

    // ── Server-only ───────────────────────────────────────────────────────────

    private readonly HashSet<ulong> _sleepingClients = new();

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;
    }

    public override void OnNetworkDespawn()
    {
        if (!IsServer) return;
        NetworkManager.OnClientDisconnectCallback -= OnClientDisconnected;
    }

    // ── Public RPC entry points ───────────────────────────────────────────────

    /// <summary>
    /// Called by BedInteraction when the local player climbs into bed.
    /// Runs on the server; the caller does NOT need to be the NetworkObject owner.
    /// </summary>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestSleepServerRpc(RpcParams serverRpcParams = default)
    {
        ulong senderId = serverRpcParams.Receive.SenderClientId;
        _sleepingClients.Add(senderId);

        int total    = NetworkManager.ConnectedClients.Count;
        int sleeping = _sleepingClients.Count;

        if (sleeping >= total)
        {
            // Capture the completed day BEFORE AdvanceDay() increments WorldTimeManager.
            // AllPlayersSleptClientRpc will pass it to every client so the Summary panel
            // shows the day that just ended (N) not the new morning (N+1).
            int completedDay = GameDataManager.Instance.CurrentDayNumber;
            _sleepingClients.Clear();
            GameDataManager.Instance.AdvanceDay();
            AllPlayersSleptClientRpc(completedDay);
        }
        else
        {
            // Still waiting on other players — tell this client to show the waiting panel
            // and refresh the sleep count display on every client.
            ShowWaitingPanelClientRpc(ToSingle(senderId));
            UpdateSleepCountClientRpc(sleeping, total);
        }
    }

    /// <summary>
    /// Called by the "Get out of bed" button in WaitingPanel.
    /// Removes this client from the sleeping set and hides their waiting panel.
    /// </summary>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void CancelSleepServerRpc(RpcParams serverRpcParams = default)
    {
        ulong senderId = serverRpcParams.Receive.SenderClientId;
        _sleepingClients.Remove(senderId);

        int total    = NetworkManager.ConnectedClients.Count;
        int sleeping = _sleepingClients.Count;

        HideWaitingPanelClientRpc(ToSingle(senderId));
        UpdateSleepCountClientRpc(sleeping, total);
    }

    // ── ClientRpcs ────────────────────────────────────────────────────────────

    /// <summary>
    /// Sent to ALL clients when every player is sleeping.
    /// Opens the day-end Summary (OpenExclusivePanel closes any other open panel,
    /// so WaitingPanel is dismissed automatically for players that were waiting).
    /// completedDay is the day number that just ended — set before AdvanceDay() runs
    /// so the Summary displays Day N (what happened today) rather than Day N+1 (tomorrow).
    /// </summary>
    [ClientRpc]
    private void AllPlayersSleptClientRpc(int completedDay)
    {
        GameDataManager.Instance.SetLastCompletedDay(completedDay);
        InGameUIManager.Instance.OpenExclusivePanel(InGamePanel.Summary);
    }

    /// <summary>Sent to a single client — opens their WaitingPanel.</summary>
    [ClientRpc]
    private void ShowWaitingPanelClientRpc(ClientRpcParams clientRpcParams = default)
    {
        InGameUIManager.Instance.TogglePanel(InGamePanel.Waiting);
    }

    /// <summary>Sent to a single client — closes their WaitingPanel.</summary>
    [ClientRpc]
    private void HideWaitingPanelClientRpc(ClientRpcParams clientRpcParams = default)
    {
        InGameUIManager.Instance.TogglePanel(InGamePanel.Waiting);
    }

    /// <summary>
    /// Broadcast to ALL clients whenever the sleeping count changes.
    /// WaitingPanel scripts can read SleepManager.Instance.SleepingCount / TotalCount
    /// to update their "N / M players sleeping" label.
    /// </summary>
    [ClientRpc]
    private void UpdateSleepCountClientRpc(int sleeping, int total)
    {
        SleepingCount = sleeping;
        TotalCount    = total;
    }

    // ── Disconnect handling ───────────────────────────────────────────────────

    /// <summary>
    /// Fires on the server whenever any client disconnects.
    /// Removes the client from the sleeping set (whether they were sleeping or not),
    /// then re-evaluates whether the remaining players now satisfy the all-sleeping
    /// condition — so WaitingPanel doesn't get permanently stuck when someone drops.
    /// </summary>
    private void OnClientDisconnected(ulong clientId)
    {
        _sleepingClients.Remove(clientId);

        // Compute remaining connected players, explicitly excluding the disconnecting ID.
        // NGO's callback timing varies — the client may or may not be gone from
        // ConnectedClients yet, so we count manually to be safe either way.
        int total = 0;
        foreach (ulong id in NetworkManager.ConnectedClients.Keys)
            if (id != clientId) total++;

        int sleeping = _sleepingClients.Count;

        if (total == 0)
        {
            // Everyone left — clear state silently, nothing to broadcast
            _sleepingClients.Clear();
            return;
        }

        if (sleeping >= total)
        {
            // The disconnect was the last holdout — the remaining players are all
            // in bed, so advance the day now.
            int completedDay = GameDataManager.Instance.CurrentDayNumber;
            _sleepingClients.Clear();
            GameDataManager.Instance.AdvanceDay();
            AllPlayersSleptClientRpc(completedDay);
        }
        else if (sleeping > 0)
        {
            // Some players are still waiting in bed — refresh their count display
            // so the label doesn't show stale numbers (e.g. "1/2" → "1/1 not right").
            UpdateSleepCountClientRpc(sleeping, total);
        }
        // sleeping == 0: nobody was in bed when this player left, nothing to update.
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Builds a ClientRpcParams that targets a single client by ID.</summary>
    private static ClientRpcParams ToSingle(ulong clientId) => new ClientRpcParams
    {
        Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { clientId } }
    };
}
