using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton MonoBehaviour. Manages which grid cells have been tilled and occupied.
/// </summary>
public class TilledGroundSystem : MonoBehaviour
{
    public static TilledGroundSystem Instance { get; private set; }

    public const float CELL_SIZE = 2f;

    [SerializeField] private List<Vector2Int> preFilledTilledCells = new List<Vector2Int>();
    
    private HashSet<Vector2Int> _tilledCells = new HashSet<Vector2Int>();
    private HashSet<Vector2Int> _occupiedCells = new HashSet<Vector2Int>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Pre-fill tilled cells for testing
        foreach (var cell in preFilledTilledCells)
        {
            _tilledCells.Add(cell);
        }
    }

    public bool IsTilled(Vector2Int cell) => _tilledCells.Contains(cell);
    public bool IsOccupied(Vector2Int cell) => _occupiedCells.Contains(cell);

    public void TillCell(Vector2Int cell)
    {
        _tilledCells.Add(cell);
        Debug.Log($"Cell {cell} tilled.");
    }

    public void PlantCell(Vector2Int cell)
    {
        if (IsTilled(cell))
        {
            _occupiedCells.Add(cell);
            Debug.Log($"Cell {cell} planted.");
        }
    }

    public static Vector2Int WorldToCell(Vector3 worldPos)
    {
        int x = Mathf.FloorToInt((worldPos.x + (CELL_SIZE / 2f)) / CELL_SIZE);
        int z = Mathf.FloorToInt((worldPos.z + (CELL_SIZE / 2f)) / CELL_SIZE);
        return new Vector2Int(x, z);
    }

    public static Vector3 CellToWorld(Vector2Int cell)
    {
        return new Vector3(cell.x * CELL_SIZE, 0, cell.y * CELL_SIZE);
    }

    // --- SAVE / LOAD HOOKS ---
    public void SaveState()
    {
        // TODO: Persist _tilledCells and _occupiedCells via SaveRepository
        // Example: SaveRepository.Instance.SaveTilledData(_tilledCells, _occupiedCells);
    }

    public void LoadState()
    {
        // TODO: Restore _tilledCells and _occupiedCells via SaveRepository
    }
}
