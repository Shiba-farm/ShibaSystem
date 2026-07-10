using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays a "+N [icon]" pickup notification near the local player whenever they successfully
/// harvest a crop.
///
/// Setup:
///   1. Attach this to the popup panel GameObject that already exists in the Canvas.
///   2. Wire itemIcon (Image) and amountText (TMP_Text) in the Inspector.
///   3. CanvasGroup and RectTransform are resolved automatically from this GameObject,
///      or you can assign them explicitly.
///   4. Tune screenOffset and the timing values to taste.
///   5. The panel should NOT be inside a Layout Group — position is set directly.
///
/// Signal chain:
///   FarmingServerManager.TryHarvestServerRpc succeeds
///   → NotifyHarvestClientRpc (targeted to harvesting client only)
///   → FarmingServerManager.OnHarvestNotification static event
///   → OnHarvest() here reacts
///
/// Accumulation rule:
///   If the same itemId comes in while the popup is still visible, the amount is
///   added and the hold timer resets — no extra flash.
///   If a different itemId arrives (or the popup has already faded), a fresh popup starts.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class HarvestPopupUI : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("References")]
    [Tooltip("Image that shows the harvested item's icon.")]
    [SerializeField] private Image         itemIcon;

    [Tooltip("TMP text showing '+N'.")]
    [SerializeField] private TMP_Text      amountText;

    [Tooltip("CanvasGroup used to fade the panel. Auto-resolved from this GameObject.")]
    [SerializeField] private CanvasGroup   canvasGroup;

    [Tooltip("RectTransform to reposition each frame. Auto-resolved from this GameObject.")]
    [SerializeField] private RectTransform panelRect;

    [Header("Positioning")]
    [Tooltip("Screen-pixel offset from the player's screen position. " +
             "Positive X → right,  positive Y → up.")]
    [SerializeField] private Vector2 screenOffset = new Vector2(80f, 120f);

    [Header("Animation")]
    [Tooltip("Time to scale from zero up to full size (the pop-in).")]
    [SerializeField] private float popInDuration  = 0.2f;

    [Tooltip("How long the popup stays at full size before popping back.")]
    [SerializeField] private float holdDuration   = 1.0f;

    [Tooltip("Time to scale back down to zero (the pop-back).")]
    [SerializeField] private float popOutDuration = 0.15f;

    [Tooltip("Peak scale during pop-in overshoot. 1.2 = 20 % bigger than final before settling.")]
    [SerializeField] private float overshootScale = 1.2f;

    // ── Private ───────────────────────────────────────────────────────────────

    private int       _currentItemId;      // itemId currently shown (-1 = nothing)
    private int       _accumulatedAmount;  // total shown in the current popup cycle
    private Coroutine _displayRoutine;

    private Camera    _cam;
    private Transform _playerTransform;
    private Canvas    _canvas;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        if (panelRect   == null) panelRect   = GetComponent<RectTransform>();
        _canvas = GetComponentInParent<Canvas>();

        // Start fully hidden — invisible and scaled to zero so it occupies no space
        canvasGroup.alpha          = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable   = false;
        panelRect.localScale       = Vector3.zero;

        _currentItemId = -1;
    }

    private void OnEnable()  => FarmingServerManager.OnHarvestNotification += OnHarvest;
    private void OnDisable() => FarmingServerManager.OnHarvestNotification -= OnHarvest;

    private void Update()
    {
        // Only follow the player while the popup is actually visible
        if (canvasGroup.alpha <= 0f) return;
        TrackPlayer();
    }

    // ── Signal handler ────────────────────────────────────────────────────────

    private void OnHarvest(int itemId, int amount)
    {
        if (_currentItemId == itemId && canvasGroup.alpha > 0f)
        {
            // Same item while still showing — accumulate and restart hold timer only
            _accumulatedAmount += amount;
        }
        else
        {
            // Different item, or popup has already faded — fresh start
            _currentItemId     = itemId;
            _accumulatedAmount = amount;

            var itemData = GameDataManager.Instance?.itemDatabases?.GetItemByID(itemId);
            if (itemIcon != null) itemIcon.sprite = itemData?.icon;
        }

        if (amountText != null) amountText.text = $"+{_accumulatedAmount}";

        // Always (re)start the full animation — resets the hold timer on accumulation too
        if (_displayRoutine != null) StopCoroutine(_displayRoutine);
        _displayRoutine = StartCoroutine(DisplaySequence());
    }

    // ── Animation ─────────────────────────────────────────────────────────────

    private IEnumerator DisplaySequence()
    {
        // Make visible immediately — scale drives the animation, not alpha
        canvasGroup.alpha    = 1f;
        panelRect.localScale = Vector3.zero;

        // 1. Pop in: scale 0 → overshoot → 1
        yield return PopIn();

        // 2. Hold at full size
        yield return new WaitForSeconds(holdDuration);

        // 3. Pop back: scale 1 → 0
        yield return PopOut();

        // Cleanup
        canvasGroup.alpha    = 0f;
        panelRect.localScale = Vector3.zero;
        _currentItemId       = -1;
        _displayRoutine      = null;
    }

    /// <summary>
    /// Scales from 0 → overshootScale → 1 for a punchy feel.
    /// Phase 1 (first 60 % of duration): rise to overshoot peak.
    /// Phase 2 (last 40 % of duration):  settle back to exactly 1.
    /// </summary>
    private IEnumerator PopIn()
    {
        float elapsed = 0f;
        while (elapsed < popInDuration)
        {
            elapsed += Time.deltaTime;
            float t     = Mathf.Clamp01(elapsed / popInDuration);
            float scale = t < 0.6f
                ? Mathf.Lerp(0f,            overshootScale, t / 0.6f)
                : Mathf.Lerp(overshootScale, 1f,            (t - 0.6f) / 0.4f);
            panelRect.localScale = Vector3.one * scale;
            yield return null;
        }
        panelRect.localScale = Vector3.one;
    }

    /// <summary>
    /// Scales from 1 → 0 with a smooth ease-in so the dismiss feels snappy.
    /// </summary>
    private IEnumerator PopOut()
    {
        float elapsed = 0f;
        while (elapsed < popOutDuration)
        {
            elapsed += Time.deltaTime;
            float t     = Mathf.Clamp01(elapsed / popOutDuration);
            float scale = 1f - Mathf.SmoothStep(0f, 1f, t);
            panelRect.localScale = Vector3.one * scale;
            yield return null;
        }
        panelRect.localScale = Vector3.zero;
    }

    // ── Player tracking ───────────────────────────────────────────────────────

    /// <summary>
    /// Repositions the popup each frame to stay near the local player's screen position.
    /// Works for both Screen Space — Overlay and Screen Space — Camera canvas modes.
    /// </summary>
    private void TrackPlayer()
    {
        // Resolve camera once
        if (_cam == null) _cam = Camera.main;
        if (_cam == null) return;

        // Resolve local player once (it spawns after Awake, so we can't cache in Awake)
        if (_playerTransform == null)
        {
            var playerObj = NetworkManager.Singleton?.LocalClient?.PlayerObject;
            if (playerObj == null) return;
            _playerTransform = playerObj.transform;
        }

        if (_canvas == null) return;

        // Project the player's world position into screen space
        Vector3 screenPos = _cam.WorldToScreenPoint(_playerTransform.position);
        if (screenPos.z < 0f) return;   // player is behind the camera — skip

        // Screen-space target: player position + configured offset
        Vector2 targetScreen = new Vector2(screenPos.x, screenPos.y) + screenOffset;

        // Position the panel — method differs by canvas render mode
        if (_canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            // Overlay: world space == screen space, so assign directly
            panelRect.position = new Vector3(targetScreen.x, targetScreen.y, 0f);
        }
        else
        {
            // Screen Space — Camera: convert through the canvas rect
            Camera uiCam = _canvas.worldCamera;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    (RectTransform)_canvas.transform, targetScreen, uiCam, out Vector2 localPos))
            {
                panelRect.position = _canvas.transform.TransformPoint(
                    new Vector3(localPos.x, localPos.y, 0f));
            }
        }
    }
}
