// DungeonLightingController.cs
// Switches URP Rendering Layers on the Player model when entering / leaving
// the dungeon, so the player is lit by dungeon torches inside and by the
// farm sun outside — without touching any physics layers.
//
// Why URP Rendering Layers (not GameObject layers)?
// ─────────────────────────────────────────────────
// GameObject layers (Physics) are shared with Colliders, Triggers, and
// Raycasts. Changing a player's GameObject layer breaks IInteractable
// detection (OverlapSphere looks for the "Interact" layer) and footstep
// physics queries. URP Rendering Layers are completely separate — they only
// affect which lights illuminate which meshes. Switching them is safe and
// has no gameplay side effects.
//
// One-time Editor Setup (do this BEFORE using this component)
// ────────────────────────────────────────────────────────────
// 1. Edit → Project Settings → Graphics → URP Global Settings
//    → Rendering Layers → Add:
//        Index 0 = "WorldLayer"   (default, farm objects)
//        Index 1 = "DungeonLayer" (dungeon tiles, objects, player-in-dungeon)
//
// 2. Main Directional Light (Sun):
//    Inspector → Light → Rendering Layers → check WorldLayer, UNCHECK DungeonLayer
//
// 3. Create a Dungeon Ambient Light (very dim directional, Intensity ~0.05):
//    Rendering Layers → UNCHECK WorldLayer, check DungeonLayer
//
// 4. Torch / Point Light prefabs:
//    Rendering Layers → UNCHECK WorldLayer, check DungeonLayer
//
// 5. All farm meshes: leave Rendering Layer at default (WorldLayer bit = 1).
//    Dungeon tiles and objects: DungeonManager will set them automatically
//    via SetDungeonRenderingLayer() in SpawnTile / SpawnNetworkObject.
//
// 6. Add THIS component to the Player prefab root (same object as
//    PlayerDungeonState).  It subscribes to the dungeon state and switches
//    the player model's rendering layer automatically.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Netcode;

namespace MyGame.Dungeon
{
    [RequireComponent(typeof(PlayerDungeonState))]
    public class DungeonLightingController : NetworkBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────

        [Header("Rendering Layer Names (must match URP Global Settings)")]
        [Tooltip("Name of the URP Rendering Layer used for the farm / overworld.")]
        public string worldLayerName   = "WorldLayer";

        [Tooltip("Name of the URP Rendering Layer used inside the dungeon.")]
        public string dungeonLayerName = "DungeonLayer";

        [Header("Dungeon Ambient Override")]
        [Tooltip("Ambient colour forced on the local client while inside the dungeon. " +
                 "Pure black = no ambient — only torch/point lights illuminate the dungeon.")]
        public Color dungeonAmbientColor = Color.black;

        [Header("Target")]
        [Tooltip("Root of the player visual model. If null, searches children of this GameObject.")]
        public Transform modelRoot;

        // ── Private ───────────────────────────────────────────────────

        private PlayerDungeonState _dungeonState;
        private uint _worldMask;
        private uint _dungeonMask;

        // Cached scene ambient so we can restore it cleanly on dungeon exit.
        private AmbientMode _cachedAmbientMode;
        private Color       _cachedAmbientLight;
        private bool        _isInDungeon;

        private readonly List<Renderer> _renderers = new();

        // ── Lifecycle ─────────────────────────────────────────────────

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            _dungeonState = GetComponent<PlayerDungeonState>();

            // Resolve layer masks once.
            _worldMask   = GetRenderingLayerMask(worldLayerName);
            _dungeonMask = GetRenderingLayerMask(dungeonLayerName);

            // Collect all renderers under the model root (or this transform).
            var root = modelRoot != null ? modelRoot : transform;
            root.GetComponentsInChildren(true, _renderers);

            // Cache the scene's current ambient settings so we can restore
            // them precisely when the local player leaves the dungeon.
            _cachedAmbientMode  = RenderSettings.ambientMode;
            _cachedAmbientLight = RenderSettings.ambientLight;
            _isInDungeon        = _dungeonState.IsInDungeon;

            // Subscribe to dungeon state changes on ALL clients so every
            // client sees the correct lighting on every player model.
            _dungeonState.OnDungeonStateChanged += HandleDungeonStateChanged;

            // Apply current state immediately (handles late-join / reconnect).
            ApplyLayer(_dungeonState.IsInDungeon);
        }

        public override void OnNetworkDespawn()
        {
            if (_dungeonState != null)
                _dungeonState.OnDungeonStateChanged -= HandleDungeonStateChanged;

            base.OnNetworkDespawn();
        }

        // ── Handlers ──────────────────────────────────────────────────

        private void HandleDungeonStateChanged(bool isInDungeon)
        {
            _isInDungeon = isInDungeon;
            ApplyLayer(isInDungeon);

            // Ambient override is local-player only — other clients manage
            // their own RenderSettings independently.
            if (!IsOwner) return;

            if (!isInDungeon)
            {
                // Restore the farm's ambient so TimeOfDaySystem can take over again.
                RenderSettings.ambientMode  = _cachedAmbientMode;
                RenderSettings.ambientLight = _cachedAmbientLight;
                // Force an immediate refresh so the transition is seamless.
                TimeOfDaySystem.Instance?.ForceUpdateLighting();
            }
        }

        // ── Ambient Override ──────────────────────────────────────────
        // TimeOfDaySystem sets RenderSettings.ambientLight globally on every
        // time-tick. URP Rendering Layers don't block global ambient — it
        // reaches dungeon geometry regardless.
        //
        // Solution: the local player's LateUpdate writes the dungeon ambient
        // AFTER any time-tick that ran this frame, winning the last-write-wins
        // race without requiring changes to TimeOfDaySystem's core logic.
        void LateUpdate()
        {
            if (!IsOwner || !_isInDungeon) return;

            // Flat mode ensures RenderSettings.ambientLight is used directly
            // (Skybox mode would re-derive ambient from the sky and ignore this).
            RenderSettings.ambientMode  = AmbientMode.Flat;
            RenderSettings.ambientLight = dungeonAmbientColor;
        }

        private void ApplyLayer(bool inDungeon)
        {
            uint mask = inDungeon ? _dungeonMask : _worldMask;
            foreach (var r in _renderers)
                if (r != null) r.renderingLayerMask = mask;
        }

        // ── Utility ───────────────────────────────────────────────────

        /// <summary>
        /// Returns the URP rendering layer mask for the named layer.
        /// Logs a warning if the layer name is not found in the URP Global Settings.
        /// </summary>
        private static uint GetRenderingLayerMask(string layerName)
        {
            uint mask = RenderingLayerMask.GetMask(layerName);
            if (mask == 0)
                Debug.LogWarning($"[DungeonLightingController] URP Rendering Layer '{layerName}' not found. " +
                                 "Check Edit → Project Settings → Graphics → URP Global Settings → Rendering Layers.");
            return mask;
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            // Friendly reminder in the Inspector if the layer names look wrong.
            if (!string.IsNullOrEmpty(worldLayerName) &&
                RenderingLayerMask.GetMask(worldLayerName) == 0)
                Debug.LogWarning($"[DungeonLightingController] WorldLayer '{worldLayerName}' not found in URP settings.");

            if (!string.IsNullOrEmpty(dungeonLayerName) &&
                RenderingLayerMask.GetMask(dungeonLayerName) == 0)
                Debug.LogWarning($"[DungeonLightingController] DungeonLayer '{dungeonLayerName}' not found in URP settings.");
        }
#endif
    }
}
