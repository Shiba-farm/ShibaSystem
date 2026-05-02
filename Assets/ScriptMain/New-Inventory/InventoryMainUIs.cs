using Unity.Netcode;
using UnityEngine;

public class InventoryMainUIs : MonoBehaviour, IInitializableUI
{
    public static InventoryMainUIs Instance { get; private set; }
    [SerializeField] private InventoryDataSignal connectionSignal; // Assign in Inspector or find at runtime
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private InventorySlotUIs[] allSlots;
    public InventoryData activeData { get; private set; }
    private bool isHostProcessing = false;

    public void InitializeUI()
    {
        Instance = this;
        // Find all SlotUIs under this panel
        allSlots = GetComponentsInChildren<InventorySlotUIs>();

        for (int i = 0; i < allSlots.Length; i++)
        {
            // Automatically assign the index based on their order in the Grid Layout
            allSlots[i].slotIndex = i;

            // You can also assign the InventoryID here
            allSlots[i].inventoryID = 0; // 0 for Player Backpack
        }
    }
    void OnEnable()
    {
        connectionSignal.OnDataUpdate += HandleInventoryConnected;
        if (connectionSignal.CurrentData != null)
        {
            HandleInventoryConnected(connectionSignal.CurrentData);
        }
    }

    void OnDisable()
    {
        // Unsubscribe to prevent memory leaks or errors when UI is hidden
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
        if (activeData == data) return;
        activeData = data;

        // 2. Subscribe to real-time network changes
        // This ensures the UI refreshes whenever an item is added/removed on the server
        activeData.InventoryItems.OnListChanged += OnNetworkListChanged;

        // 3. Do the initial fill
        RefreshAllSlots();
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
            RefreshAllSlots();
            isHostProcessing = false;
        }
    }

    public void RefreshAllSlots()
    {
        if (activeData == null) return;

        // Debug.Log("Refreshing Inventory Visuals...");

        // Loop through our UI slots and match them to the NetworkList data
        for (int i = 0; i < allSlots.Length; i++)
        {
            // Safety check: Make sure our UI isn't bigger than our data list
            if (i < activeData.InventoryItems.Count)
            {
                NetworkItems itemData = activeData.InventoryItems[i];

                // Pass the data to the individual slot script to handle images/text
                ItemSO itemSO = GameDataManager.Instance.itemDatabases.GetItemByID(itemData.ItemID);
                if (itemSO != null)
                {
                    Debug.Log($"[InventoryMainUI] : Updating Slot {i}: ItemID={itemData.ItemID}, Amount={itemData.Amount}, ItemName={itemSO?.itemName}");
                }
                allSlots[i].RefreshSlot(itemSO, itemData.Amount);
            }
        }
    }
}
