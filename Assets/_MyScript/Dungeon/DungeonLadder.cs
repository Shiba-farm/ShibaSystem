// DungeonLadder.cs
// บันไดที่ player กด E เพื่อลงชั้นถัดไป

using UnityEngine;

namespace MyGame.Dungeon
{
    public class DungeonLadder : MonoBehaviour
    {
        [Header("Interaction")]
        public float interactRadius = 1.5f;
        public KeyCode interactKey  = KeyCode.E;

        [Header("UI Prompt (Optional)")]
        public GameObject promptUI;         // ป้าย "[E] ลงชั้นถัดไป"

        private DungeonManager manager;
        private Transform playerTransform;
        private bool playerNearby = false;

        // ──────────────────────────────────────────────────────────────────────
        // Setup
        // ──────────────────────────────────────────────────────────────────────

        public void Setup(DungeonManager mgr)
        {
            manager = mgr;
            // playerTransform = mgr.player;
        }

        // ──────────────────────────────────────────────────────────────────────
        // Runtime
        // ──────────────────────────────────────────────────────────────────────

        void Update()
        {
            if (manager == null || playerTransform == null) return;

            float dist = Vector3.Distance(transform.position, playerTransform.position);
            playerNearby = dist <= interactRadius;

            if (promptUI) promptUI.SetActive(playerNearby);

            if (playerNearby && Input.GetKeyDown(interactKey))
            {
                manager.GoNextFloor();
            }
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, interactRadius);
        }
    }
}
