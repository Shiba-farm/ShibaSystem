using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class InventoryMainUIs : MonoBehaviour
{
    [SerializeField] protected InventoryDataSignal connectionSignal;
    [SerializeField] protected int inventoryID;
    [Tooltip("ถ้า >= 0: ค่านี้จะถูกใส่ใน slot.inventoryID แทน inventoryID\n" +
             "ใช้เมื่อ panel ต้องการ UI registry ID แยกจาก data ID\n" +
             "เช่น HotbarInPanelUIs: inventoryID=10 (unique), dataInventoryID=1 (hotbar)")]
    [SerializeField] private int dataInventoryID = -1;
    [SerializeField] protected InventorySlotUIs slotPrefab;
    [SerializeField] protected bool onlyShowUnEmptySlots = false;

    [Header("Interaction")]
    [SerializeField] protected SlotInteractionMode interactionMode = SlotInteractionMode.DragDrop;

    protected InventorySlotUIs[] allSlots;
    protected int[] slotToInventoryIndex;
    public InventoryData activeData { get; private set; }

    /// <summary>inventoryID ที่ slot จะใช้สำหรับ drag-drop transfer (อาจต่างจาก panel inventoryID)</summary>
    protected int SlotDataID => dataInventoryID >= 0 ? dataInventoryID : inventoryID;

    // ── Dirty flags ──────────────────────────────────────────────────────────
    private bool _needsRebuild = false;
    private bool _needsRefresh = false;

    // ── Lifecycle ────────────────────────────────────────────────────────────
    protected virtual void OnEnable()
    {
        InventoryUIRegistry.Register(inventoryID, this);
        connectionSignal.OnDataUpdate += HandleInventoryConnected;
        if (connectionSignal.CurrentData != null)
            HandleInventoryConnected(connectionSignal.CurrentData);
    }

    protected virtual void OnDisable()
    {
        InventoryUIRegistry.Unregister(inventoryID, this);
        connectionSignal.OnDataUpdate -= HandleInventoryConnected;

        if (activeData != null)
        {
            activeData.InventoryItems.OnListChanged -= OnNetworkListChanged;
            activeData = null;
        }
    }

    // ── Data binding ─────────────────────────────────────────────────────────
    private void HandleInventoryConnected(InventoryData data)
    {
        if (data == null)
        {
            ClearAllSlots();
            activeData = null;
            return;
        }

        if (activeData == data) return;
        if (activeData != null)
            activeData.InventoryItems.OnListChanged -= OnNetworkListChanged;

        activeData = data;
        activeData.InventoryItems.OnListChanged += OnNetworkListChanged;

        PopulateSlots(activeData);
        RefreshAllSlots();
    }

    // ── Network change handler ───────────────────────────────────────────────
    private void OnNetworkListChanged(NetworkListEvent<NetworkItems> changeEvent)
    {
        bool structureChanged =
            changeEvent.Type == NetworkListEvent<NetworkItems>.EventType.Add     ||
            changeEvent.Type == NetworkListEvent<NetworkItems>.EventType.Remove  ||
            changeEvent.Type == NetworkListEvent<NetworkItems>.EventType.RemoveAt||
            changeEvent.Type == NetworkListEvent<NetworkItems>.EventType.Insert  ||
            changeEvent.Type == NetworkListEvent<NetworkItems>.EventType.Clear;

        if (onlyShowUnEmptySlots && structureChanged)
            _needsRebuild = true;
        else
            _needsRefresh = true;
    }

    // ── LateUpdate: ล้าง dirty flags ────────────────────────────────────────
    private void LateUpdate()
    {
        if (activeData == null) return;

        if (_needsRebuild)
        {
            PopulateSlots(activeData);
            RefreshAllSlots();
            _needsRebuild = false;
            _needsRefresh = false;
        }
        else if (_needsRefresh)
        {
            RefreshAllSlots();
            _needsRefresh = false;
        }
    }

    // ── Slot construction (Destroy + Instantiate) ────────────────────────────
    private void PopulateSlots(InventoryData data)
    {
        if (allSlots != null)
        {
            foreach (var slot in allSlots)
                if (slot != null) Destroy(slot.gameObject);
        }

        var itemsToShow = new List<(NetworkItems item, int originalIndex)>();
        for (int i = 0; i < data.InventoryItems.Count; i++)
        {
            NetworkItems item = data.InventoryItems[i];
            if (onlyShowUnEmptySlots && item.ItemID == 0) continue;
            itemsToShow.Add((item, i));
        }

        allSlots             = new InventorySlotUIs[itemsToShow.Count];
        slotToInventoryIndex = new int[itemsToShow.Count];

        for (int i = 0; i < itemsToShow.Count; i++)
        {
            int networkIndex = itemsToShow[i].originalIndex;

            InventorySlotUIs slot = Instantiate(slotPrefab, transform);
            slot.inventoryIndex   = networkIndex;
            slot.slotIndex        = i;
            slot.inventoryID      = SlotDataID;   // ← ใช้ SlotDataID แทน inventoryID ตรงๆ
            slot.interactionMode  = interactionMode;
            slot.OnClickedCallback = OnSlotClicked;

            slotToInventoryIndex[i] = networkIndex;
            allSlots[i]             = slot;
        }

        Debug.Log($"[InventoryMainUIs] Built {itemsToShow.Count} slots (panelID={inventoryID}, slotDataID={SlotDataID})");
        OnSlotsPopulated();
    }

    protected virtual void OnSlotsPopulated() { }

    // ── Visual refresh ────────────────────────────────────────────────────────
    public virtual void RefreshAllSlots()
    {
        if (activeData == null || allSlots == null) return;

        for (int i = 0; i < allSlots.Length; i++)
        {
            int networkIndex = slotToInventoryIndex[i];
            if (networkIndex >= activeData.InventoryItems.Count) continue;

            NetworkItems itemData = activeData.InventoryItems[networkIndex];
            ItemSO itemSO = GameDataManager.Instance.itemDatabases.GetItemByID(itemData.ItemID);
            allSlots[i].RefreshSlot(itemSO, itemData.Amount);
        }
    }

    private void ClearAllSlots()
    {
        if (allSlots == null) return;
        foreach (var slot in allSlots)
            slot?.RefreshSlot(null, 0);
    }

    protected virtual void OnSlotClicked(InventorySlotUIs slot, PointerEventData eventData) { }
}
