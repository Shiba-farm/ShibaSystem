using Unity.Netcode;
using UnityEngine;

public class Door : NetworkBehaviour, IInteractable
{
    [Header("Door Settings")]
    [SerializeField] private string targetScene;
    [SerializeField] private string doorID;          // unique ID so destination knows where to spawn
    [SerializeField] private PromptType interactPromptType = PromptType.Door;

    public PromptType InteractPromptType => interactPromptType;

    public void Interact()
    {
        if (!IsOwner) return;

        // Client tells server to transition everyone
        RequestSceneTransitionServerRpc(targetScene, doorID);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestSceneTransitionServerRpc(string sceneName, string fromDoorID)
    {
        if (SceneTransitionManager.Instance == null)
        {
            Debug.LogWarning("[Door] SceneTransitionManager not found.");
            return;
        }

        SceneTransitionManager.Instance.LoadNetworkScene(sceneName, fromDoorID);
    }
}
