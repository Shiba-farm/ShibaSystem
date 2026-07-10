using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

public enum HoldState { Idle, Acting }

public class PlayerHeldItem : NetworkBehaviour
{
    public ItemSO Current { get; private set; }
    public event Action<ItemSO> OnChanged;

    [SerializeField] private HeldItemSignal heldItemSignal;
    [SerializeField] private Animator animator;

    [Header("Hand Anchors")]
    [SerializeField] private Transform rightHandAnchor;
    [SerializeField] private Transform leftHandAnchor;
    [SerializeField] private Transform liftAnchor;

    private HoldState _currentHoldState = HoldState.Idle;
    private GameObject _currentVisual;
    private static readonly int HoldTypeHash = Animator.StringToHash("HoldType");
    private static readonly int IsFishingIdleHash = Animator.StringToHash("IsFishingIdle");
    private static readonly int FishingSpeedHash = Animator.StringToHash("FishingSpeed");
    private static readonly int NoActionHash = Animator.StringToHash("NoAction");

    private int _actionsLayerIndex = -1;

    private PlayerController _controller;
    private PlayerItemUser _playerItemUser;

    private void Start()
    {
        _actionsLayerIndex = animator.GetLayerIndex("Actions");
        if (_actionsLayerIndex < 0)
            Debug.LogWarning("[PlayerHeldItem] Could not find Animator layer named 'Actions'. Fishing cancel will not reset the animation.");

        if (!IsOwner) return;
        heldItemSignal.OnChanged += SetHeld;
        if (heldItemSignal.Current != null)
            SetHeld(heldItemSignal.Current);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsOwner) return;
        _controller = GetComponent<PlayerController>();
        _playerItemUser = GetComponent<PlayerItemUser>();
        if (_controller != null)
            _controller.CurrentFishingPhase.OnValueChanged += OnFishingPhaseChanged;
    }

    public override void OnNetworkDespawn()
    {
        if (_controller != null)
            _controller.CurrentFishingPhase.OnValueChanged -= OnFishingPhaseChanged;
        _controller = null;
        _playerItemUser = null;
        base.OnNetworkDespawn();
    }

    public override void OnDestroy()
    {
        heldItemSignal.OnChanged -= SetHeld;
        base.OnDestroy();
    }

    private void OnFishingPhaseChanged(FishingPhase previous, FishingPhase next)
    {
        if (Current is not FishingRodSO rod) return;
        switch (next)
        {
            case FishingPhase.WaitingForBite:
                // Bool = true → Animator enters FishingIdle state and STAYS there
                // (Any State → FishingIdle, condition: IsFishingIdle = true, no exit time).
                // The clip loops because Loop Time is ticked on the clip asset.
                animator.SetBool(IsFishingIdleHash, true);
                animator.SetFloat(FishingSpeedHash, 1f);
                break;

            case FishingPhase.FishBiting:
                // Still in FishingIdle — bool stays true, just speed up the loop
                // to signal "react now!". Wire FishingSpeed to the state's Speed
                // Multiplier in the Animator so the clip plays faster.
                animator.SetFloat(FishingSpeedHash, 2.5f);
                break;

            case FishingPhase.Pulling:
                // Exit the idle loop first, then fire the one-shot pull trigger.
                // Order matters: turn off the bool so the condition that holds us
                // in FishingIdle is gone, then the trigger transitions to Pull.
                animator.SetBool(IsFishingIdleHash, false);
                animator.SetFloat(FishingSpeedHash, 1f);
                animator.ResetTrigger(rod.PullAnimHash);
                animator.SetTrigger(rod.PullAnimHash);
                if (IsOwner)
                {
                    if (InGameUIManager.Instance != null && InGameUIManager.Instance.IsCriticalPanelOpen)
                        InGameUIManager.Instance.CloseFishingPanel();
                }
                break;

            case FishingPhase.None:
                // Session ended (success, escape, or cancel).
                animator.SetBool(IsFishingIdleHash, false);
                animator.SetFloat(FishingSpeedHash, 1f);
                // FishingIdle lives in the Actions layer, not the Locomotions layer.
                // Play() on layer 0 was wrong — it never touched Actions, so the
                // slow FishingIdle kept blending on top of whatever locomotion did.
                // Targeting the correct layer jumps directly to NoAction with no blend.
                if (_actionsLayerIndex >= 0)
                    animator.Play(NoActionHash, _actionsLayerIndex, 0f);
                // Only the owner clears the input lock and closes the mini-game UI.
                if (IsOwner)
                {
                    _playerItemUser?.OnFishingEnded();
                }
                break;
        }
    }

    private void SetHeld(ItemSO data)
    {
        // // If switching away from fishing rod, cancel any in-progress fishing session
        // if (Current is FishingRodSO && data is not FishingRodSO && IsOwner)
        //     FishingServerManager.Instance?.CancelFishingServerRpc();

        Current = data;
        DestroyCurrentVisual();

        if (data != null && data.equipmentPrefab != null && data.holdType != HoldType.None)
            SpawnVisual(data);

        DriveAnimator(data?.holdType ?? HoldType.None);
        OnChanged?.Invoke(data);
    }

    public void SetHoldState(HoldState state)
    {
        if (_currentVisual == null || Current == null) return;
        StopAllCoroutines();
        StartCoroutine(TransitionHold(Current.GetHoldPosition(state), 0.15f));
    }

    private void SpawnVisual(ItemSO data)
    {
        Transform anchor = data.holdType switch
        {
            HoldType.OneHand => rightHandAnchor,
            HoldType.TwoHand => rightHandAnchor,
            HoldType.TwoHandLift => liftAnchor,
            _ => rightHandAnchor
        };

        _currentVisual = Instantiate(data.equipmentPrefab, anchor);
        ApplyHoldPosition(data.GetHoldPosition(_currentHoldState));
    }

    private void ApplyHoldPosition(HoldPosition hold)
    {
        if (hold == null || _currentVisual == null) return;
        _currentVisual.transform.localPosition = hold.positionOffset;
        _currentVisual.transform.localEulerAngles = hold.rotationOffset;
        // Treat an all-zero scale as "not yet configured" → use identity
        _currentVisual.transform.localScale =
            hold.localScale == Vector3.zero ? Vector3.one : hold.localScale;
    }

    private IEnumerator TransitionHold(HoldPosition target, float duration)
    {
        if (target == null) yield break;
        var t = _currentVisual.transform;
        Vector3 startPos = t.localPosition;
        Quaternion startRot = t.localRotation;
        Quaternion endRot = Quaternion.Euler(target.rotationOffset);
        Vector3 endScale = target.localScale == Vector3.zero ? Vector3.one : target.localScale;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float pct = elapsed / duration;
            t.localPosition = Vector3.Lerp(startPos, target.positionOffset, pct);
            t.localRotation = Quaternion.Lerp(startRot, endRot, pct);
            t.localScale = Vector3.Lerp(t.localScale, endScale, pct);
            yield return null;
        }

        ApplyHoldPosition(target);
    }

    private void DestroyCurrentVisual()
    {
        if (_currentVisual != null)
        {
            Destroy(_currentVisual);
            _currentVisual = null;
        }
    }

    private void DriveAnimator(HoldType holdType)
    {
        animator.SetInteger(HoldTypeHash, (int)holdType);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_currentVisual == null || Current == null) return;
        ApplyHoldPosition(Current.GetHoldPosition(_currentHoldState));
    }
#endif
}
