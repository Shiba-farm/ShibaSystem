// AutoDungeonLayerSetter.cs
// Attach this to any parent GameObject that contains dungeon environment geometry
// (e.g. DungeonBox, DungeonZone, DungeonEntrance surrounding walls).
//
// On Awake it sets every child Renderer's URP Rendering Layer Mask to DungeonLayer,
// which prevents the farm's Directional Light (Sun) from illuminating these objects.
//
// Why is this needed?
// ──────────────────
// DungeonManager.SetDungeonRenderingLayer() only runs on SPAWNED tiles and
// network objects.  Scene-placed objects (DungeonBox, DungeonZone walls, etc.)
// keep Unity's default Rendering Layer ("Default"), so the Sun — which includes
// "Default" in its Rendering Layer Mask — still illuminates them.
// Adding this component to those objects fixes the leak without any extra setup.
//
// Setup
// ─────
// 1. Select DungeonBox (and/or DungeonZone, DungeonEntrance) in the Hierarchy.
// 2. Add Component → AutoDungeonLayerSetter.
// 3. Confirm "Dungeon Layer Name" matches the name you added in
//    Project Settings → Graphics → URP Global Settings → Rendering Layers
//    (default: "DungeonLayer").
// 4. Press Play — all child Renderers will be moved to DungeonLayer automatically.

using UnityEngine;

namespace MyGame.Dungeon
{
    public class AutoDungeonLayerSetter : MonoBehaviour
    {
        [Tooltip("Name of the URP Rendering Layer to apply (must match URP Global Settings → Rendering Layers).")]
        public string dungeonLayerName = "DungeonLayer";

        void Awake()
        {
            uint mask = RenderingLayerMask.GetMask(dungeonLayerName);
            if (mask == 0)
            {
                Debug.LogWarning($"[AutoDungeonLayerSetter] URP Rendering Layer '{dungeonLayerName}' not found on '{name}'. " +
                                 "Check Project Settings → Graphics → URP Global Settings → Rendering Layers.");
                return;
            }

            var renderers = GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
                r.renderingLayerMask = mask;

            Debug.Log($"[AutoDungeonLayerSetter] '{name}': set {renderers.Length} renderer(s) → {dungeonLayerName}.");
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (!string.IsNullOrEmpty(dungeonLayerName) && RenderingLayerMask.GetMask(dungeonLayerName) == 0)
                Debug.LogWarning($"[AutoDungeonLayerSetter] '{dungeonLayerName}' not found in URP Rendering Layers.");
        }
#endif
    }
}
