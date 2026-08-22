using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Fishing mini-game panel — local logic only (no server calls yet).
///
/// Setup in Inspector:
///   leftPivot / rightPivot  — empty GameObjects that mark the two ends of the bar.
///   fish                    — the fish icon Transform (moves between pivots).
///   hook                    — the catch zone Transform (RectTransform + Image).
///   progressSlider          — Slider (min 0, max 1) that shows catch progress.
///
/// Opened / closed by InGameUIManager. Open() resets state; ForceClose() stops the loop.
/// </summary>
public class FishingMiniGameUI : MonoBehaviour
{
    // ── Bar endpoints ─────────────────────────────────────────────────────────

    [Header("Bar")]
    [SerializeField] private Transform _leftPivot;
    [SerializeField] private Transform _rightPivot;
    [SerializeField] private RectTransform _barRect;

    // ── Fish ──────────────────────────────────────────────────────────────────

    [Header("Fish")]
    [SerializeField] private Transform _fish;
    [SerializeField] private float _timerMultiplicator = 3f;   // max seconds between destination picks
    [SerializeField] private float _smoothMotion = 1f;   // SmoothDamp time

    // ── Hook (catch zone) ─────────────────────────────────────────────────────

    [Header("Hook / Catch Zone")]
    [SerializeField] private RectTransform _hookRect;              // the sliding catch zone
    /// <summary>Fraction of bar width the hook covers (0–1). Used by Resize().</summary>
    [SerializeField][Range(0.05f, 1f)] private float _hookSize = 0.15f;

    [Header("Hook Physics")]
    [SerializeField] private float _hookPullPower = 0.01f;     // velocity added per second while clicking
    [SerializeField] private float _hookGravityPower = 0.005f;    // velocity removed per second always
    [SerializeField] private float _maxHookVelocity = 0.015f;    // hard cap — prevents runaway buildup

    // ── Progress ──────────────────────────────────────────────────────────────

    [Header("Progress")]
    [SerializeField] private Slider progressSlider;              // min 0, max 1
    [SerializeField] private Slider timerSlider;              // min 0, max 1
    [SerializeField] private float _hookPower = 0.3f;  // progress gain rate while aligned
    [SerializeField] private float _hookProgressDegradation = 0.1f; // progress loss rate while misaligned
    // [SerializeField] private float _failDuration = 10f;   // seconds misaligned before Lose()

    // ── Private state ─────────────────────────────────────────────────────────

    private bool _isActive;

    // fish
    private float _fishPosition;
    private float _fishDestination;
    private float _fishSpeed;
    private float _fishTimer;
    private float _fishcatchTimeRequired;
    private float _fishUncertainty;

    // hook
    private float _hookPosition;    // normalised 0–1 along bar
    private float _hookVelocity;

    // progress
    private float _hookProgress;
    // private float _failTimer;
    private float _normalizedHalfSize;
    private float _fishFleeTime;

    // ── Unity lifecycle ───────────────────────────────────────────────────────

    private void Start()
    {
        InputHandler.Singleton.OnLeftClickReleased -= OnMouseReleased;
        InputHandler.Singleton.OnLeftClickReleased += OnMouseReleased;

        Resize();
    }

    private void OnDestroy()
    {
        if (InputHandler.Singleton != null)
            InputHandler.Singleton.OnLeftClickReleased -= OnMouseReleased;
    }

    // ── Public API (InGameUIManager) ──────────────────────────────────────────

    public void Open(float fishMoveSpeed, float fishUncertainty, float catchTimeRequired, float timeBeforeFlee, float pullStrength, float hookPower)
    {
        // Reset fish
        _fishPosition = 0.5f;
        _fishDestination = 0.5f;
        _fishSpeed = fishMoveSpeed;
        _fishTimer = 0;

        _fishcatchTimeRequired = catchTimeRequired;
        _fishUncertainty = fishUncertainty;

        _hookPullPower = pullStrength;
        _hookPower = hookPower;

        // Reset hook
        _hookPosition = 0.5f;
        _hookVelocity = 0f;

        // Reset progress
        _hookProgress = 0f;
        // _failTimer = _failDuration;

        _fishFleeTime = timeBeforeFlee;

        if (progressSlider != null)
            progressSlider.value = 0f;

        if (timerSlider != null)
        {
            timerSlider.maxValue = _fishFleeTime;
            timerSlider.value = _fishFleeTime;
        }

        Resize();
        _isActive = true;
    }

    public void ForceClose()
    {
        _isActive = false;
        _hookProgress = 0f;

        if (progressSlider != null)
            progressSlider.value = 0f;

        if (timerSlider != null)
            timerSlider.value = 0f;
    }

    // ── Game loop ─────────────────────────────────────────────────────────────

    private void Update()
    {
        if (!_isActive) return;

        MoveFish();
        MoveHook();
        UpdateProgress();
    }

    // ── Fish movement ─────────────────────────────────────────────────────────

    private void MoveFish()
    {
        _fishTimer -= Time.deltaTime;
        if (_fishTimer <= 0f)
        {
            _fishTimer = Random.value * _timerMultiplicator * Random.Range(1f - _fishUncertainty, 1f + _fishUncertainty);
            _fishDestination = Random.value;
        }

        _fishPosition = Mathf.SmoothDamp(_fishPosition, _fishDestination, ref _fishSpeed, _smoothMotion);
        _fish.position = Vector3.Lerp(_leftPivot.position, _rightPivot.position, _fishPosition);
    }

    // ── Hook (catch zone) movement ────────────────────────────────────────────

    private void MoveHook()
    {
        _hookVelocity -= _hookGravityPower * Time.deltaTime;
        // Accumulate velocity
        if (InputHandler.Singleton != null && InputHandler.Singleton.IsLeftClickHeld)
        {
            _hookVelocity += _hookPullPower * Time.deltaTime;
        }

        // ── KEY FIX: hard cap prevents runaway buildup from holding too long ──
        _hookVelocity = Mathf.Clamp(_hookVelocity, -_maxHookVelocity, _maxHookVelocity);

        Debug.Log($"[FishingMiniGameUI] Hook pos: {_hookPosition:F3}, vel: {_hookVelocity:F3}, progress: {_hookProgress:F3}");

        // Zero velocity when pressing against a wall (consistent boundary: hookSize / 2)
        float halfSize = _normalizedHalfSize;
        if (_hookPosition <= halfSize && _hookVelocity < 0f) _hookVelocity = 0f;
        if (_hookPosition >= 1f - halfSize && _hookVelocity > 0f) _hookVelocity = 0f;

        _hookPosition = Mathf.Clamp(_hookPosition + _hookVelocity, halfSize, 1f - halfSize);
        _hookRect.position = Vector3.Lerp(_leftPivot.position, _rightPivot.position, _hookPosition);
    }

    private void OnMouseReleased()
    {
        // Zero velocity on release so there's no leftover momentum dragging the hook
        _hookVelocity = 0f;
    }

    // ── Progress bar ──────────────────────────────────────────────────────────

    private void UpdateProgress()
    {
        float halfSize = _hookSize * 0.5f;
        float min = _hookPosition - halfSize;
        float max = _hookPosition + halfSize;
        bool aligned = min < _fishPosition && _fishPosition < max;

        _fishFleeTime -= Time.deltaTime;

        if (aligned)
        {
            _hookProgress += _hookPower * Time.deltaTime;
            // _failTimer = _failDuration;   // reset fail timer while catching
        }
        else
        {
            _hookProgress -= _hookProgressDegradation * Time.deltaTime;
            // _failTimer -= Time.deltaTime;

        }

        if (_fishFleeTime <= 0f)
        {
            Lose();
            return;
        }
        _hookProgress = Mathf.Clamp(_hookProgress, 0f, _fishcatchTimeRequired);

        // ── Slider: just set value directly (min 0 / max 1) ──
        if (progressSlider != null)
            progressSlider.value = _hookProgress / _fishcatchTimeRequired;
        if (timerSlider != null)
            timerSlider.value = _fishFleeTime;

        if (_hookProgress >= _fishcatchTimeRequired)
            Win();
    }

    // ── Resize hook width to match hookSize fraction of bar ───────────────────

    /// <summary>
    /// Sets the hookRect width so it covers hookSize × bar-length pixels.
    /// Call this in Start and whenever hookSize changes at runtime.
    /// Uses RectTransform.sizeDelta instead of localScale so the Image
    /// renders at the correct pixel size without distorting its aspect ratio.
    /// </summary>
    private void Resize()
    {
        if (_hookRect == null || _leftPivot == null || _rightPivot == null) return;

        float barLength = _barRect.rect.width;
        if (barLength <= 0f) return;

        // Clamp hookSize itself to a sane range so it can never produce
        // an inverted or degenerate clamp range.
        _hookSize = Mathf.Clamp(_hookSize, 0.05f, 1f);

        Vector2 size = _hookRect.sizeDelta;
        size.x = barLength * _hookSize;
        _hookRect.sizeDelta = size;

        // Derive the normalized half-size from the ACTUAL rendered rect width,
        // in the same space the position math uses (barLength), instead of
        // trusting hookSize's fraction to already match reality.
        float actualWidth = _hookRect.rect.width; // post-layout real width
        _normalizedHalfSize = (actualWidth / barLength) * 0.5f;
    }

    // ── Win / Lose ────────────────────────────────────────────────────────────

    private void Win()
    {
        _isActive = false;
        Debug.Log("[FishingMiniGameUI] Win!");
        // Notify server: give fish to inventory, transition through Pulling → None.
        // Panel closes automatically when phase reaches None via PlayerHeldItem.
        FishingServerManager.Instance?.SubmitFishingResultServerRpc(true);
    }

    private void Lose()
    {
        _isActive = false;
        Debug.Log("[FishingMiniGameUI] Lose.");
        // Close the panel immediately — no waiting on server round-trip for the visual.
        // IsCriticalPanelOpen is cleared here, so the subsequent None phase change
        // that arrives from the server is safely ignored by the guard in PlayerHeldItem.
        InGameUIManager.Instance?.CloseFishingPanel();
        // Tell server to end the session and transition phase → None.
        FishingServerManager.Instance?.SubmitFishingResultServerRpc(false);
    }

}
