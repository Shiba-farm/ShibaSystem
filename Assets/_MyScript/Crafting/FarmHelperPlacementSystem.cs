using UnityEngine;

/// <summary>
/// ระบบวาง FarmHelper ลงในฟาร์ม
///
/// วิธีใช้งาน:
///   1. แนบ Script นี้ไว้บน GameObject เช่น "PlacementSystem" ใน Scene
///   2. ตั้งค่า groundLayer ให้ตรงกับ Layer ของพื้นดิน/ฟาร์ม
///   3. เรียก StartPlacement(helperSO, itemSO) จาก Hotbar / Inventory
///   4. ผู้เล่นคลิกซ้าย = วาง, คลิกขวา/Escape = ยกเลิก
/// </summary>
public class FarmHelperPlacementSystem : MonoBehaviour
{
    public static FarmHelperPlacementSystem Instance { get; private set; }

    // ================================================================
    // Inspector Fields
    // ================================================================

    [Header("Refs")]
    [Tooltip("InventoryUI — ถ้าปล่อยว่างจะหาเอง")]
    public InventoryMainUI inventoryUI;

    [Header("Placement Config")]
    [Tooltip("Layer ของพื้นดิน / Terrain ที่ Raycast จะโดน")]
    public LayerMask groundLayer;

    [Tooltip("ปุ่มวาง (default = Mouse Left)")]
    public KeyCode placeKey = KeyCode.Mouse0;

    [Tooltip("ปุ่มยกเลิก (default = Mouse Right)")]
    public KeyCode cancelKey = KeyCode.Mouse1;

    [Tooltip("snap ตำแหน่งให้ตรง Grid (แนะนำเปิด)")]
    public bool snapToGrid = true;

    [Tooltip("ขนาด 1 ช่อง grid (ปกติ = 1 unit)")]
    public float gridSize = 1f;

    [Header("Preview (Optional)")]
    [Tooltip("Object แสดง preview ก่อนวาง — ถ้าไม่มีก็ปล่อยว่าง")]
    public GameObject previewObject;

    [Tooltip("Material สำหรับ preview (semi-transparent)")]
    public Material previewMaterial;

    // ================================================================
    // Runtime
    // ================================================================

    bool isPlacing;
    FarmHelperSO pendingHelperSO;   // SO ของตัวช่วยที่กำลังจะวาง
    ItemSO pendingItemSO;           // Item ใน Inventory ที่จะถูกลบหลังวาง
    Camera mainCam;
    GameObject currentPreview;      // instance ของ preview

    // ================================================================
    // Lifecycle
    // ================================================================

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        mainCam = Camera.main;
        if (!inventoryUI) inventoryUI = InventoryMainUI.Instance;

        // ซ่อน preview ตั้งต้น
        if (previewObject) previewObject.SetActive(false);
    }

    void Update()
    {
        if (!isPlacing) return;

        Vector3 worldPos = GetPlacementPosition();

        // อัพเดท Preview
        UpdatePreview(worldPos);

        // คลิกซ้าย → วาง
        if (Input.GetKeyDown(placeKey))
        {
            TryPlace(worldPos);
        }

        // คลิกขวา หรือ Escape → ยกเลิก
        if (Input.GetKeyDown(cancelKey) || Input.GetKeyDown(KeyCode.Escape))
        {
            CancelPlacement();
        }
    }

    // ================================================================
    // Public API — เรียกจาก Hotbar / Inventory
    // ================================================================

    /// <summary>
    /// เริ่มโหมดวาง FarmHelper
    /// เรียกจาก Hotbar เมื่อผู้เล่น "ใช้" item ประเภท FarmHelper
    /// </summary>
    /// <param name="helperSO">ข้อมูลตัวช่วย</param>
    /// <param name="sourceItem">ItemSO ใน Inventory (จะถูกลบหลังวาง)</param>
    public void StartPlacement(FarmHelperSO helperSO, ItemSO sourceItem = null)
    {
        if (helperSO == null)
        {
            Debug.LogWarning("[PlacementSystem] helperSO เป็น null!");
            return;
        }

        pendingHelperSO = helperSO;
        pendingItemSO   = sourceItem;
        isPlacing       = true;

        // สร้าง Preview
        SpawnPreview(helperSO);

        Debug.Log($"[PlacementSystem] เริ่มวาง '{helperSO.helperName}' — คลิกซ้ายเพื่อวาง, คลิกขวายกเลิก");
    }

    /// <summary>ยกเลิกการวาง</summary>
    public void CancelPlacement()
    {
        isPlacing = false;
        pendingHelperSO = null;
        pendingItemSO   = null;
        DestroyPreview();
        Debug.Log("[PlacementSystem] ยกเลิกการวาง");
    }

    /// <summary>กำลังวางอยู่ไหม</summary>
    public bool IsPlacing => isPlacing;

    // ================================================================
    // Placement Logic
    // ================================================================

    void TryPlace(Vector3 worldPos)
    {
        if (pendingHelperSO == null) return;
        if (FarmHelperManager.Instance == null)
        {
            Debug.LogError("[PlacementSystem] ไม่พบ FarmHelperManager!");
            return;
        }

        // วาง
        FarmHelper placed = FarmHelperManager.Instance.PlaceHelper(pendingHelperSO, worldPos);

        if (placed != null)
        {
            Debug.Log($"[PlacementSystem] วาง {pendingHelperSO.helperName} ที่ {worldPos} สำเร็จ!");

            // ลบ item ออกจาก Inventory
            if (inventoryUI != null && pendingItemSO != null)
            {
                RemoveOneItemFromInventory(pendingItemSO);
            }

            // จบโหมดวาง
            isPlacing = false;
            pendingHelperSO = null;
            pendingItemSO   = null;
            DestroyPreview();
        }
        else
        {
            Debug.LogWarning("[PlacementSystem] วางไม่ได้ (เต็ม หรือไม่มี Prefab)");
        }
    }

    // ================================================================
    // Position Calculation
    // ================================================================

    Vector3 GetPlacementPosition()
    {
        Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);

        // Raycast โดน Ground Layer
        if (Physics.Raycast(ray, out RaycastHit hit, 200f, groundLayer))
        {
            Vector3 pos = hit.point;
            if (snapToGrid) pos = SnapToGrid(pos);
            return pos;
        }

        // Fallback: Raycast โดน Plane Y=0
        Plane ground = new Plane(Vector3.up, Vector3.zero);
        if (ground.Raycast(ray, out float dist))
        {
            Vector3 pos = ray.GetPoint(dist);
            if (snapToGrid) pos = SnapToGrid(pos);
            return pos;
        }

        return Vector3.zero;
    }

    Vector3 SnapToGrid(Vector3 pos)
    {
        return new Vector3(
            Mathf.Round(pos.x / gridSize) * gridSize,
            pos.y,
            Mathf.Round(pos.z / gridSize) * gridSize
        );
    }

    // ================================================================
    // Preview
    // ================================================================

    void SpawnPreview(FarmHelperSO helperSO)
    {
        DestroyPreview();

        // ถ้ามี Placement Prefab → clone มาเป็น preview
        if (helperSO.placementPrefab != null)
        {
            currentPreview = Instantiate(helperSO.placementPrefab);

            // ทำให้ดู semi-transparent
            if (previewMaterial != null)
            {
                foreach (var r in currentPreview.GetComponentsInChildren<Renderer>())
                    r.material = previewMaterial;
            }

            // ปิด Script ทั้งหมดบน preview (ไม่ให้มีผลกับเกม)
            foreach (var mb in currentPreview.GetComponentsInChildren<MonoBehaviour>())
                mb.enabled = false;

            // ปิด Collider บน preview
            foreach (var col in currentPreview.GetComponentsInChildren<Collider>())
                col.enabled = false;
        }
        else if (previewObject != null)
        {
            // ใช้ previewObject ที่กำหนดใน Inspector แทน
            previewObject.SetActive(true);
        }
    }

    void UpdatePreview(Vector3 pos)
    {
        if (currentPreview) currentPreview.transform.position = pos;
        if (previewObject && previewObject.activeSelf) previewObject.transform.position = pos;
    }

    void DestroyPreview()
    {
        if (currentPreview)
        {
            Destroy(currentPreview);
            currentPreview = null;
        }
        if (previewObject) previewObject.SetActive(false);
    }

    // ================================================================
    // Inventory Helper
    // ================================================================

    void RemoveOneItemFromInventory(ItemSO item)
    {
        if (inventoryUI == null) return;

        foreach (var slot in inventoryUI.slots)
        {
            if (slot.item != item) continue;

            slot.DecreaseAmount(1);
            if (slot.amount <= 0) slot.Clear();
            break;
        }
    }
}
