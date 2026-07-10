using UnityEngine;

/// <summary>
/// Controls the visual cursor for planting, handles grid snapping, and triggers planting commands.
/// </summary>
public class PlantingCursorController : MonoBehaviour
{
    public static PlantingCursorController Instance { get; private set; }

    [Header("Signals")]
    [SerializeField] private HeldItemSignal heldItemSignal;
    [SerializeField] private InventoryDataSignal inventorySignal;

    [Header("Settings")]
    [SerializeField] private LayerMask terrainLayer;
    [SerializeField] private float yOffset = 0.05f;

    private Renderer _cursorRenderer;
    private MaterialPropertyBlock _propBlock;
    private ItemSO _currentItem;
    private bool _isActive;
    private bool _debugMode;

    private Vector3 _lastMousePos;
    private Vector2Int _currentCell;
    private bool _isValidPosition;

    private void Awake()
    {
        Instance = this;

        _cursorRenderer = GetComponent<Renderer>();
        if (_cursorRenderer == null) _cursorRenderer = GetComponentInChildren<Renderer>();

        _propBlock = new MaterialPropertyBlock();

        // Initial state: Hide the mesh, but keep the script alive to listen for signals
        if (_cursorRenderer != null) _cursorRenderer.enabled = false;
    }

    public void EnableDebugMode()
    {
        _isActive = true;
        _debugMode = true;
        if (_cursorRenderer != null) _cursorRenderer.enabled = true;
        Debug.Log("[PlantingCursor] Debug Mode Enabled: Ignoring Item Requirements.");
    }

    private void OnEnable()
    {
        if (heldItemSignal != null)
        {
            heldItemSignal.OnChanged += HandleItemChanged;
            HandleItemChanged(heldItemSignal.Current);
        }
        else
        {
            Debug.LogError("[PlantingCursor] HeldItemSignal is missing! Please assign it in the Inspector.");
        }
    }

    private void OnDisable()
    {
        if (heldItemSignal != null)
        {
            heldItemSignal.OnChanged -= HandleItemChanged;
        }
    }

    private void HandleItemChanged(ItemSO item)
    {
        if (_debugMode) return; // Ignore if in debug mode

        _currentItem = item;
        _isActive = item != null && item.category == ItemCategory.Seed;
        
        Debug.Log($"[PlantingCursor] Item Changed: {(item != null ? item.itemName : "None")} | Category: {(item != null ? item.category.ToString() : "None")} | Active: {_isActive}");
        
        if (_cursorRenderer != null) _cursorRenderer.enabled = _isActive;
    }

    private void Update()
    {
        if (!_isActive) return;
        UpdateCursorPosition();
        // Planting is triggered by PlayerItemUser.OnActionImpact → TryPlant()
        // No raw mouse input here so it goes through the animation system properly
    }

    private void UpdateCursorPosition()
    {
        Vector3 mousePos = Input.mousePosition;
        if (Camera.main == null) return;

        Ray ray = Camera.main.ScreenPointToRay(mousePos);
        
        // Always draw the ray in Scene view for debugging
        Debug.DrawRay(ray.origin, ray.direction * 100f, Color.red);

        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, terrainLayer))
        {
            _currentCell = TilledGroundSystem.WorldToCell(hit.point);
            Vector3 snappedPos = TilledGroundSystem.CellToWorld(_currentCell);
            
            snappedPos.y = hit.point.y + yOffset; 
            transform.position = snappedPos;

            _isValidPosition = TilledGroundSystem.Instance != null && 
                              TilledGroundSystem.Instance.IsTilled(_currentCell) && 
                              !TilledGroundSystem.Instance.IsOccupied(_currentCell);

            UpdateVisuals();
        }
        else
        {
            // --- DETAILED RAYCAST DEBUG ---
            // If we missed the terrainLayer, check if we hit ANYTHING at all
            if (Physics.Raycast(ray, out RaycastHit hitAny, 1000f))
            {
                Debug.LogWarning($"[PlantingCursor] Ray hit '{hitAny.collider.name}' on Layer '{LayerMask.LayerToName(hitAny.collider.gameObject.layer)}', but your TerrainLayer mask doesn't include it. Update your Inspector settings!");
            }

            if (_debugMode)
            {
                // In debug mode, we just follow the mouse at a fixed distance if we hit nothing
                transform.position = ray.GetPoint(10f);
                if (_cursorRenderer != null) _cursorRenderer.enabled = true;
            }
            else
            {
                if (_cursorRenderer != null) _cursorRenderer.enabled = false;
            }
        }
    }

    private void UpdateVisuals()
    {
        if (_cursorRenderer == null) return;
        if (!_cursorRenderer.enabled) _cursorRenderer.enabled = true;

        Color targetColor = _isValidPosition ? 
            new Color(0.4f, 1f, 0.4f, 0.5f) : 
            new Color(1f, 0.3f, 0.3f, 0.5f);

        _cursorRenderer.GetPropertyBlock(_propBlock);
        _propBlock.SetColor("_BaseColor", targetColor);
        _cursorRenderer.SetPropertyBlock(_propBlock);
    }

    public bool TryPlant()
    {
        Debug.Log($"[PlantingCursor] TryPlant — isActive:{_isActive}  isValid:{_isValidPosition}  cell:{_currentCell}  item:{_currentItem?.itemName}");

        if (_debugMode) { Debug.Log("[PlantingCursor] Debug mode is ON — planting skipped."); return false; }

        if (!_isValidPosition)
        {
            bool tilled   = TilledGroundSystem.Instance != null && TilledGroundSystem.Instance.IsTilled(_currentCell);
            bool occupied = TilledGroundSystem.Instance != null && TilledGroundSystem.Instance.IsOccupied(_currentCell);
            Debug.Log($"[PlantingCursor] Cannot plant — tilled:{tilled}  occupied:{occupied}  cell:{_currentCell}");
            return false;
        }

        InventoryData invData = inventorySignal != null ? inventorySignal.CurrentData : null;
        Debug.Log($"[PlantingCursor] inventorySignal:{inventorySignal != null}  invData:{invData != null}");

        // PlantCommand plantCmd = new PlantCommand(_currentCell, transform.position, _currentItem, invData);
        // if (plantCmd.Execute())
        // {
        //     RotatePlayerToPlantPosition();
        //     return true;
        // }
        return false;
    }

    private void RotatePlayerToPlantPosition()
    {
        var player = FindObjectOfType<PlayerController>(); 
        if (player != null && player.IsOwner)
        {
            player.FaceTo(transform.position);
        }
    }
}
