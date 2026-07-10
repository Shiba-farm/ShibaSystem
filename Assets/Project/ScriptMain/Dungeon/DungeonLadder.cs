// DungeonLadder.cs
// Phase B — Personal dungeon instancing.
//
// บันไดที่ player กด E เพื่อลงชั้นถัดไป
//
// Uses the same IInteractable + InteractController architecture as Door.cs /
// WorkbenchInteraction.cs: a local InteractController on the player detects
// this object's Collider via Physics.OverlapSphere and calls Interact() when
// the player presses the interact key. The floor change is requested from the
// server via an Rpc and validated/executed by
// DungeonManager.GoNextFloor(player) for the REQUESTING PLAYER ONLY — no other
// connected player's floor, position, or UI is affected (see DungeonManager.cs).

using Unity.Netcode;
using UnityEngine;

namespace MyGame.Dungeon
{
    [RequireComponent(typeof(Collider))]
    public class DungeonLadder : NetworkBehaviour, IInteractable
    {
        // ──────────────────────────────────────────────────────────────────────
        // IInteractable
        // ──────────────────────────────────────────────────────────────────────

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            // Ladder must be on the "Interact" layer so InteractController's
            // Physics.OverlapSphere can detect it.  Setting it here is safer
            // than relying on the prefab's layer being set correctly in the editor.
            int interactLayer = LayerMask.NameToLayer("Interact");
            if (interactLayer >= 0)
                gameObject.layer = interactLayer;
        }

        public PromptType InteractPromptType => PromptType.Door;

        public void Interact()
        {
            // Any client (owner or not) may request a floor transition — the
            // server resolves which player asked and advances only that
            // player's floor via DungeonManager.GoNextFloor().
            RequestNextFloorServerRpc();
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void RequestNextFloorServerRpc(RpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;

            if (!NetworkManager.ConnectedClients.TryGetValue(clientId, out var client) || client.PlayerObject == null)
            {
                Debug.LogWarning($"[DungeonLadder] No player object for client {clientId}.");
                return;
            }

            var player = client.PlayerObject.GetComponent<PlayerDungeonState>();
            if (player == null) return;

            DungeonManager.Instance?.GoNextFloor(player);
        }

        // ──────────────────────────────────────────────────────────────────────

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 1.5f);
        }
    }
}
