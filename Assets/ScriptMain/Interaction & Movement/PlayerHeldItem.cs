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

    private void Start()
    {
        if (!IsOwner) return;
        heldItemSignal.OnChanged += SetHeld;
        if (heldItemSignal.Current != null)
            SetHeld(heldItemSignal.Current);
    }

    public override void OnDestroy()
    {
        heldItemSignal.OnChanged -= SetHeld;
    }

    private void SetHeld(ItemSO data)
    {
        Current = data;

        DestroyCurrentVisual();

        Debug.Log($"Set Held");

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
    }

    private IEnumerator TransitionHold(HoldPosition target, float duration)
    {
        if (target == null) yield break;
        var t = _currentVisual.transform;
        Vector3 startPos = t.localPosition;
        Quaternion startRot = t.localRotation;
        Quaternion endRot = Quaternion.Euler(target.rotationOffset);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float pct = elapsed / duration;
            t.localPosition = Vector3.Lerp(startPos, target.positionOffset, pct);
            t.localRotation = Quaternion.Lerp(startRot, endRot, pct);
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
        Debug.Log($"Set animation to {holdType}");
        animator.SetInteger(HoldTypeHash, (int)holdType);
    }
#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_currentVisual == null || Current == null) return;

        var hold = Current.GetHoldPosition(_currentHoldState);
        if (hold == null) return;

        _currentVisual.transform.localPosition = hold.positionOffset;
        _currentVisual.transform.localEulerAngles = hold.rotationOffset;
        // _currentVisual.transform.localScale = Current.holdScale;
    }
#endif

}
