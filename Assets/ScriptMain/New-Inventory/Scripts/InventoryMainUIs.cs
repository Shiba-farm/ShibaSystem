using Unity.Netcode;
using UnityEngine;
// using System.Linq;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class InventoryMainUIs : MonoBehaviour
{
    [SerializeField] protected InventoryDataSignal connectionSignal; // Assign in Inspector or find at runtime
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] protected int inventoryID;
    [SerializeField] protected InventorySlotUIs slotPrefab;
    [SerializeField] protected bool onlyShowUnEmptySlots = false;

    [Header("Interaction")]
    [SerializeField] protected SlotInteractionMode interactionMode = SlotInteractionMode.DragDrop;

    protected InventorySlotUIs[] allSlots;
    protected int[] slotToInventoryIndex;
    public InventoryData activeData { get; private set; }
    protected bool isHostProcessing = false;

    protected virtual void OnEnable()
    {
        InventoryUIRegistry.Register(inventoryID, this);
        connectionSignal.OnDataUpdate += HandleInventoryConnected;
        if (connectionSignal.CurrentData != null)
        {
            HandleInventoryConnected(connectionSignal.CurrentData);
        }
    }

    protected virtual void OnDisable()
    {
        InventoryUIRegistry.Unregister(inventoryID);
        connectionSignal.OnDataUpdate -= HandleInventoryConnected;

        if (activeData != null)
        {
            activeData.InventoryItems.OnListChanged -= OnNetworkListChanged;

            activeData = null;
        }
    }

    private void HandleInventoryConnected(InventoryData data)
    {
        // Debug.Log("Handle Inventory Connected...");
        // 1. Store the reference
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

        // 2. Subscribe to real-time network changes
        // This ensures the UI refreshes whenever an item is added/removed on the server
        activeData.InventoryItems.OnListChanged += OnNetworkListChanged;

        // 3. Do the initial fill
        PopulateSlots(activeData);
        RefreshAllSlots();
    }
    private void PopulateSlots(InventoryData data)
    {
        // Clear existing slots first
        if (allSlots != null)
        {
            foreach (var slot in allSlots)
                Destroy(slot.gameObject);
        }
        List<(NetworkItems item, int originalIndex)> itemsToShow = new();

        for (int i = 0; i < data.InventoryItems.Count; i++)
        {
            NetworkItems item = data.InventoryItems[i];
            if (onlyShowUnEmptySlots && item.ItemID == 0) continue;
            itemsToShow.Add((item, i));
        }
        allSlots = new InventorySlotUIs[itemsToShow.Count];
        slotToInventoryIndex = new int[itemsToShow.Count];
        Debug.Log($"Populating {itemsToShow.Count} slots for Inventory {data.InventoryID} (UI {inventoryID}) with Interaction mode : {interactionMode}");

        for (int i = 0; i < itemsToShow.Count; i++)
        {
            var (_, networkIndex) = itemsToShow[i];

            InventorySlotUIs slot = Instantiate(slotPrefab, transform);
            slot.inventoryIndex = networkIndex;
            slot.slotIndex = i;                // UI/visual index
            slot.inventoryID = inventoryID;
            slot.interactionMode = interactionMode;
            slotToInventoryIndex[i] = networkIndex; // Map UI index to NetworkList index

            slot.OnClickedCallback = OnSlotClicked;
            allSlots[i] = slot;
            Debug.Log($"Created slot {i} for Inventory Index {networkIndex} (ItemID: {data.InventoryItems[networkIndex].ItemID}, Amount: {data.InventoryItems[networkIndex].Amount}, InteractionMode : {slot.interactionMode})");
        }
    }
    private void OnNetworkListChanged(NetworkListEvent<NetworkItems> changeEvent)
    {
        Debug.Log("Network List Changed: " + changeEvent.Type);
        isHostProcessing = true; // Prevent feedback loops if we're the host
    }
    private void LateUpdate()
    {
        // LateUpdate happens after all ServerRpc logic is finished for the frame.
        if (isHostProcessing)
        {
            PopulateSlots(activeData); // Rebuild the UI to match the current data state
            RefreshAllSlots();
            isHostProcessing = false;
        }
    }

    public virtual void RefreshAllSlots()
    {
        if (activeData == null) return;

        // Debug.Log("Refreshing Inventory Visuals...");
        Debug.Log($"Inventory has {activeData.InventoryItems.Count} items. UI has {allSlots.Length} slots from {activeData.InventoryID}.");

        // Loop through our UI slots and match them to the NetworkList data
        for (int i = 0; i < allSlots.Length; i++)
        {
            int networkIndex = slotToInventoryIndex[i];
            if (networkIndex >= activeData.InventoryItems.Count) continue;
            // Safety check: Make sure our UI isn't bigger than our data list
            NetworkItems itemData = activeData.InventoryItems[networkIndex];

            // Pass the data to the individual slot script to handle images/text
            ItemSO itemSO = GameDataManager.Instance.itemDatabases.GetItemByID(itemData.ItemID);
            // Debug.Log($"Updating Slot {i} (Network Index {networkIndex}): ItemID={itemData.ItemID}, Amount={itemData.Amount}, ItemName={itemSO?.itemName}");
            if (itemSO != null)
            {
                Debug.Log($"[InventoryMainUI] : Updating Slot {i}: ItemID={itemData.ItemID}, Amount={itemData.Amount}, ItemName={itemSO?.itemName}, Interaction mode={allSlots[i].interactionMode}");
            }
            allSlots[i].RefreshSlot(itemSO, itemData.Amount);
        }
    }

    private void ClearAllSlots()
    {
        if (allSlots == null) return;

        for (int i = 0; i < allSlots.Length; i++)
        {
            allSlots[i].RefreshSlot(null, 0);
        }
    }

    protected virtual void OnSlotClicked(InventorySlotUIs slot, PointerEventData eventData) { }
}
