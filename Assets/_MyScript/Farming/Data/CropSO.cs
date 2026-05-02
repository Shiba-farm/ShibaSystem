using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(menuName = "Farm/Crop")]
public class CropSO : SerializedScriptableObject
{
    [BoxGroup("Info"), LabelWidth(80)]
    public string cropName;

    [BoxGroup("Info"), PreviewField(64), HideLabel]
    public Sprite icon;

    [BoxGroup("Growth"), TableList]
    [InfoBox("��Ҵ�ͧ growthPrefabs ��� stageDurations �����ҡѹ")]
    public GameObject[] growthPrefabs;

    [BoxGroup("Growth")]
    [Tooltip("เวลาแต่ละ stage เป็น 'ชั่วโมงในเกม' (เช่น 6 = 6 ชม.ในเกม, 24 = 1 วันในเกม)")]
    public float[] stageDurations;

    [BoxGroup("Growth")]
    public bool requiresWaterEachStage = true;

    [BoxGroup("Harvest"), InlineEditor]
    public ItemSO harvestItem;

    [BoxGroup("Harvest"), LabelWidth(80)]
    public Vector2Int yieldRange = new Vector2Int(1, 1);

    [BoxGroup("Harvest")]
    public bool destroyOnHarvest = true;
}
