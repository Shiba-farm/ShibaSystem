using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Moves the farming tile cursor to follow the mouse cursor,
/// grid-snapped and clamped to a square reach around the local player.
///
/// Attach to the Cursor root GameObject (the one with the Mesh Renderer).
/// Player and Camera are resolved automatically at runtime — no drag-and-drop needed.
///   • The local player is found via NetworkManager once it spawns.
///   • The camera is resolved via Camera.main each frame until one is found.
///
/// In the Inspector only set:
///   • TerrainMask — select the "Terrain" layer
///   • CellSize    — world units per grid cell   (default 1)
///   • MaxCells    — reach radius in cells        (default 4)
///
/// Other systems read state via:
///   FarmTileCursor.Instance.CellCoord   → Vector2Int grid position
///   FarmTileCursor.Instance.CellCenter  → world-space XZ center of that cell
///   FarmTileCursor.Instance.IsOnTerrain → false when mouse misses terrain
/// </summary>
public class FarmTileCursor : MonoBehaviour
{
    public static FarmTileCursor Instance { get; private set; }

    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("References")]
    [Tooltip("The shared HeldItemSignal ScriptableObject — same one wired into PlayerHeldItem.")]
    [SerializeField] private HeldItemSignal heldItemSignal;
    [SerializeField] private LayerMask terrainMask;

    [Header("Grid")]
    [Tooltip("World units per grid cell.")]
    [SerializeField] private float cellSize = 1f;

    [Tooltip("Max reach from the player in grid cells (forms a square boundary).")]
    [SerializeField] private int maxCells = 4;

    [Header("Visual")]
    [Tooltip("Small gap above the terrain surface to avoid z-fighting.")]
    [SerializeField] private float yOffset = 0.02f;

    // ── Public state ─────────────────────────────────────────────────────────

    /// <summary>The grid cell the cursor is currently hovering.</summary>
    public Vector2Int CellCoord   { get; private set; }

    /// <summary>World-space centre of the hovered cell (Y = terrain + yOffset).</summary>
    public Vector3    CellCenter  { get; private set; }

    /// <summary>True while the cursor is visible over terrain.</summary>
    public bool       IsOnTerrain { get; private set; }

    // ── Private ───────────────────────────────────────────────────────────────

    private Transform _player;       // cached local player transform
    private Camera    _cam;          // cached main camera
    private Renderer  _renderer;
    private bool      _hoeEquipped;         // true only while the local player holds a Hoe
    private bool      _seedEquipped;        // true only while the local player holds a Seed
    private bool      _wateringCanEquipped; // true only while the local player holds a Watering Can
    private bool      _harvestMode;          // true when the player holds nothing — aim at any terrain to harvest
    private bool      _farmHelperEquipped;   // true while holding any FarmHelper item (fertilizer, etc.)

    /// <summary>
    /// When true, Update skips the raycast/snap/clamp logic and the cursor stays
    /// frozen at the last computed cell.  Set by Lock() / cleared by Unlock().
    /// </summary>
    private bool _isLocked;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance  = this;
        _renderer = GetComponent<Renderer>();
    }

    private void Start()
    {
        if (heldItemSignal == null) return;
        heldItemSignal.OnChanged += OnHeldItemChanged;
        // Evaluate whatever is already held when this object first becomes active
        OnHeldItemChanged(heldItemSignal.Current);
    }

    private void OnDestroy()
    {
        if (heldItemSignal != null)
            heldItemSignal.OnChanged -= OnHeldItemChanged;
    }

    private void OnHeldItemChanged(ItemSO item)
    {
        _hoeEquipped         = item is ToolItemSO tool && tool.toolTypeAction == ToolAction.Hoe;
        _seedEquipped        = item is SeedItemSO;
        _wateringCanEquipped = item is ToolItemSO wtool && wtool.toolTypeAction == ToolAction.Water;
        _harvestMode         = item == null;   // empty hands → harvest cursor
        _farmHelperEquipped  = item is FarmHelperItemSO;

        // Hide immediately when switching to a non-farming item (e.g. a sword)
        if (!_hoeEquipped && !_seedEquipped && !_wateringCanEquipped && !_harvestMode && !_farmHelperEquipped)
            SetVisible(false);
    }

    private void Update()
    {
        // Only active while the local player has a farming tool equipped OR has empty hands
        if (!_hoeEquipped && !_seedEquipped && !_wateringCanEquipped && !_harvestMode && !_farmHelperEquipped) { SetVisible(false); return; }

        // Cursor is locked during an action animation — hold the last valid cell so that
        // OnActionImpact() always reads the cell the player originally aimed at.
        if (_isLocked) return;

        // Resolve local player once it has spawned
        if (_player == null)
        {
            var localPlayerObj = NetworkManager.Singleton?.LocalClient?.PlayerObject;
            if (localPlayerObj == null) { SetVisible(false); return; }
            _player = localPlayerObj.transform;
        }

        // Resolve camera (Camera.main is set when the player's CameraTarget spawns)
        if (_cam == null)
        {
            _cam = Camera.main;
            if (_cam == null) { SetVisible(false); return; }
        }

        if (_player == null || _cam == null) { SetVisible(false); return; }

        // 1. Ray from camera through mouse → terrain
        Ray ray = _cam.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit mouseHit, 200f, terrainMask))
        {
            SetVisible(false);
            return;
        }

        // 2. Snap mouse hit to nearest grid cell
        Vector2Int mouseCell  = ToCell(mouseHit.point);

        // 3. Clamp to player's reach (square, per-axis — preserves NW/NE/SW/SE direction)
        Vector2Int playerCell = ToCell(_player.position);
        Vector2Int cell = new Vector2Int(
            Mathf.Clamp(mouseCell.x, playerCell.x - maxCells, playerCell.x + maxCells),
            Mathf.Clamp(mouseCell.y, playerCell.y - maxCells, playerCell.y + maxCells)
        );

        // 4. Per-tool cell state filter
        // _harvestMode intentionally has no filter — it shows on any terrain cell.
        // TryHarvestServerRpc validates readiness server-side.
        if (_hoeEquipped)
        {
            // Hoe: only show on cells that have NOT been tilled yet
            bool alreadyTilled = FarmingServerManager.Instance?.IsTilled(cell) ?? false;
            if (alreadyTilled) { SetVisible(false); return; }
        }
        else if (_seedEquipped)
        {
            // Seed: only show on tilled cells that are still empty (no crop growing)
            bool tilled  = FarmingServerManager.Instance?.IsTilled(cell)  ?? false;
            bool planted = FarmingServerManager.Instance?.IsPlanted(cell) ?? false;
            if (!tilled || planted) { SetVisible(false); return; }
        }
        else if (_wateringCanEquipped)
        {
            // Watering can: show on any tilled cell — planted or bare, both are fine
            bool tilled = FarmingServerManager.Instance?.IsTilled(cell) ?? false;
            if (!tilled) { SetVisible(false); return; }
        }
        else if (_farmHelperEquipped)
        {
            // FarmHelper (fertilizer, etc.): show on any tilled cell — same as watering can.
            // The server validates the specific effect and crop state.
            bool tilled = FarmingServerManager.Instance?.IsTilled(cell) ?? false;
            if (!tilled) { SetVisible(false); return; }
        }

        // 5. Sample the actual terrain height at the snapped cell
        //    (separate downward ray so the cursor sits flush even on slopes)
        float groundY = SampleGroundY(cell, mouseHit.point.y);

        // 6. Apply
        CellCoord  = cell;
        CellCenter = CellToWorld(cell, groundY + yOffset);
        transform.position = CellCenter;
        SetVisible(true);
    }

    // ── Coordinate helpers (public — reusable by tilling / planting etc.) ────

    /// <summary>World position → grid cell (XZ plane only).</summary>
    public Vector2Int ToCell(Vector3 world)
        => new Vector2Int(
            Mathf.RoundToInt(world.x / cellSize),
            Mathf.RoundToInt(world.z / cellSize));

    /// <summary>Grid cell → world-space centre at the given Y.</summary>
    public Vector3 CellToWorld(Vector2Int cell, float y = 0f)
        => new Vector3(cell.x * cellSize, y, cell.y * cellSize);

    // ── Lock API ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Freezes the cursor at its current <see cref="CellCoord"/> / <see cref="CellCenter"/>.
    /// Call from <c>PlayerItemUser.TryUse()</c> the moment an action animation starts.
    /// While locked, <c>Update()</c> skips all raycast/snap/clamp work so the cell
    /// the player aimed at is still readable when <c>OnActionImpact()</c> fires —
    /// even if the mouse moved in the meantime.
    /// </summary>
    public void Lock()   => _isLocked = true;

    /// <summary>
    /// Releases the position lock so the cursor resumes tracking the mouse.
    /// Call from <c>PlayerItemUser.OnActionAnimationFinished()</c>.
    /// </summary>
    public void Unlock() => _isLocked = false;

    // ── Private helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Cast a short ray straight down at the snapped cell to get the terrain Y.
    /// Falls back to <paramref name="fallback"/> if nothing is hit.
    /// </summary>
    private float SampleGroundY(Vector2Int cell, float fallback)
    {
        Vector3 probeOrigin = CellToWorld(cell, fallback + 10f);
        return Physics.Raycast(probeOrigin, Vector3.down, out RaycastHit hit, 25f, terrainMask)
            ? hit.point.y
            : fallback;
    }

    private void SetVisible(bool on)
    {
        IsOnTerrain = on;
        if (_renderer != null) _renderer.enabled = on;
    }

    // ── Editor gizmo ──────────────────────────────────────────────────────────
    //  Draws the square reach boundary in the Scene view when the Cursor is selected.
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (_player == null) return;

        Vector2Int pc     = ToCell(_player.position);
        float      r      = (maxCells + 0.5f) * cellSize; // +0.5 → line at cell edges
        Vector3    center = CellToWorld(pc, _player.position.y);

        Gizmos.color = new Color(0f, 1f, 0.8f, 0.7f);
        Gizmos.DrawLine(center + new Vector3(-r, 0,  r), center + new Vector3( r, 0,  r));
        Gizmos.DrawLine(center + new Vector3( r, 0,  r), center + new Vector3( r, 0, -r));
        Gizmos.DrawLine(center + new Vector3( r, 0, -r), center + new Vector3(-r, 0, -r));
        Gizmos.DrawLine(center + new Vector3(-r, 0, -r), center + new Vector3(-r, 0,  r));
    }
#endif
}
