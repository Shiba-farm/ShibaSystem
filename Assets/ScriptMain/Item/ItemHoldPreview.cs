using UnityEngine;

#if UNITY_EDITOR
public class ItemHoldPreview : MonoBehaviour
{
    [SerializeField] public ItemSO previewItem;
    [SerializeField] public HoldState previewState = HoldState.Idle;
    [HideInInspector] public GameObject previewInstance;

    public void ApplyOffsets()
    {
        if (previewInstance == null || previewItem == null) return;

        var hold = previewItem.GetHoldPosition(previewState);
        if (hold == null) return;

        previewInstance.transform.localPosition    = hold.positionOffset;
        previewInstance.transform.localEulerAngles = hold.rotationOffset;
        // previewInstance.transform.localScale    = previewItem.holdScale;
    }

    public void SpawnPreview()
    {
        ClearPreview();
        if (previewItem?.equipmentPrefab == null) return;

        previewInstance = Instantiate(previewItem.equipmentPrefab, transform);
        previewInstance.hideFlags = HideFlags.DontSave;
        ApplyOffsets();
    }

    private void OnValidate()
    {
        ApplyOffsets();
    }

    public void ClearPreview()
    {
        if (previewInstance != null)
        {
            DestroyImmediate(previewInstance);
            previewInstance = null;
        }
    }

    private void OnDestroy() => ClearPreview();
}
#endif