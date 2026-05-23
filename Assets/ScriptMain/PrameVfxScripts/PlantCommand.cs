using UnityEngine;

/// <summary>
/// Command to handle the planting of a crop.
/// </summary>
public class PlantCommand : IFarmCommand
{
    private readonly Vector2Int _cell;
    private readonly Vector3 _worldPos;
    private readonly ItemSO _seedItem;
    private readonly InventoryData _inventory;

    public PlantCommand(Vector2Int cell, Vector3 worldPos, ItemSO seedItem, InventoryData inventory)
    {
        _cell = cell;
        _worldPos = worldPos;
        _seedItem = seedItem;
        _inventory = inventory;
    }

    public bool Execute()
    {
        if (TilledGroundSystem.Instance == null) return false;
        if (TilledGroundSystem.Instance.IsOccupied(_cell)) return false;

        // 1. Mark as occupied
        TilledGroundSystem.Instance.PlantCell(_cell);

        // 2. Remove item from inventory
        if (_inventory != null)
        {
            _inventory.RemoveItem(_seedItem.itemID, 1);
        }

        // 3. Spawn placeholder crop (Green Sphere)
        GameObject placeholder = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        placeholder.name = $"Crop_{_seedItem.itemName}_{_cell}";
        placeholder.transform.position = _worldPos;
        placeholder.transform.localScale = Vector3.one * 0.5f;
        placeholder.GetComponent<Renderer>().material.color = Color.green;

        // 4. Publish event
        // Note: Using a placeholder event publication since EventBus implementation varies.
        // If a central EventBus exists, it should be called here.
        Debug.Log($"[PlantCommand] Published CropPlantedEvent for cell {_cell}");
        
        // TODO: Publish via real EventBus if available
        // EventBus.Publish(new CropPlantedEvent(_cell, _worldPos, _seedItem));

        return true;
    }

    public void Undo()
    {
        // Undo planting requires removing the crop instance and restoring inventory
        Debug.LogWarning("[PlantCommand] Undo not fully implemented.");
    }
}
