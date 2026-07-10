// LadderDirectionArrow.cs
// Per-player proximity arrow that orbits the player model in 3D, always
// pointing toward the nearest DungeonLadder on the current floor.
//
// Behaviour summary
// ─────────────────
// • Only the owning client runs this logic — non-owners see nothing.
// • The arrow is INVISIBLE until the player is within DetectionRadius of a ladder.
// • When a ladder enters range, the arrow object is placed at a fixed orbit
//   distance from the player and rotates on the XZ plane to face the ladder.
// • The arrow bobs gently up and down so it reads well against 3D geometry.
// • When the player is very close to the ladder (AtLadderRadius), the arrow
//   hides — the player has found it.
// • If the player leaves the dungeon or there is no ladder in range, the
//   arrow hides automatically.
//
// This pairs with DungeonLadderBeacon (world-space beam on the Ladder) to
// form a two-tier navigation system:
//   Far   → glowing beam above ladder visible across the whole floor.
//   Close → orbiting arrow helps the player zero-in precisely.
//
// Setup (one-time, on the Player prefab)
// ──────────────────────────────────────
// 1. Add this component to the Player root GameObject (same object that has
//    PlayerDungeonState and NetworkObject).
// 2. Create a child GameObject (e.g. "LadderArrow") with your arrow mesh or
//    sprite. Make it a sibling — NOT a child — of this component's transform
//    in the hierarchy so it can be freely repositioned each frame without
//    fighting the player's own transform hierarchy.  Alternatively keep it as
//    a child and un-parent it at runtime (the component does this for you if
//    UnparentOnStart is true).
// 3. Assign that GameObject to the ArrowObject field in the Inspector.
// 4. Tune DetectionRadius, OrbitRadius, ArrowHeight, and AtLadderRadius to
//    taste.
//
// Layer requirement
// ─────────────────
// DungeonLadder.cs already sets the ladder's layer to "Interact" in
// OnNetworkSpawn.  This component searches that same layer, so no extra
// setup is needed.

using UnityEngine;
using Unity.Netcode;

namespace MyGame.Dungeon
{
    [RequireComponent(typeof(PlayerDungeonState))]
    public class LadderDirectionArrow : NetworkBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────

        [Header("Arrow Object")]
        [Tooltip("The GameObject that represents the arrow (mesh, sprite, or particle system). " +
                 "Assign a child of the Player prefab. It will be unparented at runtime so it " +
                 "can move independently.")]
        public GameObject arrowObject;

        [Tooltip("If true, the arrow is un-parented from the player at start so its position " +
                 "can be set freely each frame without inheriting scale/rotation from the player rig.")]
        public bool unparentOnStart = true;

        [Header("Detection")]
        [Tooltip("Radius (metres) within which the arrow appears. " +
                 "Beyond this range the Ladder Beacon beam is the navigation aid.")]
        public float detectionRadius = 14f;

        [Tooltip("Radius (metres) at which the player is considered 'at' the ladder " +
                 "and the arrow hides — they've found it.")]
        public float atLadderRadius = 1.8f;

        [Header("Arrow Positioning")]
        [Tooltip("Distance from the player's pivot the arrow floats at (orbit radius).")]
        public float orbitRadius = 1.2f;

        [Tooltip("Height above the player's pivot point.")]
        public float arrowHeight = 1.1f;

        [Header("Bob Animation")]
        public float bobSpeed  = 2.2f;
        public float bobAmount = 0.08f;

        [Header("Smooth Turn")]
        [Tooltip("How fast (degrees/sec) the arrow rotates to track the new ladder direction. " +
                 "Higher = snappier. Use 0 for instant.")]
        public float turnSpeed = 360f;

        // ── Private ───────────────────────────────────────────────────

        private PlayerDungeonState _dungeonState;
        private DungeonLadder      _cachedLadder;

        private float _searchTimer;
        private const float SearchInterval = 0.25f; // seconds between OverlapSphere calls

        private Quaternion _currentArrowRotation = Quaternion.identity;
        private bool       _arrowVisible;

        // ── Lifecycle ─────────────────────────────────────────────────

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            _dungeonState = GetComponent<PlayerDungeonState>();

            if (!IsOwner)
            {
                // Non-owning clients: completely disable. The arrow is cosmetic
                // and personal — other players should not see your direction hint.
                enabled = false;
                if (arrowObject != null) arrowObject.SetActive(false);
                return;
            }

            if (arrowObject != null)
            {
                // Un-parent so position is set in world-space without
                // fighting the player rig's transform hierarchy.
                if (unparentOnStart)
                    arrowObject.transform.SetParent(null, true);

                arrowObject.SetActive(false);
            }
        }

        void Update()
        {
            if (!IsOwner) return;

            // Hide while not in dungeon.
            if (_dungeonState == null || !_dungeonState.IsInDungeon)
            {
                SetVisible(false);
                _cachedLadder = null;
                return;
            }

            // Refresh nearest ladder on a timer to avoid OverlapSphere every frame.
            _searchTimer -= Time.deltaTime;
            if (_searchTimer <= 0f)
            {
                _searchTimer = SearchInterval;
                RefreshNearestLadder();
            }

            UpdateArrow();
        }

        // ── Cleanup ───────────────────────────────────────────────────

        public override void OnNetworkDespawn()
        {
            // Destroy the un-parented arrow object so it doesn't linger in
            // the scene after the player disconnects.
            if (arrowObject != null)
                Destroy(arrowObject);

            base.OnNetworkDespawn();
        }

        void OnDestroy()
        {
            if (arrowObject != null)
                Destroy(arrowObject);
        }

        // ── Core Logic ────────────────────────────────────────────────

        /// <summary>
        /// OverlapSphere search for the nearest DungeonLadder within detection range.
        /// Runs every SearchInterval seconds to keep CPU cost low.
        /// </summary>
        private void RefreshNearestLadder()
        {
            _cachedLadder = null;
            float bestDist = detectionRadius;

            // Ladders are placed on the "Interact" layer by DungeonLadder.OnNetworkSpawn.
            var interactMask = LayerMask.GetMask("Interact");
            var hits = Physics.OverlapSphere(transform.position, detectionRadius, interactMask);

            foreach (var col in hits)
            {
                var ladder = col.GetComponent<DungeonLadder>();
                if (ladder == null) continue;

                float dist = Vector3.Distance(transform.position, ladder.transform.position);
                if (dist < bestDist)
                {
                    bestDist  = dist;
                    _cachedLadder = ladder;
                }
            }
        }

        /// <summary>
        /// Positions and rotates the arrow to orbit the player, pointing at the ladder.
        /// </summary>
        private void UpdateArrow()
        {
            if (_cachedLadder == null)
            {
                SetVisible(false);
                return;
            }

            float distToLadder = Vector3.Distance(transform.position, _cachedLadder.transform.position);

            // Hide when player is standing on top of the ladder.
            if (distToLadder <= atLadderRadius)
            {
                SetVisible(false);
                return;
            }

            SetVisible(true);

            if (arrowObject == null) return;

            // ── Direction (XZ only — dungeon floors are flat) ──
            Vector3 toTarget = _cachedLadder.transform.position - transform.position;
            toTarget.y = 0f;

            if (toTarget.sqrMagnitude < 0.001f) return;
            toTarget.Normalize();

            // ── Smooth rotation ──
            Quaternion targetRot = Quaternion.LookRotation(toTarget, Vector3.up);
            if (turnSpeed > 0f)
                _currentArrowRotation = Quaternion.RotateTowards(
                    _currentArrowRotation, targetRot, turnSpeed * Time.deltaTime);
            else
                _currentArrowRotation = targetRot;

            // ── Bob ──
            float bob = Mathf.Sin(Time.time * bobSpeed) * bobAmount;

            // ── World-space position: orbiting the player ──
            Vector3 orbitDir = _currentArrowRotation * Vector3.forward;
            arrowObject.transform.position =
                transform.position
                + orbitDir * orbitRadius
                + Vector3.up * (arrowHeight + bob);

            arrowObject.transform.rotation = _currentArrowRotation;
        }

        // ── Helpers ───────────────────────────────────────────────────

        private void SetVisible(bool visible)
        {
            if (_arrowVisible == visible) return;
            _arrowVisible = visible;
            if (arrowObject != null) arrowObject.SetActive(visible);
        }

#if UNITY_EDITOR
        // ── Editor Gizmos (visible only in the Unity Editor) ─────────

        void OnDrawGizmosSelected()
        {
            // Detection radius — yellow sphere.
            Gizmos.color = new Color(1f, 1f, 0f, 0.15f);
            Gizmos.DrawSphere(transform.position, detectionRadius);
            Gizmos.color = new Color(1f, 1f, 0f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, detectionRadius);

            // "At ladder" radius — green sphere (hide arrow zone).
            Gizmos.color = new Color(0f, 1f, 0.3f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, atLadderRadius);

            // Orbit radius — white circle approximation.
            Gizmos.color = new Color(1f, 1f, 1f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, orbitRadius);
        }
#endif
    }
}
