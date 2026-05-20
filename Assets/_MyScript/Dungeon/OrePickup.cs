// OrePickup.cs
// แร่ที่ drop บนพื้น — หน่วงสั้นๆ แล้วดูดเข้าหา Player อัตโนมัติ

using UnityEngine;

public class OrePickup : MonoBehaviour
{
    [Header("Item")]
    public ItemSO item;
    public int    amount = 1;

    [Header("Magnet Settings")]
    [Tooltip("หน่วงกี่วิก่อนเริ่มบินเข้าหา Player")]
    public float magnetDelay    = 0.3f;
    [Tooltip("ความเร็วเริ่มต้นบินเข้าหา Player")]
    public float magnetSpeed    = 8f;
    [Tooltip("ระยะที่ถือว่าถึงแล้ว → เก็บทันที")]
    public float pickupDistance = 0.4f;

    [Header("Auto Destroy")]
    [Tooltip("หายไปถ้าไม่มีใครเก็บนานเกินกำหนด")]
    public float lifetime = 30f;

    // ──────────────────────────────────────────────────────────────────────
    // Runtime — ตั้งจาก DungeonOreNode.DropOre() หรือ Start() fallback
    // ──────────────────────────────────────────────────────────────────────
    [HideInInspector]
    public Transform targetTransform;   // Player transform (set by spawner)

    private float spawnTime;
    private bool  pickedUp = false;

    // ──────────────────────────────────────────────────────────────────────
    void Start()
    {
        spawnTime = Time.time;

        // fallback: หา Player เองถ้าไม่ได้รับมา
        if (targetTransform == null)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go) targetTransform = go.transform;
        }

        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        if (pickedUp) return;

        // รอ delay ก่อนเริ่มบิน
        if (Time.time < spawnTime + magnetDelay) return;

        if (targetTransform == null) return;

        // เคลื่อนที่เข้าหา Player (ใช้ตำแหน่ง XZ เพื่อไม่ให้ Y รบกวน)
        Vector3 target = new Vector3(targetTransform.position.x,
                                     transform.position.y,
                                     targetTransform.position.z);

        float elapsed = Time.time - spawnTime - magnetDelay;
        float speed   = magnetSpeed + elapsed * 6f;

        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);

        // เก็บเมื่อถึง Player (วัดระยะ XZ เท่านั้น)
        float dist = Vector2.Distance(
            new Vector2(transform.position.x, transform.position.z),
            new Vector2(targetTransform.position.x, targetTransform.position.z));

        if (dist <= pickupDistance)
            TryPickup();
    }

    // ──────────────────────────────────────────────────────────────────────
    void TryPickup()
    {
        if (pickedUp || item == null) return;

        pickedUp = true; // ตั้งก่อนเลย ป้องกัน call ซ้ำ

        bool added = false;

        if (HotbarUI.Instance != null)
            added = HotbarUI.Instance.AddItemToFirstEmptySlot(item, amount);

        if (!added)
            Debug.Log($"[OrePickup] Hotbar เต็ม — {item.itemName} x{amount} หายไป");

        Destroy(gameObject); // Destroy เสมอ ไม่ว่าจะเพิ่มสำเร็จหรือไม่
    }
}
