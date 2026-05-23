using UnityEngine;

/// <summary>
/// Event struct published when a crop is successfully planted.
/// </summary>
public readonly struct CropPlantedEvent
{
    public readonly Vector2Int GridCell;
    public readonly Vector3 WorldPosition;
    public readonly ItemSO SeedItem;

    public CropPlantedEvent(Vector2Int gridCell, Vector3 worldPosition, ItemSO seedItem)
    {
        GridCell = gridCell;
        WorldPosition = worldPosition;
        SeedItem = seedItem;
    }
}
