using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;

public class CameraManager : NetworkBehaviour
{
    [SerializeField] private CinemachineCamera[] playerCameras;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            InputHandler.Singleton.OnNumkeyTriggered += HandleCameraSwitch;
            HandleCameraSwitch(1);
        }
        else
        {
            foreach (var cam in playerCameras) cam.enabled = false;
        }
    }
    private void HandleCameraSwitch(int key)
    {
        // Convert key (1-based) to index (0-based)
        int index = key - 1;

        if (index < 0 || index >= playerCameras.Length) return;

        // Reset all to low priority, set target to high
        for (int i = 0; i < playerCameras.Length; i++)
        {
            playerCameras[i].Priority = (i == index) ? 20 : 10;
        }
    }
    public override void OnNetworkDespawn()
    {
        if (IsOwner && InputHandler.Singleton != null)
        {
            InputHandler.Singleton.OnNumkeyTriggered -= HandleCameraSwitch;
        }
    }
}
