using Unity.Netcode;
using UnityEngine;

public class DebugToHost : MonoBehaviour
{
    private void Start()
    {
        bool startAsClient = false;

        // 1) Standalone build: pass "-client" on the command line.
        //    e.g.  ShibaFarm.exe -client
        if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-client") >= 0)
        {
            startAsClient = true;
            Debug.Log("[DebugToHost] -client flag detected (command line) - starting as client.");
        }

#if UNITY_EDITOR
        // 2) Multiplayer Play Mode (MPPM) 2.x detection.
        //
        //    MPPM 2.x moved its API into Unity engine modules — the
        //    Unity.Multiplayer.Playmode namespace is no longer directly
        //    referenceable from Assembly-CSharp, and reflection is fragile
        //    across package versions.
        //
        //    Instead, we detect virtual players via Application.dataPath:
        //      Main Editor  →  .../ShibaFarm6/Assets
        //      Virtual Player → .../ShibaFarm6/Library/VP/<id>/Assets
        //
        //    Confirmed from project layout: Library/VP/mppmd7e024d6/Assets
        //    contains only MPPMVersion.json, while the real Assets folder
        //    holds the project.  Any instance running from Library/VP/ is
        //    a virtual player and should join as a client.
        if (!startAsClient)
        {
            bool isVirtualPlayer = Application.dataPath.Replace('\\', '/').Contains("/Library/VP/");
            if (isVirtualPlayer)
            {
                startAsClient = true;
                Debug.Log($"[DebugToHost] MPPM virtual player detected (dataPath contains Library/VP/) — starting as client.");
            }
            else
            {
                Debug.Log("[DebugToHost] Main editor instance — starting as host.");
            }
        }
#endif

        if (startAsClient)
        {
            NetworkManager.Singleton.StartClient();
            return;
        }

        NetworkManager.Singleton.ConnectionApprovalCallback += ApprovalCheck;
        NetworkManager.Singleton.StartHost();
    }

    // Counts how many clients have been approved this session.
    // Player 0 → SpawnPoint1, Player 1 → SpawnPoint2, etc.
    private int _approvalCount = 0;

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

        if (SpawnPointManager.Instance != null)
        {
            // GetMultiplayerSpawnTransform: sequential, no randomness,
            // does not interfere with single-player door/dungeon spawn logic.
            var spawnTransform = SpawnPointManager.Instance.GetMultiplayerSpawnTransform(_approvalCount);
            if (spawnTransform != null)
            {
                response.Position = spawnTransform.position;
                response.Rotation = spawnTransform.rotation;
            }
            else
            {
                response.Position = Vector3.zero;
                response.Rotation = Quaternion.identity;
            }
        }
        else
        {
            response.Position = Vector3.zero;
            response.Rotation = Quaternion.identity;
        }

        Debug.Log($"[Approval] Spawning client {request.ClientNetworkId} at SpawnPoint{_approvalCount + 1} → {response.Position}");
        _approvalCount++;
    }
}
