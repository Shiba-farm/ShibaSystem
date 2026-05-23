using Unity.Netcode;
using UnityEngine;

public class DebugToHost : MonoBehaviour
{
    private void Start()
    {
        NetworkManager.Singleton.ConnectionApprovalCallback += ApprovalCheck;
        NetworkManager.Singleton.StartHost();
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.ConnectionApprovalCallback -= ApprovalCheck;
    }

    private void ApprovalCheck(
        NetworkManager.ConnectionApprovalRequest request,
        NetworkManager.ConnectionApprovalResponse response)
    {
        response.Approved           = true;
        response.CreatePlayerObject = true;

        // Read spawn point from current scene's SpawnPointManager
        if (SpawnPointManager.Instance != null)
        {
            response.Position = SpawnPointManager.Instance.GetNextPosition();
            response.Rotation = SpawnPointManager.Instance.GetNextRotation();
        }
        else
        {
            // No SpawnPointManager in scene — use origin
            response.Position = Vector3.zero;
            response.Rotation = Quaternion.identity;
        }

        Debug.Log($"[Approval] Spawning client {request.ClientNetworkId} " +
                  $"at {response.Position}");
    }
}
