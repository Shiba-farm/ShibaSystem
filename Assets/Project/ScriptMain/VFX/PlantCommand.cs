using UnityEngine;

/// <summary>
/// Command to handle the planting of a crop.
/// </summary>
public class PlantCommand 
{
    // private readonly Vector2Int _cell;
    // private readonly Vector3 _worldPos;
    // private readonly ItemSO _seedItem;
    // private readonly InventoryData _inventory;

    // public PlantCommand(Vector2Int cell, Vector3 worldPos, ItemSO seedItem, InventoryData inventory)
    // {
    //     _cell = cell;
    //     _worldPos = worldPos;
    //     _seedItem = seedItem;
    //     _inventory = inventory;
    // }

    // public bool Execute()
    // {
    //     Debug.Log($"[PlantCommand] Execute — cell:{_cell}  item:{_seedItem?.itemName}");

    //     if (TilledGroundSystem.Instance == null)
    //     {
    //         Debug.LogWarning("[PlantCommand] TilledGroundSystem.Instance is null!");
    //         return false;
    //     }

    //     if (TilledGroundSystem.Instance.IsOccupied(_cell))
    //     {
    //         Debug.Log($"[PlantCommand] Cell {_cell} is already occupied.");
    //         return false;
    //     }

    //     // Seed must carry a CropSO
    //     if (_seedItem is not SeedItemSO seed || seed.crop == null)
    //     {
    //         Debug.LogWarning($"[PlantCommand] '{_seedItem?.itemName}' is not a SeedItemSO or has no CropSO assigned. " +
    //                          $"Make sure the item asset is a SeedItemSO and its Crop field is set.");
    //         return false;
    //     }

    //     Debug.Log($"[PlantCommand] Seed OK — crop: {seed.crop.cropName}, growthPrefabs: {seed.crop.growthPrefabs?.Length}");

    //     // Use the direct registry — no physics, no layer guessing
    //     if (HoeTillingSystem.Instance == null)
    //     {
    //         Debug.LogWarning("[PlantCommand] HoeTillingSystem.Instance is null!");
    //         return false;
    //     }

    //     SoilTile tile = HoeTillingSystem.Instance.GetSoilTileAt(_cell);
    //     if (tile == null)
    //     {
    //         Debug.LogWarning($"[PlantCommand] No SoilTile registered at cell {_cell}. " +
    //                          $"Dig the soil there first (registry has {_cell} missing).");
    //         return false;
    //     }

    //     Debug.Log($"[PlantCommand] Found tile '{tile.name}' — isTilled:{tile.isTilled}  crop:{tile.crop?.cropName ?? "null"}");

    //     if (!tile.CanPlant(seed.crop))
    //     {
    //         Debug.Log($"[PlantCommand] tile.CanPlant returned false — isTilled:{tile.isTilled}, cropNull:{tile.crop == null}");
    //         return false;
    //     }

    //     // Plant — this calls SpawnCropStage which instantiates growthPrefabs[0]
    //     tile.Plant(seed.crop);

    //     // Mark occupied
    //     TilledGroundSystem.Instance.PlantCell(_cell);

    //     // Inventory removal is handled by PlayerItemUser.ConsumeSeedServerRpc (NetworkList is server-only)
    //     Debug.Log($"[PlantCommand] SUCCESS — planted {seed.crop.cropName} at cell {_cell}");
    //     return true;
    // }

    // public void Undo()
    // {
    //     // Undo planting requires removing the crop instance and restoring inventory
    //     Debug.LogWarning("[PlantCommand] Undo not fully implemented.");
    // }
}
