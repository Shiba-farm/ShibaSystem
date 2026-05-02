using UnityEngine;

/// <summary>
/// ระบบเรือ — ขึ้น/ลงเรือ, พายเรือด้วย WASD, ตกปลาจากเรือ
///
/// Setup:
/// 1. สร้าง GameObject "Boat" ใส่ Rigidbody + Collider (ตัวเรือ) + script นี้
/// 2. สร้าง child "SeatPoint"  — ตำแหน่ง+ทิศที่ player จะนั่ง
/// 3. สร้าง child "ExitPoint"  — ตำแหน่งที่ player จะลงเรือ
/// 4. สร้าง child "BoatFishingZone" ใส่ FishingZone script (useTrigger = false) + FishingZoneSO
/// 5. Assign ทุก field ใน Inspector
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class BoatController : MonoBehaviour
{
    // ─── Static ───────────────────────────────────────────────────────
    /// <summary>เรือที่ player กำลังอยู่บน (null = ไม่ได้อยู่บนเรือ)</summary>
    public static BoatController ActiveBoat { get; private set; }

    // ─── Inspector ────────────────────────────────────────────────────
    [Header("Movement")]
    [Tooltip("ความเร็วพาย (Acceleration)")]
    public float moveSpeed    = 5f;
    [Tooltip("ความเร็วหมุน")]
    public float turnSpeed    = 60f;
    [Tooltip("แรงต้าน (ยิ่งสูงหยุดเร็ว)")]
    public float waterDrag    = 3f;
    public float waterAngularDrag = 8f;

    [Header("Board / Exit")]
    public KeyCode boardKey   = KeyCode.E;
    [Tooltip("ระยะที่ player ต้องอยู่ใกล้ก่อนขึ้นเรือได้")]
    public float boardRadius  = 2.5f;
    [Tooltip("จุดที่ player นั่ง (child transform)")]
    public Transform seatPoint;
    [Tooltip("จุดที่ player จะ spawn ตอนลงเรือ (child transform)")]
    public Transform exitPoint;

    [Header("Fishing from Boat")]
    [Tooltip("FishingZone บนเรือ — useTrigger ต้องเป็น false")]
    public FishingZone boatFishingZone;

    [Header("Animations")]
    [Tooltip("Animator ของ model เรือ (ถ้ามี) — ใช้ bool 'Row'")]
    public Animator boatAnimator;

    [Header("Prompt UI")]
    public GameObject boardPromptUI;
    public TMPro.TextMeshProUGUI boardPromptText;
    public GameObject exitPromptUI;
    public TMPro.TextMeshProUGUI exitPromptText;

    // ─── Runtime ──────────────────────────────────────────────────────
    Rigidbody _rb;
    PlayerController _player;
    Animator _playerAnimator;
    CharacterController _playerCC;
    bool _hasPlayer;
    bool _playerNearby;

    // ──────────────────────────────────────────────────────────────────
    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.drag        = waterDrag;
        _rb.angularDrag = waterAngularDrag;
        // ล็อกการเอียงเพื่อไม่ให้เรือล้ม
        _rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        SetUI(boardPromptUI, false);
        SetUI(exitPromptUI,  false);
    }

    void Start()
    {
        // cache player ครั้งเดียวใน Start
        _player    = FindObjectOfType<PlayerController>();
        if (_player)
        {
            _playerAnimator = _player.GetComponent<Animator>();
            _playerCC       = _player.GetComponent<CharacterController>();
        }
    }

    void Update()
    {
        if (_hasPlayer)
        {
            // ───── บนเรือ ─────
            // Snap player ไปที่ seatPoint ทุก frame
            if (_player && seatPoint)
            {
                _player.transform.position = seatPoint.position;
                _player.transform.rotation = seatPoint.rotation;
            }

            // กด E เพื่อลง
            if (Input.GetKeyDown(boardKey)) ExitBoat();
        }
        else
        {
            // ───── บนบก ─────
            CheckNearby();
        }
    }

    void FixedUpdate()
    {
        if (!_hasPlayer) return;

        float v = Input.GetAxisRaw("Vertical");
        float h = Input.GetAxisRaw("Horizontal");
        bool moving = Mathf.Abs(v) > 0.05f || Mathf.Abs(h) > 0.05f;

        // เดินหน้า/ถอย
        if (Mathf.Abs(v) > 0.05f)
            _rb.AddForce(transform.forward * v * moveSpeed, ForceMode.Acceleration);

        // หมุน
        if (Mathf.Abs(h) > 0.05f)
        {
            float torque = h * turnSpeed * Time.fixedDeltaTime;
            _rb.AddTorque(Vector3.up * torque, ForceMode.VelocityChange);
        }

        // ─── Rowing Animation ────────────────────────────────────────
        if (_playerAnimator) _playerAnimator.SetBool("Row", moving);
        if (boatAnimator)    boatAnimator.SetBool("Row", moving);
    }

    // ─── Nearby Detection ─────────────────────────────────────────────

    void CheckNearby()
    {
        if (_player == null) return;

        float dist    = Vector3.Distance(transform.position, _player.transform.position);
        bool nearNow  = dist <= boardRadius;

        if (nearNow != _playerNearby)
        {
            _playerNearby = nearNow;
            SetUI(boardPromptUI, nearNow);
            if (nearNow && boardPromptText)
                boardPromptText.text = $"กด [{boardKey}] เพื่อขึ้นเรือ";
        }

        if (_playerNearby && Input.GetKeyDown(boardKey))
            EnterBoat();
    }

    // ─── Enter / Exit ─────────────────────────────────────────────────

    void EnterBoat()
    {
        if (_player == null) return;

        _hasPlayer  = true;
        ActiveBoat  = this;

        SetUI(boardPromptUI, false);
        _playerNearby = false;

        // ปิดการเคลื่อนที่ของ player
        if (_playerCC) _playerCC.enabled = false;
        _player.SetBusy(true);

        // Parent player เข้าเรือ
        _player.transform.SetParent(transform);
        if (seatPoint)
        {
            _player.transform.position = seatPoint.position;
            _player.transform.rotation = seatPoint.rotation;
        }

        // Animation Row = true, IsFishing = false (reset)
        if (_playerAnimator)
        {
            _playerAnimator.SetBool("Row",       true);
            _playerAnimator.SetBool("IsFishing", false);
        }

        // ลงทะเบียน fishing zone บนเรือ
        if (boatFishingZone)
            FishingSystem.Instance?.EnterZone(boatFishingZone);

        // แสดง prompt "กด E เพื่อลง / กด F เพื่อตกปลา"
        SetUI(exitPromptUI, true);
        if (exitPromptText)
            exitPromptText.text = $"[{boardKey}] ลงเรือ  |  [F] ตกปลา";

        Debug.Log("[Boat] ขึ้นเรือแล้ว — WASD พาย, F ตกปลา, E ลง");
    }

    void ExitBoat()
    {
        if (_player == null) return;

        // ยกเลิก fishing zone
        if (boatFishingZone)
            FishingSystem.Instance?.ExitZone(boatFishingZone);

        // หยุด animation
        if (_playerAnimator)
        {
            _playerAnimator.SetBool("Row",       false);
            _playerAnimator.SetBool("IsFishing", false);
        }

        // Unparent player
        _player.transform.SetParent(null);

        // ย้ายไป exitPoint
        if (exitPoint)
        {
            _player.transform.position = exitPoint.position;
            _player.transform.rotation = exitPoint.rotation;
        }

        // คืนการเคลื่อนที่
        if (_playerCC) _playerCC.enabled = true;
        _player.SetBusy(false);

        SetUI(exitPromptUI, false);

        _hasPlayer = false;
        ActiveBoat = null;

        Debug.Log("[Boat] ลงเรือแล้ว");
    }

    // ─── Helpers ──────────────────────────────────────────────────────

    static void SetUI(GameObject ui, bool active)
    {
        if (ui) ui.SetActive(active);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, boardRadius);

        if (seatPoint)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(seatPoint.position, 0.2f);
        }
        if (exitPoint)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(exitPoint.position, 0.2f);
        }
    }
}
