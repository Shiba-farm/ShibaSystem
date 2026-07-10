using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : NetworkBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }
    public static string LastUsedDoorID { get; private set; }

    // ── Static-state reset ───────────────────────────────────
    // When "Reload Domain" is disabled in Enter Play Mode settings, static
    // fields survive between Play sessions.  This method runs unconditionally
    // every time Play mode is entered (even without a domain reload) and wipes
    // any leftover door ID so RepositionPlayersNextFrame never routes players
    // to a door from a prior session.
#if UNITY_EDITOR
    [UnityEngine.RuntimeInitializeOnLoadMethod(
        UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        LastUsedDoorID = string.Empty;
    }
#endif

    // True only while an actual LoadNetworkScene transition is in flight.
    // Prevents RepositionPlayersNextFrame from firing when a late-joining
    // client syncs the current scene (OnLoadComplete fires per-client, so
    // without this guard every new joiner would warp ALL existing players).
    private bool _pendingReposition = false;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        // Belt-and-suspenders: also clear here in case ResetStaticState ran
        // before this object was spawned in a previous session.
        LastUsedDoorID = string.Empty;

        // Listen for NGO scene events to trigger fade on all clients
        NetworkManager.Singleton.SceneManager.OnLoad += OnNetworkSceneLoad;
        NetworkManager.Singleton.SceneManager.OnLoadComplete += OnNetworkSceneLoadComplete;
    }

    public override void OnNetworkDespawn()
    {
        if (NetworkManager.Singleton?.SceneManager == null) return;
        NetworkManager.Singleton.SceneManager.OnLoad -= OnNetworkSceneLoad;
        NetworkManager.Singleton.SceneManager.OnLoadComplete -= OnNetworkSceneLoadComplete;
    }

    // ── Multiplayer Scene Transition (server only) ───────────
    // Moves ALL players to a new scene together
    public void LoadNetworkScene(string sceneName, string fromDoorID = "")
    {
        if (!IsServer)
        {
            Debug.LogWarning("[SceneTransition] Only server can load network scenes.");
            return;
        }
        LastUsedDoorID = fromDoorID;

        StartCoroutine(NetworkSceneTransition(sceneName));
    }

    private IEnumerator NetworkSceneTransition(string sceneName, string fromDoorID = "")
    {
        // Tell SaveLoadManager we're transitioning — don't restore on reconnect
        SaveLoadManager.Instance?.SetSceneTransitioning(true);

        // Arm the reposition guard BEFORE calling LoadScene so that the very
        // first OnLoadComplete (server's own) triggers RepositionPlayersNextFrame.
        _pendingReposition = true;
        string leavingScene = SceneManager.GetActiveScene().name;
        GameDataManager.Instance?.CaptureTimeForTransition();
        GameDataManager.Instance?.CaptureDebtForTransition();
        GameDataManager.Instance?.CaptureGoldForTransition();
        GameDataManager.Instance?.CaptureFarmForTransition(leavingScene);

        FadeOutClientRpc();
        yield return new WaitForSeconds(GetFadeDuration());

        NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }

    // ── NGO Scene Event Callbacks ────────────────────────────
    // Fires on ALL clients when NGO starts loading
    private void OnNetworkSceneLoad(
        ulong clientId,
        string sceneName,
        LoadSceneMode loadSceneMode,
        AsyncOperation asyncOperation)
    {
        // Already fading from server RPC — just ensure local fade is active
        UIManager.Instance?.FadeOut();
    }

    // Fires on ALL clients when scene fully loaded.
    // NGO calls this once per client that finishes loading, so for a 3-player
    // game the server receives it 3 times.
    //   • Real scene transition (_pendingReposition=true): reposition ALL players.
    //   • Initial load / late-join (_pendingReposition=false): position ONLY the
    //     newly loading client so existing players are never disturbed.
    private void OnNetworkSceneLoadComplete(
    ulong clientId,
    string sceneName,
    LoadSceneMode loadSceneMode)
    {
        StartCoroutine(FadeInAfterLoad());

        if (!IsServer) return;

        if (_pendingReposition)
        {
            _pendingReposition = false;
            RepositionAllPlayers();               // sync — same frame, no flash
        }
        else
        {
            RepositionSinglePlayer(clientId);     // sync — same frame, no flash
        }
    }

    // ── Full scene-transition reposition (ALL players) ───────
    // Runs synchronously in the same frame as OnLoadComplete so that
    // TeleportOwnerRpc is batched with the NGO scene-load messages.
    // Clients receive spawn-state and teleport together → no 0,0,0 flash.
    private void RepositionAllPlayers()
    {
        // Reset flag — scene loaded, normal connection behavior resumes
        SaveLoadManager.Instance?.SetSceneTransitioning(false);

        if (SpawnPointManager.Instance == null)
        {
            Debug.LogWarning("[SceneTransition] No SpawnPointManager in new scene.");
            return;
        }

        bool hasOverride = SpawnPointManager.Instance.HasOverridePoint();

        int playerIndex = 0;
        foreach (var client in NetworkManager.ConnectedClientsList)
        {
            var playerObj = client.PlayerObject;
            if (playerObj == null) { playerIndex++; continue; }

            Vector3 pos;
            Quaternion rot;

            if (hasOverride)
            {
                pos = SpawnPointManager.Instance.GetNextPosition();
                rot = SpawnPointManager.Instance.GetNextRotation();
            }
            else
            {
                var t = SpawnPointManager.Instance.GetMultiplayerSpawnTransform(playerIndex);
                pos = t != null ? t.position : Vector3.zero;
                rot = t != null ? t.rotation : Quaternion.identity;
            }

            var pc = playerObj.GetComponent<PlayerController>();
            if (pc != null)
                pc.TeleportOwnerRpc(pos, rot);
            else
            {
                playerObj.transform.position = pos;
                playerObj.transform.eulerAngles = new Vector3(0, rot.eulerAngles.y, 0);
            }

            Debug.Log($"[SceneTransition] Repositioned client {client.ClientId} → {pos}");
            playerIndex++;
        }
    }

    // ── Late-join / initial-load reposition (ONE player only) ─
    // Runs synchronously in the same frame as OnLoadComplete.
    // If PlayerObject isn't ready yet (rare edge-case), falls back to a
    // 1-frame-delayed coroutine rather than silently failing.
    private void RepositionSinglePlayer(ulong clientId)
    {
        // If a save file has a position for this client, SaveLoadManager
        // handles it via LoadGame / OnClientConnected.  Don't compete.
        var loadedSave = SaveLoadManager.Instance?.GetLoadedSave();
        if (loadedSave?.FindPlayer(clientId) != null)
        {
            Debug.Log($"[SceneTransition] Client {clientId} has saved position — deferring to SaveLoadManager.");
            return;
        }

        if (SpawnPointManager.Instance == null) return;
        if (!NetworkManager.ConnectedClients.TryGetValue(clientId, out var client)) return;

        var playerObj = client.PlayerObject;
        if (playerObj == null)
        {
            // PlayerObject not ready yet — rare but possible; retry next frame
            StartCoroutine(RepositionSinglePlayerFallback(clientId));
            return;
        }

        ApplySpawnPoint(clientId, playerObj);
    }

    // Fallback: retry once after 1 frame (only reached when PlayerObject wasn't
    // assigned by the time OnLoadComplete fired — should not happen normally).
    private IEnumerator RepositionSinglePlayerFallback(ulong clientId)
    {
        yield return null;

        var loadedSave = SaveLoadManager.Instance?.GetLoadedSave();
        if (loadedSave?.FindPlayer(clientId) != null) yield break;

        if (!NetworkManager.ConnectedClients.TryGetValue(clientId, out var client)) yield break;
        var playerObj = client.PlayerObject;
        if (playerObj == null) yield break;

        ApplySpawnPoint(clientId, playerObj);
    }

    // Shared helper: calculate spawn slot and send TeleportOwnerRpc.
    private void ApplySpawnPoint(ulong clientId, NetworkObject playerObj)
    {
        if (SpawnPointManager.Instance == null) return;

        int playerIndex = 0;
        foreach (var c in NetworkManager.ConnectedClientsList)
        {
            if (c.ClientId == clientId) break;
            playerIndex++;
        }

        var t = SpawnPointManager.Instance.GetMultiplayerSpawnTransform(playerIndex);
        var pos = t != null ? t.position : Vector3.zero;
        var rot = t != null ? t.rotation : Quaternion.identity;

        var pc = playerObj.GetComponent<PlayerController>();
        if (pc != null)
            pc.TeleportOwnerRpc(pos, rot);
        else
        {
            playerObj.transform.position = pos;
            playerObj.transform.eulerAngles = new Vector3(0, rot.eulerAngles.y, 0);
        }

        Debug.Log($"[SceneTransition] Positioned client {clientId} (slot {playerIndex}) → {pos}");
    }

    private IEnumerator FadeInAfterLoad()
    {
        yield return new WaitForSeconds(0.1f);  // small buffer for objects to spawn
        UIManager.Instance?.FadeIn();
    }

    // ── Client RPCs for fade sync ────────────────────────────
    [Rpc(SendTo.Everyone)]
    private void FadeOutClientRpc()
    {
        UIManager.Instance?.FadeOut();
    }

    private float GetFadeDuration()
    {
        return UIManager.Instance != null ? UIManager.Instance.FadeDuration : 0.5f;
    }
}
