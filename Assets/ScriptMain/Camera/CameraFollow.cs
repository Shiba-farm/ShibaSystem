using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public static CameraFollow Instance { get; private set; }

    [SerializeField] private CinemachineCamera virtualCamera;
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
        virtualCamera.Follow = target;
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
            virtualCamera.Follow = _followTarget;
            Debug.Log($"[CameraFollow] Reassigned follow after scene load: {sceneName}");
        }
        else
        {
            // Target lost — try to find local player again
            var localClient = NetworkManager.Singleton.LocalClient;
            if (localClient?.PlayerObject != null)
            {
                SetFollowTarget(localClient.PlayerObject.transform);
            }
        }
    }
}
