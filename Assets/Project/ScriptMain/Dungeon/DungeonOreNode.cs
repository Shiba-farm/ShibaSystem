// DungeonOreNode.cs
// แร่ใน Dungeon — คลิก Left Mouse → เล่น Animation ขุด → Drop แร่ → ดูดเข้าตัว

using System.Collections;
using UnityEngine;

namespace MyGame.Dungeon
{
    public class DungeonOreNode : MonoBehaviour
    {
        [Header("Interaction")]
        public float interactRadius = 2f;   // ระยะที่คลิกแล้วขุดได้

        [Header("UI Prompt (Optional)")]
        public GameObject promptUI;          // ป้าย "[คลิก] ขุดแร่"

        [Header("Mining Animation")]
        [Tooltip("ชื่อ Trigger ใน Animator ของ Player")]
        public string miningAnimTrigger = "Dig";
        [Tooltip("รอกี่วิก่อน drop แร่ (ควรตรงกับความยาว animation)")]
        public float  miningDuration    = 1.2f;

        [Header("Ore Drop Prefab")]
        [Tooltip("Prefab ที่มี OrePickup component — ถ้าว่างจะเพิ่มเข้า Hotbar ตรงๆ")]
        public GameObject orePickupPrefab;
        [Tooltip("ความสูง Y ที่ drop แร่")]
        public float dropYOffset = 0.5f;

        // ──────────────────────────────────────────────────────────────────────
        // Runtime
        // ──────────────────────────────────────────────────────────────────────
        private DungeonOreSO   oreData;
        private Vector2Int     gridPos;
        private DungeonManager manager;
        private ulong          ownerClientId;
        private Transform      playerTransform;
        private Animator       playerAnimator;
        private bool           isMining  = false;
        private bool           harvested = false;

        // ──────────────────────────────────────────────────────────────────────
        // Setup
        // ──────────────────────────────────────────────────────────────────────
        // Phase B: ownerClientId records which player's personal dungeon instance
        // this ore belongs to, so MineSequence() reports the harvest to that
        // player's DungeonFloorData (via DungeonManager.OnOreHarvested) and not
        // some other player's.
        public void Setup(DungeonOreSO ore, Vector2Int pos, DungeonManager mgr, ulong ownerClientId)
        {
            oreData            = ore;
            gridPos            = pos;
            manager            = mgr;
            this.ownerClientId = ownerClientId;
            // playerTransform = mgr.player;

            if (playerTransform)
                playerAnimator = playerTransform.GetComponent<Animator>();
        }

        // ──────────────────────────────────────────────────────────────────────
        // Update — ตรวจจับคลิก Left Mouse ขณะอยู่ใกล้แร่
        // ──────────────────────────────────────────────────────────────────────
        void Update()
        {
            if (harvested || isMining || playerTransform == null) return;

            float dist = Vector2.Distance(
                new Vector2(transform.position.x, transform.position.z),
                new Vector2(playerTransform.position.x, playerTransform.position.z));

            bool nearby = dist <= interactRadius;
            if (promptUI) promptUI.SetActive(nearby);

            // คลิก Left Mouse ขณะอยู่ในระยะ
            if (nearby && Input.GetMouseButtonDown(0))
                StartCoroutine(MineSequence());
        }

        // ──────────────────────────────────────────────────────────────────────
        // Mining Coroutine
        // ──────────────────────────────────────────────────────────────────────
        private IEnumerator MineSequence()
        {
            isMining = true;
            if (promptUI) promptUI.SetActive(false);

            // หัน player มาหาแร่
            if (playerTransform)
            {
                Vector3 dir = (transform.position - playerTransform.position);
                dir.y = 0;
                if (dir != Vector3.zero)
                    playerTransform.rotation = Quaternion.LookRotation(dir);
            }

            // เล่น animation ขุด
            if (playerAnimator && !string.IsNullOrEmpty(miningAnimTrigger))
                playerAnimator.SetTrigger(miningAnimTrigger);

            // รอให้ animation เล่นจบ
            yield return new WaitForSeconds(miningDuration);

            // Drop แร่
            DropOre();

            // แจ้ง Manager
            manager?.OnOreHarvested(gridPos, ownerClientId);
            harvested = true;

            Destroy(gameObject);
        }

        // ──────────────────────────────────────────────────────────────────────
        // Drop Ore
        // ──────────────────────────────────────────────────────────────────────
        private void DropOre()
        {
            if (oreData == null) return;

            int amount = oreData.GetRandomYield();

            if (orePickupPrefab != null)
            {
                Vector3 dropPos = transform.position + Vector3.up * dropYOffset;
                var go = Instantiate(orePickupPrefab, dropPos, Quaternion.identity);
                var pickup = go.GetComponent<OrePickup>();
                if (pickup)
                {
                    pickup.item            = oreData.dropItem;
                    pickup.amount          = amount;
                    pickup.targetTransform = playerTransform; // ส่ง player ให้ดูดหาทันที
                }
            }
            else
            {
                // Fallback: เพิ่มเข้า Hotbar ตรงๆ
                // if (oreData.dropItem != null && HotbarUI.Instance != null)
                //     HotbarUI.Instance.AddItemToFirstEmptySlot(oreData.dropItem, amount);
            }
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, interactRadius);
        }
    }
}
