using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Implementation เริ่มต้นของ IEquipmentVisualApplier — Instantiate
/// WearableItemSO.visualPrefab ลงใน anchor ของช่องนั้น ๆ แล้วทำลายของเก่าออก
///
/// รองรับ หลาย anchor ต่อ 1 slot:
///   เพิ่มรายการใน Slot Anchors ที่ slot เดิมซ้ำได้เลย
///   เช่น Boots → foot.l  และ Boots → foot.r จะขึ้นทั้ง 2 ข้าง
/// </summary>
public class PlayerPreviewRig : MonoBehaviour, IEquipmentVisualApplier
{
    [System.Serializable]
    public struct SlotAnchor
    {
        public EquipSlot slot;
        public Transform anchor;
    }

    [SerializeField] private List<SlotAnchor> slotAnchors;

    // หลาย anchor ต่อ 1 slot (เช่น Boots มี foot.l + foot.r)
    private readonly Dictionary<EquipSlot, List<Transform>>   _anchorLookup  = new();
    private readonly Dictionary<EquipSlot, List<GameObject>>  _currentVisuals = new();

    private void Awake()
    {
        foreach (var entry in slotAnchors)
        {
            if (entry.anchor == null) continue;
            if (!_anchorLookup.ContainsKey(entry.slot))
                _anchorLookup[entry.slot] = new List<Transform>();
            _anchorLookup[entry.slot].Add(entry.anchor);
        }
    }

    public void ApplyVisual(EquipSlot slot, ItemSO equippedItem)
    {
        // ทำลาย visual เดิมทุกชิ้นของ slot นี้ก่อน
        if (_currentVisuals.TryGetValue(slot, out var existing))
        {
            foreach (var go in existing)
                if (go != null) Destroy(go);
            existing.Clear();
        }
        _currentVisuals.Remove(slot);

        if (equippedItem is not WearableItemSO wearable || wearable.visualPrefab == null) return;
        if (!_anchorLookup.TryGetValue(slot, out var anchors) || anchors == null) return;

        int uiPreviewLayer = LayerMask.NameToLayer("UIPreview");
        var spawned = new List<GameObject>();

        foreach (var anchor in anchors)
        {
            if (anchor == null) continue;

            GameObject visual = Instantiate(wearable.visualPrefab, anchor);
            visual.transform.localPosition    = wearable.visualPositionOffset;
            visual.transform.localEulerAngles = wearable.visualRotationOffset;
            visual.transform.localScale       = wearable.visualScale == Vector3.zero ? Vector3.one : wearable.visualScale;

            if (uiPreviewLayer >= 0)
                SetLayerRecursive(visual, uiPreviewLayer);

            spawned.Add(visual);
        }

        _currentVisuals[slot] = spawned;
    }

    private static void SetLayerRecursive(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.transform)
            SetLayerRecursive(child.gameObject, layer);
    }
}
