using UnityEngine;

/// <summary>
/// Command to handle tilling a grid cell.
/// </summary>
public class TillCommand : IFarmCommand
{
    private readonly Vector2Int _cell;
    private readonly Vector3 _worldPos;

    public TillCommand(Vector2Int cell, Vector3 worldPos)
    {
        _cell = cell;
        _worldPos = worldPos;
    }

    public bool Execute()
    {
        if (TilledGroundSystem.Instance == null) return false;
        if (TilledGroundSystem.Instance.IsTilled(_cell)) return false;

        TilledGroundSystem.Instance.TillCell(_cell);
        
        // In the future, this is where we would spawn a "Tilled Earth" visual/decal
        Debug.Log($"[TillCommand] Executed at {_cell}");
        
        return true;
    }

    public void Undo()
    {
        // Undo tilling would involve removing the tilled state
    }
}
