using UnityEngine;

public class CinematicFreeCam : MonoBehaviour
{
    [Header("Settings")]
    public float movementSpeed = 10f;   // ความเร็วปกติ
    public float fastSpeed = 30f;       // ความเร็วตอนกด Shift
    public float sensitivity = 2f;      // ความไวเมาส์
    public KeyCode toggleKey = KeyCode.G; // ปุ่มเปิด/ปิดโหมด

    [Header("References (ลากมาใส่)")]
    public GameObject uiCanvas;         // Canvas หลัก (เพื่อซ่อน UI)
    public MonoBehaviour playerScript;  // สคริปต์ PlayerController (เพื่อหยุดเดิน)
    public MonoBehaviour camFollowScript; // สคริปต์กล้องตามตัวละคร (ถ้ามี ให้ใส่เพื่อปิดมัน)

    private bool isEnable = false;
    private float rotX, rotY;

    // เก็บค่าเดิมไว้ เพื่อคืนค่าตอนปิดโหมด
    private Transform originalParent;
    private Vector3 originalPos;
    private Quaternion originalRot;
    private CursorLockMode originalCursorMode;
    private bool originalCursorVisible;

    void Start()
    {
        // เก็บค่าเริ่มต้นของมุมกล้อง
        rotX = transform.eulerAngles.y;
        rotY = transform.eulerAngles.x;
    }

    void Update()
    {
        // กด G เพื่อสลับโหมด
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleMode();
        }

        if (isEnable)
        {
            MoveCamera();
            RotateCamera();
        }
    }

    void ToggleMode()
    {
        isEnable = !isEnable;

        if (isEnable)
        {
            // --- เริ่มโหมดบิน ---

            // 1. จำค่าเดิม
            originalParent = transform.parent;
            originalPos = transform.localPosition;
            originalRot = transform.localRotation;
            originalCursorMode = Cursor.lockState;
            originalCursorVisible = Cursor.visible;

            // 2. ปลดกล้องออกจากตัวละคร (ถ้าเป็นลูก)
            transform.SetParent(null);

            // 3. ปิดการทำงานอื่นๆ
            if (uiCanvas) uiCanvas.SetActive(false);
            if (playerScript) playerScript.enabled = false;
            if (camFollowScript) camFollowScript.enabled = false;

            // 4. ล็อคเมาส์เพื่อหมุนกล้อง
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            // --- กลับสู่โหมดปกติ ---

            // 1. คืนค่าเดิม
            transform.SetParent(originalParent);
            transform.localPosition = originalPos;
            transform.localRotation = originalRot;
            Cursor.lockState = originalCursorMode;
            Cursor.visible = originalCursorVisible;

            // 2. เปิดการทำงานคืน
            if (uiCanvas) uiCanvas.SetActive(true);
            if (playerScript) playerScript.enabled = true;
            if (camFollowScript) camFollowScript.enabled = true;
        }
    }

    void MoveCamera()
    {
        float speed = Input.GetKey(KeyCode.LeftShift) ? fastSpeed : movementSpeed;

        // รับค่าปุ่ม WASD
        float h = Input.GetAxis("Horizontal"); // A, D
        float v = Input.GetAxis("Vertical");   // W, S

        // คำนวณทิศทาง (บินตามหน้ากล้อง)
        Vector3 moveDir = transform.forward * v + transform.right * h;

        // ปุ่ม Q (ลง) / E (ขึ้น)
        if (Input.GetKey(KeyCode.E)) moveDir += Vector3.up;
        if (Input.GetKey(KeyCode.Q)) moveDir -= Vector3.up;

        transform.position += moveDir * speed * Time.deltaTime;
    }

    void RotateCamera()
    {
        // หมุนกล้องด้วยเมาส์
        float mouseX = Input.GetAxis("Mouse X") * sensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivity;

        rotX += mouseX;
        rotY -= mouseY;
        rotY = Mathf.Clamp(rotY, -90f, 90f); // จำกัดไม่ให้เงยจนคอหัก

        transform.rotation = Quaternion.Euler(rotY, rotX, 0f);
    }
}