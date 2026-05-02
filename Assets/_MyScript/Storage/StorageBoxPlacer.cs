using UnityEngine;

/// <summary>
/// วาง StorageBox ลงในโลก
///
/// วิธีใช้:
///   1. สร้าง ItemSO ชื่อ "Storage Box" (sellable=false, category=CraftingMaterial)
///   2. ตั้งค่า storageBoxItem ใน Inspector ให้ชี้ที่ ItemSO นั้น
///   3. ตั้งค่า storageBoxPrefab ให้ชี้ที่ Prefab StorageBox
///   4. เพิ่ม Recipe ใน CraftingManager ให้คราฟ ItemSO นั้นได้
///   5. ติด StorageBoxPlacer script นี้ไว้กับ Player หรือ GameManager
///
/// การวาง:
///   • มี "Storage Box" ใน Inventory/Hotbar
///   • กด PlaceKey (default = B) → วางลงตรงหน้า player ทันที
///   • ลบ 1 ชิ้นออกจาก Inventory/Hotbar อัตโนมัติ
/// </summary>
public class StorageBoxPlacer : MonoBehaviour
{
    [Header("References")]
    [Tooltip("ItemSO ของ Storage Box ที่คราฟได้")]
    public ItemSO storageBoxItem;
    [Tooltip("Prefab กล่องที่จะ spawn ในโลก")]
    public GameObject storageBoxPrefab;

    [Header("Placement")]
    public KeyCode placeKey = KeyCode.B;
    [Tooltip("ระยะวางหน้า player")]
    public float placeDistance = 1.5f;
    [Tooltip("ความสูงที่วางบน ground (offset Y)")]
    public float placeHeightOffset = 0f;

    // ─── Runtime ──────────────────────────────────────────────────────
    Transform _playerTransform;

    // ──────────────────────────────────────────────────────────────────
    void Start()
    {
        // หา player
        var player = FindObjectOfType<PlayerController>();
        if (player) _playerTransform = player.transform;
    }

    void Update()
    {
        if (!Input.GetKeyDown(placeKey)) return;
        if (storageBoxItem == null || storageBoxPrefab == null) return;

        // ตรวจว่ามีของใน Inventory หรือ Hotbar
        if (!HasStorageBoxItem())
        {
            Debug.Log("[StorageBoxPlacer] ไม่มี Storage Box ใน Inventory");
            return;
        }

        PlaceBox();
    }

    // ─── Logic ────────────────────────────────────────────────────────

    bool HasStorageBoxItem()
    {
        if (InventoryMainUI.Instance != null)
        {
            foreach (var slot in InventoryMainUI.Instance.slots)
                if (slot != null && slot.item == storageBoxItem && slot.amount > 0)
                    return true;
        }

        if (HotbarUI.Instance != null)
        {
            foreach (var slot in HotbarUI.Instance.slots)
                if (slot != null && slot.item == storageBoxItem && slot.amount > 0)
                    return true;
        }

        return false;
    }

    void PlaceBox()
    {
        // หาตำแหน่งวาง — หน้า player
        Vector3 spawnPos = GetPlacePosition();

        // Spawn กล่อง
        var box = Instantiate(storageBoxPrefab, spawnPos, GetPlaceRotation());
        Debug.Log($"[StorageBoxPlacer] วาง StorageBox ที่ {spawnPos}");

        // ลบ item ออก 1 ชิ้น
        ConsumeOneStorageBox();
    }

    Vector3 GetPlacePosition()
    {
        if (_playerTransform == null)
            return Vector3.zero;

        // วางตรงหน้า player แบน Y
        Vector3 forward = _playerTransform.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 pos = _playerTransform.position + forward * placeDistance;
        pos.y = _playerTransform.position.y + placeHeightOffset;

        // Raycast ลง ground เพื่อหาความสูงพื้น
        if (Physics.Raycast(pos + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 10f))
            pos.y = hit.point.y + placeHeightOffset;

        return pos;
    }

    Quaternion GetPlaceRotation()
    {
        if (_playerTransform == null) return Quaternion.identity;

        // หันหน้าเข้าหา player
        Vector3 dir = _playerTransform.position - GetPlacePosition();
        dir.y = 0f;
        if (dir == Vector3.zero) return Quaternion.identity;
        return Quaternion.LookRotation(dir);
    }

    void ConsumeOneStorageBox()
    {
        // ลองลบจาก Inventory ก่อน
        if (InventoryMainUI.Instance != null)
        {
            foreach (var slot in InventoryMainUI.Instance.slots)
            {
                if (slot == null || slot.item != storageBoxItem || slot.amount <= 0) continue;
                slot.DecreaseAmount(1);
                return;
            }
        }

        // ถ้าไม่มีใน Inventory ลองลบจาก Hotbar
        if (HotbarUI.Instance != null)
        {
            foreach (var slot in HotbarUI.Instance.slots)
            {
                if (slot == null || slot.item != storageBoxItem || slot.amount <= 0) continue;
                int newAmt = slot.amount - 1;
                if (newAmt <= 0) slot.Clear();
                else slot.SetStack(slot.item, newAmt);
                return;
            }
        }
    }
}
