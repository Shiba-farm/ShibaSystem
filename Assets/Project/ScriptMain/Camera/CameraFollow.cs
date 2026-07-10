using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public static CameraFollow Instance { get; private set; }

    [Tooltip("Every VCam that should track the local player. " +
             "Each VCam controls its own offset and distance via its Body settings in the Inspector.")]
    [SerializeField] private CinemachineCamera[] virtualCameras;
    private Transform _followTarget;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnEnable()
    {
        if (NetworkManager.Singleton?.SceneManager != null)
            NetworkManager.Singleton.SceneManager.OnLoadComplete += OnSceneLoadComplete;
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton?.SceneManager != null)
            NetworkManager.Singleton.SceneManager.OnLoadComplete -= OnSceneLoadComplete;
    }

    // Called by PlayerController on the local owner only
    public void SetFollowTarget(Transform target)
    {
        _followTarget = target;
        ApplyFollowTarget(target);
        Debug.Log($"[CameraFollow] Now following {target.name}");
    }

    private void OnSceneLoadComplete(
        ulong clientId,
        string sceneName,
        UnityEngine.SceneManagement.LoadSceneMode loadSceneMode)
    {
        // Only care about local client's scene load
        if (clientId != NetworkManager.Singleton.LocalClientId) return;

        if (_followTarget != null)
        {
            ApplyFollowTarget(_followTarget);
            Debug.Log($"[CameraFollow] Reassigned follow after scene load: {sceneName}");
        }
        else
        {
            // Target lost — try to find local player again
            var localClient = NetworkManager.Singleton.LocalClient;
            if (localClient?.PlayerObject != null)
                SetFollowTarget(localClient.PlayerObject.transform);
        }
    }

    private void ApplyFollowTarget(Transform target)
    {
        if (virtualCameras == null) return;
        foreach (var vcam in virtualCameras)
            if (vcam != null) vcam.Follow = target;
    }
}
