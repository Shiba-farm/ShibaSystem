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
    [SerializeField] private Transform leftPivot;
    [SerializeField] private Transform rightPivot;

    // ── Fish ──────────────────────────────────────────────────────────────────

    [Header("Fish")]
    [SerializeField] private Transform fish;
    [SerializeField] private float     timerMultiplicator = 3f;   // max seconds between destination picks
    [SerializeField] private float     smoothMotion       = 1f;   // SmoothDamp time

    // ── Hook (catch zone) ─────────────────────────────────────────────────────

    [Header("Hook / Catch Zone")]
    [SerializeField] private RectTransform hookRect;              // the sliding catch zone
    /// <summary>Fraction of bar width the hook covers (0–1). Used by Resize().</summary>
    [SerializeField] [Range(0.05f, 0.5f)] private float hookSize = 0.15f;

    [Header("Hook Physics")]
    [SerializeField] private float hookPullPower    = 0.01f;     // velocity added per second while clicking
    [SerializeField] private float hookGravityPower = 0.005f;    // velocity removed per second always
    [SerializeField] private float maxHookVelocity  = 0.015f;    // hard cap — prevents runaway buildup

    // ── Progress ──────────────────────────────────────────────────────────────

    [Header("Progress")]
    [SerializeField] private Slider progressSlider;              // min 0, max 1
    [SerializeField] private float  hookPower              = 0.3f;  // progress gain rate while aligned
    [SerializeField] private float  hookProgressDegradation = 0.1f; // progress loss rate while misaligned
    [SerializeField] private float  failDuration           = 10f;   // seconds misaligned before Lose()

    // ── Private state ─────────────────────────────────────────────────────────

    private bool  _isActive;

    // fish
    private float _fishPosition;
    private float _fishDestination;
    private float _fishSpeed;
    private float _fishTimer;

    // hook
    private float _hookPosition;    // normalised 0–1 along bar
    private float _hookVelocity;

    // progress
    private float _hookProgress;
    private float _failTimer;

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

    public void Open(float fishMoveSpeed, float fishUncertainty, float catchTimeRequired, float pullStrength)
    {
        // Reset fish
        _fishPosition    = 0.5f;
        _fishDestination = 0.5f;
        _fishSpeed       = 0f;
        _fishTimer       = 0f;

        // Reset hook
        _hookPosition = 0.5f;
        _hookVelocity = 0f;

        // Reset progress
        _hookProgress = 0f;
        _failTimer    = failDuration;

        if (progressSlider != null)
            progressSlider.value = 0f;

        Resize();
        _isActive = true;
    }

    public void ForceClose()
    {
        _isActive = false;
        _hookProgress = 0f;

        if (progressSlider != null)
            progressSlider.value = 0f;
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
            _fishTimer       = Random.value * timerMultiplicator;
            _fishDestination = Random.value;
        }

        _fishPosition = Mathf.SmoothDamp(_fishPosition, _fishDestination, ref _fishSpeed, smoothMotion);
        fish.position = Vector3.Lerp(leftPivot.position, rightPivot.position, _fishPosition);
    }

    // ── Hook (catch zone) movement ────────────────────────────────────────────

    private void MoveHook()
    {
        // Accumulate velocity
        if (InputHandler.Singleton != null && InputHandler.Singleton.IsLeftClickHeld)
            _hookVelocity += hookPullPower * Time.deltaTime;

        _hookVelocity -= hookGravityPower * Time.deltaTime;

        // ── KEY FIX: hard cap prevents runaway buildup from holding too long ──
        _hookVelocity = Mathf.Clamp(_hookVelocity, -maxHookVelocity, maxHookVelocity);

        // Zero velocity when pressing against a wall (consistent boundary: hookSize / 2)
        float halfSize = hookSize * 0.5f;
        if (_hookPosition <= halfSize       && _hookVelocity < 0f) _hookVelocity = 0f;
        if (_hookPosition >= 1f - halfSize  && _hookVelocity > 0f) _hookVelocity = 0f;

        _hookPosition = Mathf.Clamp(_hookPosition + _hookVelocity, halfSize, 1f - halfSize);
        hookRect.position = Vector3.Lerp(leftPivot.position, rightPivot.position, _hookPosition);
    }

    private void OnMouseReleased()
    {
        // Zero velocity on release so there's no leftover momentum dragging the hook
        _hookVelocity = 0f;
    }

    // ── Progress bar ──────────────────────────────────────────────────────────

    private void UpdateProgress()
    {
        float halfSize = hookSize * 0.5f;
        float min = _hookPosition - halfSize;
        float max = _hookPosition + halfSize;
        bool  aligned = min < _fishPosition && _fishPosition < max;

        if (aligned)
        {
            _hookProgress += hookPower * Time.deltaTime;
            _failTimer    =  failDuration;   // reset fail timer while catching
        }
        else
        {
            _hookProgress -= hookProgressDegradation * Time.deltaTime;
            _failTimer    -= Time.deltaTime;

            if (_failTimer <= 0f)
            {
                Lose();
                return;
            }
        }

        _hookProgress = Mathf.Clamp01(_hookProgress);

        // ── Slider: just set value directly (min 0 / max 1) ──
        if (progressSlider != null)
            progressSlider.value = _hookProgress;

        if (_hookProgress >= 1f)
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
        if (hookRect == null || leftPivot == null || rightPivot == null) return;

        float barLength = Vector3.Distance(leftPivot.position, rightPivot.position);
        Vector2 size = hookRect.sizeDelta;
        size.x = barLength * hookSize;
        hookRect.sizeDelta = size;
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
