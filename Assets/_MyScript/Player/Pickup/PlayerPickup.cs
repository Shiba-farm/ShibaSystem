using UnityEngine;
using TMPro;

/// <summary>
/// ติดไว้บน Player — จัดการเก็บไอเทมจากพื้น
/// - กด E เพื่อเก็บทุก Pickupable ในรัศมี
/// - แสดง prompt "กด E เพื่อเก็บ" ใกล้ item ที่อยู่ใกล้ที่สุด
/// - เรียก PlayPickupEffects() ก่อน Destroy
/// - ใส่ Hotbar ก่อน ถ้าเต็มค่อยใส่ Inventory
/// - (Optional) Auto-pickup
/// </summary>
public class PlayerPickup : MonoBehaviour
{
    [Header("Pickup Settings")]
    public float pickupRadius = 2f;
    public LayerMask pickupLayer;
    public KeyCode pickupKey = KeyCode.E;

    [Header("Auto Pickup")]
    [Tooltip("เก็บอัตโนมัติเมื่อเดินเข้าไปใกล้ (ไม่ต้องกด E)")]
    public bool autoPickup = false;
    [Tooltip("รัศมีสำหรับ Auto-pickup — ควรน้อยกว่า pickupRadius")]
    public float autoPickupRadius = 0.8f;

    [Header("Prompt UI")]
    [Tooltip("TextMeshProUGUI สำหรับแสดง 'กด E เพื่อเก็บ'\n(ใส่ใน ScreenSpace-Overlay Canvas)")]
    public TextMeshProUGUI promptText;
    [Tooltip("RectTransform ของ panel/root ที่ครอบ promptText\n(ใช้เลื่อนตำแหน่งให้ลอยเหนือ item)")]
    public RectTransform promptRoot;

    // Runtime
    Camera _cam;
    Pickupable _nearestPickupable;

    void Awake()
    {
        _cam = Camera.main;
    }

    void Update()
    {
        if (InventoryMainUI.IsOpen) return;

        FindNearest();
        UpdatePrompt();

        // Auto-pickup
        if (autoPickup)
            AutoPickupInRadius();

        // Manual pickup — กด E
        if (Input.GetKeyDown(pickupKey))
            PickupAllInRadius();
    }

    // ──────────────────────────────────────────
    // หา Pickupable ที่อยู่ใกล้ที่สุดในรัศมี
    // ──────────────────────────────────────────
    void FindNearest()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, pickupRadius, pickupLayer);
        float minDist = float.MaxValue;
        _nearestPickupable = null;

        foreach (var hit in hits)
        {
            var p = hit.GetComponent<Pickupable>();
            if (p == null) continue;

            float d = Vector3.Distance(transform.position, hit.transform.position);
            if (d < minDist)
            {
                minDist = d;
                _nearestPickupable = p;
            }
        }
    }

    // ──────────────────────────────────────────
    // แสดง / ซ่อน Prompt เหนือ item
    // ──────────────────────────────────────────
    void UpdatePrompt()
    {
        if (promptText == null) return;

        if (_nearestPickupable != null)
        {
            promptText.gameObject.SetActive(true);

            // เลื่อน promptRoot ให้ลอยเหนือ item (ถ้าใช้ Screen-Space Canvas)
            if (promptRoot != null && _cam != null)
            {
                Vector3 worldPos = _nearestPickupable.transform.position + Vector3.up * 1.2f;
                Vector3 screenPos = _cam.WorldToScreenPoint(worldPos);

                // ถ้า item อยู่หลัง Camera ให้ซ่อน prompt
                if (screenPos.z < 0f)
                {
                    promptText.gameObject.SetActive(false);
                    return;
                }

                promptRoot.position = new Vector2(screenPos.x, screenPos.y);
            }

            string itemName = _nearestPickupable.itemData != null
                ? _nearestPickupable.itemData.itemName
                : "Item";
            promptText.text = $"[{pickupKey}] เก็บ {itemName}";
        }
        else
        {
            promptText.gameObject.SetActive(false);
        }
    }

    // ──────────────────────────────────────────
    // Auto-pickup (รัศมีเล็ก)
    // ──────────────────────────────────────────
    void AutoPickupInRadius()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, autoPickupRadius, pickupLayer);
        foreach (var hit in hits)
        {
            var p = hit.GetComponent<Pickupable>();
            if (p != null) DoPickup(p);
        }
    }

    // ──────────────────────────────────────────
    // เก็บทุก item ในรัศมีใหญ่ (กด E)
    // ──────────────────────────────────────────
    void PickupAllInRadius()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, pickupRadius, pickupLayer);
        foreach (var hit in hits)
        {
            var p = hit.GetComponent<Pickupable>();
            if (p != null) DoPickup(p);
        }
    }

    // ──────────────────────────────────────────
    // เก็บ Pickupable ชิ้นเดียว
    // ──────────────────────────────────────────
    void DoPickup(Pickupable pickupable)
    {
        if (pickupable == null || pickupable.itemData == null)
        {
            Debug.LogWarning("[PlayerPickup] พบ Pickupable ที่ไม่มี itemData — ข้ามไป");
            return;
        }

        int remaining = pickupable.amount;

        // 1) ลอง Hotbar ก่อน
        if (HotbarUI.Instance != null)
        {
            bool added = HotbarUI.Instance.AddItemToFirstEmptySlot(pickupable.itemData, remaining);
            if (added) remaining = 0;
        }

        // 2) ที่เหลือใส่ Inventory
        if (remaining > 0)
        {
            if (InventoryMainUI.Instance == null)
            {
                Debug.Log("[PlayerPickup] ไม่พบ InventoryUI — เก็บไม่ได้");
                return;
            }

            bool added = InventoryMainUI.Instance.AddItemToInventory(pickupable.itemData, remaining);
            if (!added)
            {
                Debug.Log("[PlayerPickup] Inventory & Hotbar เต็ม — เก็บไม่ได้");
                return; // ไม่ทำลาย item ถ้าเก็บไม่ได้
            }
        }

        // 3) เล่น effect แล้ว Destroy
        pickupable.PlayPickupEffects();
        Destroy(pickupable.gameObject);
    }

    // ──────────────────────────────────────────
    // Debug Gizmo
    // ──────────────────────────────────────────
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.9f, 0f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, pickupRadius);

        if (autoPickup)
        {
            Gizmos.color = new Color(0f, 1f, 0.2f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, autoPickupRadius);
        }
    }
}
