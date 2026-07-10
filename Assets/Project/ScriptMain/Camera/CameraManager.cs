using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// Scene-placed singleton that owns all Cinemachine priority swaps.
/// Place this on a dedicated GameObject in the scene alongside your VCams.
///
/// Any system that needs a temporary camera moment calls Push / PushFor / Pop.
/// Nothing else should touch VCam priorities directly.
///
/// API
/// ───
/// Push(vcam)              — activate vcam, stay until Pop() or the next Push()
/// PushFor(vcam, duration) — activate vcam, auto-return after N seconds
/// Pop()                   — immediately return to default camera
///
/// Access from any script:
///   CameraManager.Instance?.Push(myVCam);
///   CameraManager.Instance?.PushFor(myVCam, 2f);
///   CameraManager.Instance?.Pop();
///
/// How priority works
/// ──────────────────
/// Your default gameplay VCam sits at its natural priority (e.g. 10).
/// Every override VCam should have its Inspector Priority set to 0 so it is
/// dormant by default — this script bumps it to overridePriority when pushed,
/// then drops it back to 0 on pop. CinemachineBrain detects the priority change
/// and blends using the style / duration you set on the Brain component.
/// EaseInOut 0.4 s is a good starting point.
///
/// Adding new feature cameras
/// ──────────────────────────
/// For dedicated VCams tied to a game state (sleep, dungeon entrance, etc.):
///   1. Add a [SerializeField] CinemachineCamera field under "Feature Cameras".
///   2. Add its push/pop logic to the appropriate event handler below.
///
/// For ad-hoc one-off moments (treasure chest, NPC event) the calling script
/// already has a scene reference to its own VCam — just call Instance directly:
///   CameraManager.Instance?.Push(mySceneVCam);
///   CameraManager.Instance?.Pop();
/// No changes to this file needed for those cases.
/// </summary>
public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance { get; private set; }

    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Feature Cameras")]
    [Tooltip("Close-up VCam that activates during the fishing pull animation. " +
             "Set its Inspector Priority to 0 — this script controls it at runtime.")]
    [SerializeField] private CinemachineCamera fishingPullVCam;
    // Add more feature VCams here as needed, e.g.:
    // [SerializeField] private CinemachineCamera sleepVCam;
    // [SerializeField] private CinemachineCamera dungeonEntranceVCam;

    [Header("Override Priority")]
    [Tooltip("Priority given to the active override VCam. " +
             "Must exceed your default gameplay VCam's priority.")]
    [SerializeField] private int overridePriority = 100;

    // ── Private state ─────────────────────────────────────────────────────────

    private CinemachineCamera _activeOverride;
    private Coroutine         _autoReturnRoutine;
    private PlayerController  _localPlayer;   // registered by the owner player on spawn

    // ── Unity lifecycle ───────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Guarantee all override VCams are dormant at scene start
        SetPriority(fishingPullVCam, 0);
    }

    // ── Player registration ───────────────────────────────────────────────────

    /// <summary>
    /// Called by the local owner player in OnNetworkSpawn.
    /// Subscribes to that player's state so CameraManager can react to
    /// game-state changes (fishing phase, etc.) without needing NetworkBehaviour.
    /// </summary>
    public void RegisterLocalPlayer(PlayerController controller)
    {
        // Clean up any previous registration (e.g. scene reload)
        UnregisterLocalPlayer(_localPlayer);

        _localPlayer = controller;
        if (_localPlayer == null) return;

        _localPlayer.CurrentFishingPhase.OnValueChanged += OnFishingPhaseChanged;
        SetPriority(fishingPullVCam, 0);   // ensure dormant on fresh registration
    }

    /// <summary>
    /// Called by the local owner player in OnNetworkDespawn.
    /// Clears subscriptions and resets any active camera override.
    /// </summary>
    public void UnregisterLocalPlayer(PlayerController controller)
    {
        if (controller == null || controller != _localPlayer) return;

        _localPlayer.CurrentFishingPhase.OnValueChanged -= OnFishingPhaseChanged;
        _localPlayer = null;

        // Safety: clear any active override so the camera doesn't get stuck
        ForcePopImmediate();
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Activates <paramref name="vcam"/> immediately. Stays active until
    /// <see cref="Pop"/> is called or another <see cref="Push"/> replaces it.
    /// </summary>
    public void Push(CinemachineCamera vcam)
    {
        if (vcam == null) return;

        CancelAutoReturn();
        SetPriority(_activeOverride, 0);   // drop previous (no-op when null)

        _activeOverride = vcam;
        SetPriority(vcam, overridePriority);
    }

    /// <summary>
    /// Activates <paramref name="vcam"/> for <paramref name="duration"/> seconds,
    /// then automatically calls <see cref="Pop"/>.
    /// </summary>
    public void PushFor(CinemachineCamera vcam, float duration)
    {
        Push(vcam);
        _autoReturnRoutine = StartCoroutine(AutoReturn(duration));
    }

    /// <summary>
    /// Returns to the default camera. Cancels any pending auto-return.
    /// Safe to call even when nothing has been pushed.
    /// </summary>
    public void Pop()
    {
        CancelAutoReturn();
        SetPriority(_activeOverride, 0);
        _activeOverride = null;
    }

    // ── Built-in: fishing pull ────────────────────────────────────────────────

    private void OnFishingPhaseChanged(FishingPhase prev, FishingPhase next)
    {
        switch (next)
        {
            case FishingPhase.Pulling:
                Push(fishingPullVCam);
                break;

            case FishingPhase.None:
                // Only pop if fishing is still the active override — avoids cutting
                // short a different system's camera moment that may have started
                // while the fishing result was being resolved.
                if (_activeOverride == fishingPullVCam)
                    Pop();
                break;
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void CancelAutoReturn()
    {
        if (_autoReturnRoutine == null) return;
        StopCoroutine(_autoReturnRoutine);
        _autoReturnRoutine = null;
    }

    private IEnumerator AutoReturn(float duration)
    {
        yield return new WaitForSeconds(duration);
        _autoReturnRoutine = null;
        Pop();
    }

    private void SetPriority(CinemachineCamera vcam, int priority)
    {
        if (vcam != null)
            vcam.Priority = priority;
    }

    private void ForcePopImmediate()
    {
        _autoReturnRoutine = null;
        SetPriority(_activeOverride, 0);
        _activeOverride = null;
    }
}
