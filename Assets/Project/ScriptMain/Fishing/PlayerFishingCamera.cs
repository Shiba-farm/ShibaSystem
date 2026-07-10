using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Owner-only component that shifts Cinemachine priority to a close-up virtual
/// camera while the fishing pull animation plays, then blends back on completion.
///
/// How it works:
///   Your normal gameplay VCam sits at some base priority (e.g. 10).
///   The fishing pull VCam normally has priority 0 so it is dormant.
///   When FishingPhase becomes Pulling this script bumps its priority above the
///   normal VCam; CinemachineBrain detects the change and blends to it using
///   whatever blend style/duration you set on the Brain component.
///   When the phase returns to None the priority drops back to 0 and the Brain
///   blends back to the normal VCam automatically.
///
/// Editor setup (do once):
///   1. In your CM camera rig, add a second CinemachineVirtualCamera.
///      Name it e.g. "VCam_FishingPull".
///      • Follow  → same transform as your normal VCam (player body/root)
///      • Look At → player chest / head, or a dedicated "fish anchor" child transform
///      • Body    → adjust the shoulder / offset so the camera is closer and slightly
///                  lower, looking up at the rod tip. Framing that works well:
///                  Offset Y +0.5, camera closer by ~1–2 units.
///      • Lens    → lower FOV (e.g. 40 vs your normal 60) for the zoom-in feel.
///   2. Leave that VCam's Priority field at 0 in the Inspector — this script drives it.
///   3. On CinemachineBrain set Default Blend to "EaseInOut" 0.4 s (or your taste).
///   4. Assign the VCam to the fishingPullVCam slot on this component (player prefab).
///
/// Note: if you are on Cinemachine 3.x, replace CinemachineVirtualCamera with
/// CinemachineCamera and the using directive with "using Unity.Cinemachine;".
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class PlayerFishingCamera : NetworkBehaviour
{
    [Header("Virtual Camera")]
    [Tooltip("The close-up VCam that activates during the fishing pull animation. " +
             "Leave its Inspector Priority at 0 — this script controls it at runtime.")]
    [SerializeField] private CinemachineCamera fishingPullVCam;

    [Header("Priority")]
    [Tooltip("Priority while the pull VCam is inactive. Must be below your normal gameplay VCam's priority.")]
    [SerializeField] private int idlePriority   = 0;
    [Tooltip("Priority while the pull VCam is active. Must be above your normal gameplay VCam's priority.")]
    [SerializeField] private int activePriority = 20;

    private PlayerController _controller;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Non-owners share the same physical scene camera driven by the local player's
        // components — they must never touch it.
        if (!IsOwner)
        {
            enabled = false;
            return;
        }

        _controller = GetComponent<PlayerController>();

        if (_controller != null)
            _controller.CurrentFishingPhase.OnValueChanged += OnPhaseChanged;

        // Ensure the VCam starts dormant regardless of what the Inspector Priority was set to.
        if (fishingPullVCam != null)
            fishingPullVCam.Priority = idlePriority;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        if (_controller != null)
            _controller.CurrentFishingPhase.OnValueChanged -= OnPhaseChanged;

        // Reset priority so the VCam is never left active if this object despawns
        // mid-session (e.g. host migration, disconnect).
        if (fishingPullVCam != null)
            fishingPullVCam.Priority = idlePriority;
    }

    private void OnPhaseChanged(FishingPhase prev, FishingPhase next)
    {
        if (fishingPullVCam == null) return;

        fishingPullVCam.Priority = next == FishingPhase.Pulling ? activePriority : idlePriority;
    }
}
