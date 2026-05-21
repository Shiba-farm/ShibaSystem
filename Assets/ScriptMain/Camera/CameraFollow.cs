using Unity.Cinemachine;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public static CameraFollow Instance { get; private set; }

    [SerializeField] private CinemachineCamera virtualCamera;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    // Called by PlayerController on the local owner only
    public void SetFollowTarget(Transform target)
    {
        virtualCamera.Follow = target;
        Debug.Log($"[CameraFollow] Now following {target.name}");
    }
}
