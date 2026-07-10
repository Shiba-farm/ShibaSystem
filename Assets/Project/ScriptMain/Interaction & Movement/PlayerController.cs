using UnityEngine;
using Unity.Cinemachine;
using Unity.Netcode;

public class PlayerController : NetworkSaveableBehaviour
{
    public void SetBusy(bool busy) { isBusyAction = busy; }
    [Header("Movement")]
    public float walkSpeed = 2f;
    public float runSpeed = 4f;
    public float rotationSpeed = 10f;
    public float gravity = -9.81f;

    [Header("Movement Skill Bonus")]
    [Tooltip("skillId ของสกิล Movement ที่ใช้เพิ่มความเร็ว (ดู Skill_SwiftPaws.asset)")]
    [SerializeField] private int moveSpeedSkillId = 501;
    [Tooltip("ความเร็วที่เพิ่มต่อ 1 เลเวล เช่น 0.1 = +10% ต่อเลเวล")]
    [SerializeField] private float speedBonusPerLevel = 0.1f;
    private SkillManager skillManager;
    private EquipmentData equipmentData;

    [Header("References")]
    public Transform cameraTransform;
    // [SerializeField] private CinemachineCamera playerVcam;

    [Header("")]
    private StatManager statManager;

    private CharacterController controller;
    private Animator animator;
    private Vector3 velocity;
    public bool _isRunning { get; private set; }
    public NetworkVariable<bool> IsRunning = new NetworkVariable<bool>(false,
    NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    /// <summary>Server-authoritative fishing phase — drives animations and camera on all clients.</summary>
    public NetworkVariable<FishingPhase> CurrentFishingPhase = new NetworkVariable<FishingPhase>(
        FishingPhase.None,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);
    public bool isGrounded { get; private set; }
    public bool isBusyAction { get; private set; }
    public bool isSitting { get; private set; }
    private Transform currentSitPoint;
    public override bool IsPlayerSaveable => true;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        statManager = GetComponentInChildren<StatManager>();
        skillManager = GetComponent<SkillManager>();
        equipmentData = GetComponent<EquipmentData>();

        if (cameraTransform == null) cameraTransform = Camera.main.transform;

        // Cursor management: Make cursor visible for 3D grid interaction
        Cursor.lockState = CursorLockMode.None; // Changed from Locked to None
        Cursor.visible = true; // Changed from false to true

        if (IsOwner)
        {
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += HandleSceneLoaded;
            Cursor.lockState = CursorLockMode.None; // Changed from Locked to None
            Cursor.visible   = true; // Changed from false to true
            AssignCamera();

            // Tell scene VCam to follow us
            if (CameraFollow.Instance != null)
                CameraFollow.Instance.SetFollowTarget(transform);
            else
                Debug.Log($"Camera follow is null : {CameraFollow.Instance == null}");

            // Register with CameraManager so it can subscribe to our state changes (fishing, etc.)
            CameraManager.Instance?.RegisterLocalPlayer(this);

            InputHandler.Singleton.OnInteractTriggered -= OnPlayerInteract;
            InputHandler.Singleton.OnInteractTriggered += OnPlayerInteract;

            var hotbar = FindFirstObjectByType<HotbarUIController>();
            hotbar?.SetOwnerClientId(OwnerClientId);
            Debug.Log("Set hotbar owner client ID");
        }
        if (IsServer)
        {
            SaveLoadManager.Instance?.Register(this);
        }

    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        if (IsOwner)
        {
            CameraManager.Instance?.UnregisterLocalPlayer(this);
            if (InputHandler.Singleton != null)
                InputHandler.Singleton.OnInteractTriggered -= OnPlayerInteract;
        }
        if (IsServer)
            SaveLoadManager.Instance?.Unregister(this);
    }
    private void HandleSceneLoaded(string sceneName,
                                UnityEngine.SceneManagement.LoadSceneMode loadSceneMode,
                                System.Collections.Generic.List<ulong> clientsCompleted,
                                System.Collections.Generic.List<ulong> clientsTimedOut)
    {
        AssignCamera();
    }

    private void AssignCamera()
    {
        if (Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
            Debug.Log("[Player controller] : Assign camera");
        }
        else
        {
            Debug.Log("[Player controller] : Can't find camera");
        }
    }

    private void Update()
    {
        if (!IsOwner)
        {
            return;
        }
        if (cameraTransform == null)
        {
            Debug.Log("[Player controller] : Camera not found");
            return;
        }
        if (isSitting) { HandleSitInput(); return; }

        if (isBusyAction) return;

        HandleMovement();
    }

    private void HandleMovement()
    {
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0f) velocity.y = -2f;

        Vector2 input = InputHandler.Singleton.MoveInput;

        _isRunning = InputHandler.Singleton.IsSprinting;
        int swiftPawsLevel = skillManager != null ? skillManager.GetLevel(moveSpeedSkillId) : 0;
        float speedMultiplier = 1f + swiftPawsLevel * speedBonusPerLevel;
        // บวก speedBonus ของ Wearable ที่สวมอยู่ (เช่น Boots) เข้าไปตรง ๆ แบบ flat — คำนวณสดทุกเฟรม
        // จาก EquipmentData เลย ไม่ต้อง apply ผ่าน StatManager (ดูเหตุผลใน WearableItemSO.OnEquip)
        float equipSpeedBonus = equipmentData != null ? equipmentData.GetTotalSpeedBonus() : 0f;
        float targetSpeed = (_isRunning ? runSpeed : walkSpeed) * speedMultiplier + equipSpeedBonus;
        Vector3 moveDir = Vector3.zero;

        // เดิมเช็ค targetSpeed == runSpeed เพื่อสรุปว่ากำลังวิ่งอยู่ไหม แต่พอ targetSpeed
        // ถูกคูณ speedMultiplier (จากสกิล Swift Paws) แล้ว มันจะไม่เท่ากับ runSpeed พอดีอีกต่อไป
        // (ถึงจะกำลังวิ่งจริงก็ตาม) เลยเปลี่ยนไปอ้างอิง _isRunning ตรง ๆ แทน — กัน StatManager
        // (ที่เช็ค IsRunning.Value เพื่อคำนวณการใช้ Energy/Stamina) อ่านค่าผิดไปด้วย
        IsRunning.Value = _isRunning;

        if (input.sqrMagnitude >= 0.01f)
        {
            Vector3 camForward = cameraTransform.forward;
            Vector3 camRight = cameraTransform.right;

            camForward.y = 0f;
            camRight.y = 0f;
            camForward.Normalize();
            camRight.Normalize();

            moveDir = (camForward * input.y + camRight * input.x).normalized;

            Quaternion targetRotation = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
        velocity.y += gravity * Time.deltaTime;
        Vector3 finalVelocity = (moveDir * targetSpeed) + (Vector3.up * velocity.y);
        controller.Move(finalVelocity * Time.deltaTime);

        float animSpeed = input.magnitude * (_isRunning ? 1f : 0.5f);

        animator.SetFloat("Speed", animSpeed);
        animator.SetBool("IsRunning", _isRunning);
    }

    /// <summary>New Input System fires this → NPCInteractable subscribes ด้วย event นี้</summary>
    public static event System.Action OnInteractPressed;

    private void OnPlayerInteract()
    {
        Debug.Log("Interact!!");
        OnInteractPressed?.Invoke();
    }

    private void StartActionTrigger(string triggerName) { isBusyAction = true; animator.ResetTrigger(triggerName); animator.SetTrigger(triggerName); }
    public void FaceTo(Vector3 worldPos) { Vector3 dir = worldPos - transform.position; dir.y = 0f; if (dir.sqrMagnitude < 0.001f) return; transform.rotation = Quaternion.LookRotation(dir.normalized); }

    public void Sit(Transform sitPoint) { if (isSitting) return; currentSitPoint = sitPoint; isSitting = true; isBusyAction = false; controller.enabled = false; transform.position = sitPoint.position; transform.rotation = sitPoint.rotation; animator.SetBool("Sit", true); }
    private void HandleSitInput() { if (Input.GetKeyDown(KeyCode.E)) StandUpFromSit(); }
    public void StandUpFromSit() { if (!isSitting) return; isSitting = false; animator.SetBool("Sit", false); controller.enabled = true; }
    public void StartFishing(Transform fishPoint) { if (isBusyAction) return; transform.position = fishPoint.position; transform.rotation = fishPoint.rotation; isBusyAction = true; animator.SetTrigger("Fish"); }
    public void OnFishingAnimationFinished() { isBusyAction = false; }

    // ── Teleport (owner RPC) ─────────────────────────────────
    // Always sends to the owning client so it works even when
    // ClientNetworkTransform is client-authoritative.  The server
    // is also the owner for the host player, so it runs locally there.
    [Rpc(SendTo.Owner)]
    public void TeleportOwnerRpc(Vector3 position, Quaternion rotation)
    {
        // CharacterController must be disabled before repositioning —
        // otherwise its internal grounding logic fights the teleport.
        if (controller != null)
        {
            controller.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            controller.enabled = true;
        }
        else
        {
            transform.SetPositionAndRotation(position, rotation);
        }
    }

    public override void CaptureState(GameSaveData save, ulong clientId = 0)
    {
        var playerData = save.GetOrCreatePlayer(clientId);
        var pos = transform.position;

        playerData.currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        playerData.posX = pos.x;
        playerData.posY = pos.y;
        playerData.posZ = pos.z;
        playerData.rotY = transform.eulerAngles.y;
    }

    public override void RestoreState(GameSaveData save, ulong clientId = 0)
    {
        if (!IsServer) return;
        var playerData = save.FindPlayer(clientId);
        if (playerData == null) return;

        // Send to owner client — works with ClientNetworkTransform
        TeleportOwnerRpc(
            new Vector3(playerData.posX, playerData.posY, playerData.posZ),
            Quaternion.Euler(0, playerData.rotY, 0));
    }
}
