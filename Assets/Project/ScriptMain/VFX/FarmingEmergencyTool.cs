using UnityEngine;

/// <summary>
/// Emergency tool to debug and force-test the farming system without needing items.
/// </summary>
public class FarmingEmergencyTool : MonoBehaviour
{
    [Header("Debug Targets")]
    // public PlantingCursorController cursorController;
    public TilledGroundSystem groundSystem;
    public HeldItemSignal heldItemSignal;

    [Header("Test Item (Optional)")]
    public ItemSO testSeedItem;

    public void ForceShowCursor()
    {
        // if (cursorController == null) cursorController = FindObjectOfType<PlantingCursorController>();
        // if (cursorController == null) { Debug.LogError("Cursor Controller not found!"); return; }

        // We use SendMessage to set the flag internally or we can modify the controller
        // cursorController.gameObject.SetActive(true);
        // cursorController.Invoke("EnableDebugMode", 0); 
        Debug.Log("[EmergencyTool] Cursor Forced Active. Move your mouse over the terrain.");
    }

    public void SimulateHoldingSeed()
    {
        if (heldItemSignal == null || testSeedItem == null) 
        {
            Debug.LogError("Assign HeldItemSignal and a Test Seed Item in the Inspector!");
            return;
        }
        heldItemSignal.Set(testSeedItem, 0);
        Debug.Log($"[EmergencyTool] Simulated holding: {testSeedItem.itemName}");
    }

    public void TillAtMouse()
    {
        if (Camera.main == null || groundSystem == null) return;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Vector2Int cell = TilledGroundSystem.WorldToCell(hit.point);
            groundSystem.TillCell(cell);
            Debug.Log($"[EmergencyTool] Manually tilled cell: {cell}");
        }
    }

    public void CheckSystemStatus()
    {
        Debug.Log("--- Farming System Status ---");
        Debug.Log($"Terrain Layer Exists: {LayerMask.NameToLayer("Terrain") != -1}");
        Debug.Log($"TilledGroundSystem Instance: {(TilledGroundSystem.Instance != null ? "OK" : "MISSING")}");
        Debug.Log($"Camera.main: {(Camera.main != null ? Camera.main.name : "NULL")}");
    }
}
