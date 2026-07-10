// DungeonLadderBeacon.cs
// World-space visual marker attached as a child of the Ladder prefab.
//
// Purpose
// ───────
// Gives players a long-range visual cue so they can always locate the Ladder
// from anywhere on the floor — without needing to open a map or rely on the
// Scene View.
//
// Setup (one-time, in the Ladder prefab)
// ──────────────────────────────────────
// 1. Add a child GameObject called "Beacon" under the Ladder root.
// 2. Add this component to "Beacon".
// 3. Add a child ParticleSystem under "Beacon" for the rising beam effect
//    and assign it to BeamParticles.
// 4. Optionally add a Point Light under "Beacon" and assign to BeaconLight.
// 5. Optionally assign a Billboard child renderer to BeaconSprite (e.g. a
//    glowing "▼" quad that always faces the camera).
//
// No networking required — this component is purely cosmetic.  The Ladder
// itself is a NetworkObject; all clients will instantiate and see the beacon
// automatically when NGO spawns the ladder on their machine.

using UnityEngine;

namespace MyGame.Dungeon
{
    public class DungeonLadderBeacon : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────

        [Header("Visuals")]
        [Tooltip("Particle system for the rising light column. Assign the child PS here.")]
        public ParticleSystem beamParticles;

        [Tooltip("Optional pulsing point light on the ladder.")]
        public Light beaconLight;

        [Tooltip("Optional billboard quad / sprite renderer (icon above ladder).")]
        public Renderer beaconSprite;

        [Header("Light Pulse")]
        [Tooltip("How fast the light pulses. 0 = no pulse.")]
        public float pulseSpeed    = 1.8f;
        public float minIntensity  = 0.6f;
        public float maxIntensity  = 2.4f;

        [Header("Sprite Bob")]
        [Tooltip("How far the billboard icon bobs up and down.")]
        public float bobAmount = 0.12f;
        public float bobSpeed  = 1.5f;

        // ── Private ───────────────────────────────────────────────────

        private Vector3 _spriteOriginLocal;

        // ── Lifecycle ─────────────────────────────────────────────────

        void Start()
        {
            if (beamParticles != null && !beamParticles.isPlaying)
                beamParticles.Play();

            if (beaconSprite != null)
                _spriteOriginLocal = beaconSprite.transform.localPosition;
        }

        void Update()
        {
            float t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f; // 0..1

            // Pulse light intensity.
            if (beaconLight != null)
                beaconLight.intensity = Mathf.Lerp(minIntensity, maxIntensity, t);

            // Bob the billboard icon.
            if (beaconSprite != null)
            {
                Vector3 pos = _spriteOriginLocal;
                pos.y += Mathf.Sin(Time.time * bobSpeed) * bobAmount;
                beaconSprite.transform.localPosition = pos;
            }
        }

        void OnDisable()
        {
            if (beamParticles != null && beamParticles.isPlaying)
                beamParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        void OnEnable()
        {
            if (beamParticles != null && !beamParticles.isPlaying)
                beamParticles.Play();
        }
    }
}
