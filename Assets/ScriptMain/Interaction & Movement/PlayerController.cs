using UnityEngine;
using Unity.Cinemachine;
using Unity.Netcode;

public class PlayerController : NetworkBehaviour, ISaveable
{
    public void SetBusy(bool busy) { isBusyAction = busy; }
    [Header("Movement")]
    public float walkSpeed = 2f;
    public float runSpeed = 4f;
    public float rotationSpeed = 10f;
    public float gravity = -9.81f;

    [Header("References")]
    public Transform cameraTransform;
    // [SerializeField] private CinemachineCamera playerVcam;

    [Header("")]
    private StatManager statManager;

    private CharacterController controller;
    private Animator animator;
    private Vector3 velocity;
    public bool _isRunning {get; private set;}
    public NetworkVariable<bool> IsRunning = new NetworkVariable<bool>(false,
    NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public bool isGrounded { get; private set; }
    public bool isBusyAction { get; private set; }
    public bool isSitting { get; private set; }
    private Transform currentSitPoint;

    public override void OnNetworkSpawn()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        statManager = GetComponentInChildren<StatManager>();

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

            InputHandler.Singleton.OnInteractTriggered -= OnPlayerInteract;
            InputHandler.Singleton.OnInteractTriggered += OnPlayerInteract;
        }
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
        float targetSpeed = _isRunning ? runSpeed : walkSpeed;
        Vector3 moveDir = Vector3.zero;

        if (_isRunning && targetSpeed == runSpeed)
        {
            IsRunning.Value = true;
        }
        else
        {
            IsRunning.Value = false;
        }
        
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

    private void OnPlayerInteract()
    {
        Debug.Log("Ineract!!");
    }

    private void StartActionTrigger(string triggerName) { isBusyAction = true; animator.ResetTrigger(triggerName); animator.SetTrigger(triggerName); }
    public void FaceTo(Vector3 worldPos) { Vector3 dir = worldPos - transform.position; dir.y = 0f; if (dir.sqrMagnitude < 0.001f) return; transform.rotation = Quaternion.LookRotation(dir.normalized); }

    public void Sit(Transform sitPoint) { if (isSitting) return; currentSitPoint = sitPoint; isSitting = true; isBusyAction = false; controller.enabled = false; transform.position = sitPoint.position; transform.rotation = sitPoint.rotation; animator.SetBool("Sit", true); }
    private void HandleSitInput() { if (Input.GetKeyDown(KeyCode.E)) StandUpFromSit(); }
    public void StandUpFromSit() { if (!isSitting) return; isSitting = false; animator.SetBool("Sit", false); controller.enabled = true; }
    public void StartFishing(Transform fishPoint) { if (isBusyAction) return; transform.position = fishPoint.position; transform.rotation = fishPoint.rotation; isBusyAction = true; animator.SetTrigger("Fish"); }
    public void OnFishingAnimationFinished() { isBusyAction = false; }

    public void CaptureState(GameSaveData save, ulong clientId = 0)
    {
        var playerData = save.GetOrCreatePlayer(clientId);
        var pos = transform.position;

        playerData.posX = pos.x;
        playerData.posY = pos.y;
        playerData.posZ = pos.z;
        playerData.rotY = transform.eulerAngles.y;
    }

    public void RestoreState(GameSaveData save, ulong clientId = 0)
    {
        if (!IsServer) return;
        var playerData = save.FindPlayer(clientId);
        if (playerData == null) return;

        // teleport on server — ClientNetworkTransform syncs it to client
        transform.position    = new Vector3(playerData.posX, playerData.posY, playerData.posZ);
        transform.eulerAngles = new Vector3(0, playerData.rotY, 0);
    }
}
