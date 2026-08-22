using UnityEngine;

[CreateAssetMenu(menuName = "Farm/Crop")]
public class CropSO : ScriptableObject
{
    [Header("Info")]
    public string cropName;

    public Sprite icon;

    [Header("Growth")]
    // growthPrefabs and stageDurations must be the same length (stage[i] uses growthPrefabs[i]).
    public GameObject[] growthPrefabs;

    [Tooltip("เวลาแต่ละ stage เป็น 'ชั่วโมงในเกม' (เช่น 6 = 6 ชม.ในเกม, 24 = 1 วันในเกม)")]
    public float[] stageDurations;

    public bool requiresWaterEachStage = true;

    [Header("Harvest")]
    public ItemSO harvestItem;

    public Vector2Int yieldRange = new Vector2Int(1, 1);

    public bool destroyOnHarvest = true;
}
