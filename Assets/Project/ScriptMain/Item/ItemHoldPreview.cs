using UnityEngine;

#if UNITY_EDITOR
// ─────────────────────────────────────────────────────────────────────────────
// Bridge: lets ItemSO.OnValidate (runtime assembly) notify
//         EquipmentPreviewWindow (editor assembly) without a direct reference.
// ─────────────────────────────────────────────────────────────────────────────
public static class ItemPreviewBridge
{
    /// <summary>
    /// Subscribed by EquipmentPreviewWindow.
    /// Fired by ItemSO.OnValidate when the SO is modified in the Inspector.
    /// </summary>
    public static System.Action<ItemSO> OnItemSOChanged;
}

// ─────────────────────────────────────────────────────────────────────────────
// Legacy component — superseded by the Equipment Preview window.
// Keep it so existing scene references don't break.
// Open: Tools > Shiba Farm > Equipment Preview
// ─────────────────────────────────────────────────────────────────────────────
[System.Obsolete("Use the Equipment Preview window instead: Tools > Shiba Farm > Equipment Preview")]
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
        previewInstance.transform.localScale =
            hold.localScale == Vector3.zero ? Vector3.one : hold.localScale;
    }

    public void SpawnPreview()
    {
        ClearPreview();
        if (previewItem?.equipmentPrefab == null) return;
        previewInstance = Instantiate(previewItem.equipmentPrefab, transform);
        previewInstance.hideFlags = HideFlags.DontSave;
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

    private void OnValidate() => ApplyOffsets();
    private void OnDestroy()  => ClearPreview();
}
#endif
