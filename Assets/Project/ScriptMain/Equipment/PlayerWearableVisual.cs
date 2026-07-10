using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// ติด component นี้บน Player prefab — apply WearableItemSO.visualPrefab ไปยัง
/// bone anchors ของโมเดลจริงในเกม ทำงานถูกต้องทั้ง Local และ Remote players
///
/// การทำงาน (Multiplayer-safe):
///   EquipmentData.OnNetworkSpawn() เรียก ConnectToData(this) โดยตรง
///   แทนที่จะผ่าน EquipmentDataSignal (SO ที่ shared กัน)
///   ทำให้ player แต่ละคนใช้ข้อมูลของตัวเองอย่างอิสระ
///
/// Setup ใน Inspector:
///   Slot Anchors → ผูก EquipSlot กับ bone Transform
///   (เพิ่ม Boots 2 แถวสำหรับ foot.l และ foot.r)
///   *** ไม่ต้องลาก EquipmentDataSignal *** (ลบ field นั้นออกแล้ว)
/// </summary>
public class PlayerWearableVisual : MonoBehaviour
{
    [System.Serializable]
    public struct SlotAnchor
    {
        public EquipSlot slot;
        public Transform anchor;
    }

    [Header("Bone Anchors (เพิ่มซ้ำได้ — เช่น Boots 2 แถวสำหรับซ้าย/ขวา)")]
    [SerializeField] private List<SlotAnchor> slotAnchors;

    private readonly Dictionary<EquipSlot, List<Transform>>  _anchorLookup  = new();
    private readonly Dictionary<EquipSlot, List<GameObject>> _currentVisuals = new();
    private EquipmentData _activeData;
    private bool _needsRefresh;

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

    private void OnEnable()
    {
        if (_activeData != null) return;
        var data = GetComponentInParent<EquipmentData>();
        if (data != null) ConnectToData(data);
    }

    private void OnDestroy()
    {
        DisconnectData();
    }

    // ─── Public API (called by EquipmentData.OnNetworkSpawn/Despawn) ──────────

    /// <summary>
    /// เรียกโดย EquipmentData.OnNetworkSpawn() โดยตรง
    /// ทำให้ visual ของ player แต่ละคนผูกกับข้อมูลของตัวเองอย่างอิสระ
    /// ไม่ต้องผ่าน EquipmentDataSignal ที่เป็น shared SO
    /// </summary>
    public void ConnectToData(EquipmentData data)
    {
        if (_activeData == data) return;

        DisconnectData(); // ตัด connection เดิมก่อน (ถ้ามี)

        _activeData = data;
        _activeData.EquippedItems.OnListChanged += HandleListChanged;
        _needsRefresh = true;
    }

    /// <summary>เรียกโดย EquipmentData.OnNetworkDespawn()</summary>
    public void DisconnectData()
    {
        if (_activeData == null) return;
        _activeData.EquippedItems.OnListChanged -= HandleListChanged;
        _activeData = null;

        // ทำลาย visual ทั้งหมดเมื่อ player despawn
        ClearAllVisuals();
    }

    // ─── Internal ────────────────────────────────────────────────────────────

    private void HandleListChanged(NetworkListEvent<NetworkEquipment> evt) => _needsRefresh = true;

    private void LateUpdate()
    {
        if (!_needsRefresh || _activeData == null) return;
        _needsRefresh = false;
        RefreshAll();
    }

    private void RefreshAll()
    {
        foreach (var entry in _activeData.EquippedItems)
        {
            ItemSO item = entry.ItemID != 0
                ? GameDataManager.Instance.itemDatabases.GetItemByID(entry.ItemID)
                : null;
            ApplyVisual(entry.Slot, item);
        }
    }

    private void ApplyVisual(EquipSlot slot, ItemSO equippedItem)
    {
        // ทำลาย visual เดิมทุกชิ้นของ slot นี้
        if (_currentVisuals.TryGetValue(slot, out var existing))
        {
            foreach (var go in existing)
                if (go != null) Destroy(go);
            existing.Clear();
        }
        _currentVisuals.Remove(slot);

        if (equippedItem is not WearableItemSO wearable || wearable.visualPrefab == null) return;

        var anchors = GetAnchors(slot);
        if (anchors == null || anchors.Count == 0)
        {
            Debug.LogWarning($"[PlayerWearableVisual] ไม่มี anchor สำหรับ slot {slot}");
            return;
        }

        var spawned = new List<GameObject>();
        foreach (var anchor in anchors)
        {
            if (anchor == null) continue;

            var visual = Instantiate(wearable.visualPrefab, anchor);
            visual.transform.localPosition    = wearable.visualPositionOffset;
            visual.transform.localEulerAngles = wearable.visualRotationOffset;
            visual.transform.localScale       = wearable.visualScale == Vector3.zero ? Vector3.one : wearable.visualScale;

            // บังคับ layer ให้ตรงกับ Player model (ป้องกัน main camera cull ออก)
            SetLayerRecursive(visual, gameObject.layer);
            spawned.Add(visual);
        }

        _currentVisuals[slot] = spawned;
    }

    private void ClearAllVisuals()
    {
        foreach (var list in _currentVisuals.Values)
            foreach (var go in list)
                if (go != null) Destroy(go);
        _currentVisuals.Clear();
    }

    private static void SetLayerRecursive(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.transform)
            SetLayerRecursive(child.gameObject, layer);
    }

    private List<Transform> GetAnchors(EquipSlot slot)
    {
        if (_anchorLookup.TryGetValue(slot, out var anchors) && anchors.Count > 0)
            return anchors;

        // Editor Mode fallback
        var result = new List<Transform>();
        foreach (var entry in slotAnchors)
            if (entry.slot == slot && entry.anchor != null)
                result.Add(entry.anchor);
        return result;
    }

    /// <summary>คืน anchor แรกของ slot — ใช้โดย EquipmentPreviewWindow</summary>
    public Transform GetAnchor(EquipSlot slot)
    {
        var anchors = GetAnchors(slot);
        return anchors.Count > 0 ? anchors[0] : null;
    }
}
