// PlayerDungeonState.cs
// Phase B — Personal dungeon instancing.
//
// Holds ALL per-player dungeon state that used to live globally on
// DungeonManager (via NetworkVariables + a single _saveData/_currentFloorData
// pair) or in the static DungeonReturnData class. One of these lives on every
// player's NetworkObject (FarmPlayer prefab).
//
// Replicated fields use NetworkVariableReadPermission.Owner — only the server
// and this specific player's own client ever see their values. This is the
// core mechanism that prevents one player's dungeon floor/seed/in-dungeon
// status from being broadcast to (or affecting) any other player.
//
// Teleportation (entering a floor, returning to the farm) is done with
// [Rpc(SendTo.Owner)], so only this player's client is moved — never the
// other connected players.

using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace MyGame.Dungeon
{
    public class PlayerDungeonState : NetworkSaveableBehaviour
    {
        // ── Lighting event (fires on ALL clients) ─────────────
        // DungeonLightingController subscribes here to switch URP Rendering
        // Layers on the player model when entering / leaving the dungeon.
        // Fired after netInDungeon changes so subscribers always see the
        // correct IsInDungeon value when they query it.
        public event Action<bool> OnDungeonStateChanged;

        // ── ISaveable ─────────────────────────────────────────
        public override bool IsPlayerSaveable => true;

        public override void CaptureState(GameSaveData save, ulong clientId = 0)
        {
            var playerData = save.GetOrCreatePlayer(clientId);

            playerData.dungeonInDungeon = IsInDungeon;
            playerData.dungeon = DungeonSave;

            playerData.hasDungeonReturnPosition = HasReturnPosition;
            if (HasReturnPosition)
            {
                playerData.dungeonReturnPosX = ReturnPosition.x;
                playerData.dungeonReturnPosY = ReturnPosition.y;
                playerData.dungeonReturnPosZ = ReturnPosition.z;
                playerData.dungeonReturnRotY = ReturnRotation.eulerAngles.y;
            }
        }

        public override void RestoreState(GameSaveData save, ulong clientId = 0)
        {
            if (!IsServer) return;

            var playerData = save.FindPlayer(clientId);
            if (playerData == null) return;

            if (playerData.hasDungeonReturnPosition)
            {
                SetReturnPosition(
                    new Vector3(playerData.dungeonReturnPosX, playerData.dungeonReturnPosY, playerData.dungeonReturnPosZ),
                    Quaternion.Euler(0f, playerData.dungeonReturnRotY, 0f));
            }

            if (playerData.dungeonInDungeon && playerData.dungeon != null)
            {
                Debug.Log($"[PlayerDungeonState] Restoring dungeon state for client {clientId}: floor={playerData.dungeon.currentFloor}, deepest={playerData.dungeon.deepestFloorReached}");
                DungeonManager.Instance?.EnterDungeon(this, playerData.dungeon);
            }
        }

        // ── Network Sync — Owner-only (Phase B) ────────────────
        // Only the server and this player's own client read these values. No
        // other connected player ever observes (or is affected by) this
        // player's dungeon state.
        private readonly NetworkVariable<int> netInstanceSlot = new NetworkVariable<int>(
            -1, NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Server);

        private readonly NetworkVariable<int> netMasterSeed = new NetworkVariable<int>(
            0, NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Server);

        private readonly NetworkVariable<int> netCurrentFloor = new NetworkVariable<int>(
            0, NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Server);

        private readonly NetworkVariable<bool> netInDungeon = new NetworkVariable<bool>(
            false, NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Server);

        public bool IsInDungeon => netInDungeon.Value;
        public int CurrentFloor => netCurrentFloor.Value;
        public int InstanceSlot => netInstanceSlot.Value;
        public int MasterSeed => netMasterSeed.Value;

        // ── Server-only runtime state ──────────────────────────
        // Not networked: lives only on the server, mirrors the old
        // DungeonManager._saveData / _currentFloorData but scoped per player.
        public DungeonSaveData DungeonSave { get; private set; }
        public DungeonFloorData CurrentFloorData { get; private set; }
        public readonly List<GameObject> SpawnedObjects = new();
        public GameObject WaypointParent { get; set; }

        public bool HasReturnPosition { get; private set; }
        public Vector3 ReturnPosition { get; private set; }
        public Quaternion ReturnRotation { get; private set; } = Quaternion.identity;

        // ── Lifecycle ───────────────────────────────────────────

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn(); // handles Register with SaveLoadManager

            netCurrentFloor.OnValueChanged += OnNetCurrentFloorChanged;
            netInDungeon.OnValueChanged += OnNetInDungeonChanged;

            // Late-join / reconnect: NetworkVariables already hold the server's
            // current values by the time OnNetworkSpawn runs on the owner's
            // client, but OnValueChanged does not fire retroactively — so build
            // the local view here if we're already inside the dungeon.
            if (!IsServer && IsOwner && netInDungeon.Value)
            {
                DungeonManager.Instance?.ClientEnterFloor(this, netCurrentFloor.Value, netMasterSeed.Value, netInstanceSlot.Value);
            }
        }

        public override void OnNetworkDespawn()
        {
            netCurrentFloor.OnValueChanged -= OnNetCurrentFloorChanged;
            netInDungeon.OnValueChanged -= OnNetInDungeonChanged;

            // Server-side cleanup: free this player's instance slot and remove
            // any objects spawned in their personal dungeon instance. This never
            // touches any other player's slot/objects.
            if (IsServer)
            {
                DungeonManager.Instance?.ClearFloor(this);
                DungeonManager.Instance?.ReleaseSlot(OwnerClientId);
            }

            base.OnNetworkDespawn(); // handles Unregister
        }

        // ── Server-side state mutators (called only by DungeonManager) ─────

        public void SetDungeonRuntimeState(DungeonSaveData save, int floor, bool inDungeon, int slot)
        {
            if (!IsServer) return;

            DungeonSave = save;
            netMasterSeed.Value = save.masterSeed;
            netInstanceSlot.Value = slot;
            netInDungeon.Value = inDungeon;
            // netCurrentFloor is set separately by SetCurrentFloorNumber once the
            // floor has actually been generated and visualized.
        }

        public void SetCurrentFloorData(DungeonFloorData data)
        {
            if (!IsServer) return;
            CurrentFloorData = data;
        }

        /// <summary>
        /// Updates the networked instance slot so the client knows which world-space
        /// origin to use when rebuilding cosmetic tiles via ClientEnterFloor.
        /// Must be called BEFORE SetCurrentFloorNumber so the slot value is already
        /// replicated when OnNetCurrentFloorChanged fires on the owner's client.
        /// </summary>
        public void SetInstanceSlot(int slot)
        {
            if (!IsServer) return;
            netInstanceSlot.Value = slot;
        }

        public void SetCurrentFloorNumber(int floor)
        {
            if (!IsServer) return;
            netCurrentFloor.Value = floor;
        }

        public void SetInDungeon(bool value)
        {
            if (!IsServer) return;
            netInDungeon.Value = value;
            if (!value) netCurrentFloor.Value = 0;
        }

        public void SetReturnPosition(Vector3 position, Quaternion rotation)
        {
            if (!IsServer) return;
            HasReturnPosition = true;
            ReturnPosition = position;
            ReturnRotation = rotation;
        }

        // ── Teleport — Owner only (Phase B) ────────────────────
        // Replaces the old global TeleportPlayerClientRpc/FreezeAndRelease pair.
        // SendTo.Owner guarantees this only ever runs on THIS player's client
        // (and on the host if this player IS the host) — entering/exiting a
        // dungeon (or transitioning floors) never moves any other player.

        [Rpc(SendTo.Owner)]
        public void TeleportOwnerRpc(Vector3 position, Quaternion rotation)
        {
            // CharacterController must be disabled before repositioning —
            // otherwise controller.Move() on the next Update() frame fights
            // the teleport and snaps the player back to the old position.
            var cc = GetComponentInChildren<CharacterController>();
            if (cc != null)
            {
                cc.enabled = false;
                transform.SetPositionAndRotation(position, rotation);
                cc.enabled = true;
                return;
            }

            var rb = GetComponent<Rigidbody>();
            if (rb != null)
                StartCoroutine(FreezeAndTeleport(rb, position, rotation));
            else
                transform.SetPositionAndRotation(position, rotation);
        }

        private IEnumerator FreezeAndTeleport(Rigidbody rb, Vector3 position, Quaternion rotation)
        {
            var original = rb.constraints;
            rb.constraints = RigidbodyConstraints.FreezeAll;
            rb.linearVelocity = Vector3.zero;
            transform.SetPositionAndRotation(position, rotation);
            yield return null;
            yield return null;
            rb.constraints = original;
        }

        // ── Floor Transition UI — Owner only ────────────────────
        // Mirrors the host's DungeonFloorTransition animation for non-host players.
        // DungeonManager.TransitionToNextFloor calls these when advancing a
        // non-host player so they see the same fade/floor-text overlay.

        [Rpc(SendTo.Owner)]
        public void BeginFloorFadeRpc()
        {
            var trans = DungeonFloorTransition.Instance;
            if (trans != null) StartCoroutine(trans.FadeIn());
        }

        [Rpc(SendTo.Owner)]
        public void CompleteFloorTransitionRpc(int floor)
        {
            var trans = DungeonFloorTransition.Instance;
            if (trans != null) StartCoroutine(ShowAndFadeOut(trans, floor));
        }

        private IEnumerator ShowAndFadeOut(DungeonFloorTransition trans, int floor)
        {
            trans.ShowFloorText(floor);
            yield return new WaitForSeconds(trans.holdDuration);
            yield return StartCoroutine(trans.FadeOut());
        }

        // ── Requests from the owning client ────────────────────
        // Default RPC invoke permission for SendTo.Server is the object's
        // owner, which is exactly what we want here: only this player's own
        // client may ask the server to remove THEM from their dungeon.
        [Rpc(SendTo.Server)]
        public void RequestExitDungeonServerRpc()
        {
            if (!IsServer) return;
            if (!IsInDungeon) return;
            DungeonManager.Instance?.ExitDungeon(this);
        }

        // ── Client-side sync ────────────────────────────────────
        // Mirrors the old OnNetCurrentFloorChanged / OnNetInDungeonChanged, but
        // guarded with IsOwner so a player's client only ever reacts to changes
        // on their OWN PlayerDungeonState — never another player's.

        private void OnNetCurrentFloorChanged(int previousFloor, int newFloor)
        {
            if (IsServer) return;       // server already visualized via EnterFloor
            if (!IsOwner) return;
            if (!netInDungeon.Value) return;
            if (newFloor <= 0) return;

            DungeonManager.Instance?.ClientEnterFloor(this, newFloor, netMasterSeed.Value, netInstanceSlot.Value);
        }

        private void OnNetInDungeonChanged(bool previousValue, bool newValue)
        {
            // Lighting event fires on ALL clients so every client can update
            // the rendering layer of this player's model (other players'
            // clients need to see the model lit correctly too).
            OnDungeonStateChanged?.Invoke(newValue);

            if (IsServer) return;
            if (!IsOwner) return;

            if (newValue)
                DungeonManager.Instance?.ClientEnterFloor(this, netCurrentFloor.Value, netMasterSeed.Value, netInstanceSlot.Value);
            else
                DungeonManager.Instance?.ClientExitDungeon(this);
        }
    }
}
