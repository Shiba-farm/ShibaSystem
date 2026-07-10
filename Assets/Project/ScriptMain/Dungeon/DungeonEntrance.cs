// DungeonEntrance.cs
// Phase B — Personal dungeon instancing.
//
// วางไว้ที่ปากถ้ำ/ทางเข้า Dungeon — กด E เพื่อเข้า
//
// Uses the same IInteractable + InteractController architecture as Door.cs: a
// local InteractController on the player detects this object's Collider via
// Physics.OverlapSphere and calls Interact() when the player presses the
// interact key.
//
// The dungeon is an always-loaded additive area split into per-player
// instance slots (see DungeonManager.instances). Entering no longer triggers
// any scene transition — the server simply records the requesting player's
// current position/rotation as their personal "return point", assigns them an
// instance slot, and teleports ONLY that player into their dungeon floor via
// PlayerDungeonState.TeleportOwnerRpc. No other connected player is moved,
// has their floor changed, or sees any transition UI as a result.

using Unity.Netcode;
using UnityEngine;

namespace MyGame.Dungeon
{
    [RequireComponent(typeof(Collider))]
    public class DungeonEntrance : NetworkBehaviour, IInteractable
    {
        // ──────────────────────────────────────────────────────────────────────
        // IInteractable
        // ──────────────────────────────────────────────────────────────────────

        public PromptType InteractPromptType => PromptType.Mine;

        public void Interact()
        {
            // Any client (owner or not) may request entry — the server resolves
            // which player asked and only ever affects that one player.
            RequestEnterDungeonServerRpc();
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void RequestEnterDungeonServerRpc(RpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;

            if (!NetworkManager.ConnectedClients.TryGetValue(clientId, out var client) || client.PlayerObject == null)
            {
                Debug.LogWarning($"[DungeonEntrance] No player object for client {clientId}.");
                return;
            }

            var player = client.PlayerObject.GetComponent<PlayerDungeonState>();
            if (player == null)
            {
                Debug.LogWarning($"[DungeonEntrance] Client {clientId}'s player has no PlayerDungeonState.");
                return;
            }

            if (player.IsInDungeon) return;

            player.SetReturnPosition(client.PlayerObject.transform.position, client.PlayerObject.transform.rotation);
            DungeonManager.Instance?.EnterDungeon(player);
        }

        // ──────────────────────────────────────────────────────────────────────

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, 1.5f);
        }
    }
}
