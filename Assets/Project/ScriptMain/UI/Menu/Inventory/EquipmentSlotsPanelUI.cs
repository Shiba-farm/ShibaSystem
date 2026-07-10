using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// สร้างช่อง EquipmentSlotUI ทั้งหมดจาก EquipmentSlotConfigSO ตอน runtime แล้วผูกกับ
/// EquipmentDataSignal ของผู้เล่น local — เพิ่มช่องใหม่ในอนาคตแค่แก้ asset config
/// ไม่ต้องแตะไฟล์นี้เลย
/// </summary>
public class EquipmentSlotsPanelUI : MonoBehaviour
{
    [SerializeField] private EquipmentDataSignal connectionSignal;
    [SerializeField] private EquipmentSlotConfigSO slotConfig;
    [SerializeField] private EquipmentSlotUI slotPrefab;
    [SerializeField] private Transform container;

    private readonly List<EquipmentSlotUI> _slots = new();
    private EquipmentData _activeData;
    private bool _needsRefresh;

    private void OnEnable()
    {
        BuildSlotsIfNeeded();
        connectionSignal.OnDataUpdate += HandleConnected;
        if (connectionSignal.CurrentData != null) HandleConnected(connectionSignal.CurrentData);
    }

    private void OnDisable()
    {
        connectionSignal.OnDataUpdate -= HandleConnected;
        if (_activeData != null) _activeData.EquippedItems.OnListChanged -= HandleListChanged;
        // null ออกเพื่อให้ OnEnable ครั้งถัดไป re-subscribe OnListChanged ใหม่
        // ถ้าไม่ null HandleConnected จะ return early (if _activeData == data)
        // ทำให้ปิด-เปิด inventory แล้ว slot icon ไม่ update หลัง unequip
        _activeData = null;
    }

    private void BuildSlotsIfNeeded()
    {
        if (_slots.Count > 0 || slotConfig == null) return;

        foreach (var entry in slotConfig.slots)
        {
            EquipmentSlotUI slot = Instantiate(slotPrefab, container);
            slot.Setup(entry.slot, entry.displayName, entry.emptyIcon);
            _slots.Add(slot);
        }
    }

    private void HandleConnected(EquipmentData data)
    {
        if (_activeData == data) return;
        if (_activeData != null) _activeData.EquippedItems.OnListChanged -= HandleListChanged;

        _activeData = data;
        if (_activeData == null) return;

        _activeData.EquippedItems.OnListChanged += HandleListChanged;
        RefreshAll();
    }

    private void HandleListChanged(NetworkListEvent<NetworkEquipment> evt) => _needsRefresh = true;

    private void LateUpdate()
    {
        if (!_needsRefresh) return;
        _needsRefresh = false;
        RefreshAll();
    }

    private void RefreshAll()
    {
        if (_activeData == null) return;

        foreach (var slot in _slots)
        {
            int itemId = _activeData.GetEquippedItemId(slot.Slot);
            ItemSO item = itemId != 0 ? GameDataManager.Instance.itemDatabases.GetItemByID(itemId) : null;
            slot.Refresh(item);
        }
    }
}
