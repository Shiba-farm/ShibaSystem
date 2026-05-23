using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : NetworkBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }
    public static string LastUsedDoorID { get; private set; }

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
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

    // Fires on ALL clients when scene fully loaded
    private void OnNetworkSceneLoadComplete(
    ulong clientId,
    string sceneName,
    LoadSceneMode loadSceneMode)
    {
        StartCoroutine(FadeInAfterLoad());

        if (!IsServer) return;
        StartCoroutine(RepositionPlayersNextFrame());
    }

    private IEnumerator RepositionPlayersNextFrame()
    {
        yield return null;

        // Reset flag — scene loaded, normal connection behavior resumes
        SaveLoadManager.Instance?.SetSceneTransitioning(false);

        if (SpawnPointManager.Instance == null)
        {
            Debug.LogWarning("[SceneTransition] No SpawnPointManager in new scene.");
            yield break;
        }

        foreach (var client in NetworkManager.ConnectedClientsList)
        {
            var playerObj = client.PlayerObject;
            if (playerObj == null) continue;

            Vector3 pos = SpawnPointManager.Instance.GetNextPosition();
            Quaternion rot = SpawnPointManager.Instance.GetNextRotation();

            playerObj.transform.position = pos;
            playerObj.transform.eulerAngles = new Vector3(0, rot.eulerAngles.y, 0);

            Debug.Log($"[SceneTransition] Repositioned client {client.ClientId} to {pos}");
            // ← NO RestorePlayerState here — stats stay as they are
        }
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
