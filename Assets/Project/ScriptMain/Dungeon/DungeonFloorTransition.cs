// DungeonFloorTransition.cs
// Fade ดำ + แสดง "ชั้น X" เมื่อเปลี่ยนชั้น Dungeon
// ต้องการ Canvas (Screen Space - Overlay) ที่มี Image (FadePanel) และ TextMeshProUGUI (FloorText)

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MyGame.Dungeon
{
    public class DungeonFloorTransition : MonoBehaviour
    {
        // ──────────────────────────────────────────────────────────────────────
        // Singleton
        // ──────────────────────────────────────────────────────────────────────
        public static DungeonFloorTransition Instance { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            // Hide both UI elements at startup — they are only shown during
            // floor transitions (ShowFloorText / FadeIn / FadeOut).
            if (floorText != null) floorText.gameObject.SetActive(false);
            if (fadePanel != null) fadePanel.gameObject.SetActive(false);
        }

        // ──────────────────────────────────────────────────────────────────────
        // Inspector
        // ──────────────────────────────────────────────────────────────────────
        [Header("UI References")]
        [Tooltip("Image สีดำเต็มหน้าจอ — ใส่ Image component ที่ stretch เต็ม Canvas")]
        public Image        fadePanel;

        [Tooltip("TextMeshPro แสดงชื่อชั้น เช่น  ชั้น 2")]
        public TextMeshProUGUI floorText;

        [Header("Timing (วินาที)")]
        public float fadeDuration  = 0.4f;   // ความเร็ว fade เข้า/ออก
        public float holdDuration  = 1.2f;   // นานแค่ไหนที่แสดงข้อความ

        [Header("Font Size")]
        public float fontSize = 72f;

        // ──────────────────────────────────────────────────────────────────────
        // Public API — เรียกจาก DungeonManager
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>Fade ดำ — เรียกก่อนเปลี่ยนชั้น</summary>
        public IEnumerator FadeIn()
        {
            if (fadePanel == null) yield break;
            fadePanel.gameObject.SetActive(true);
            yield return StartCoroutine(SetAlpha(0f, 1f, fadeDuration));
        }

        /// <summary>แสดงข้อความ "ชั้น X" กลางจอ</summary>
        public void ShowFloorText(int floor)
        {
            if (floorText == null) return;
            floorText.fontSize = fontSize;
            floorText.text = $"ชั้น {floor}";
            floorText.gameObject.SetActive(true);
        }

        /// <summary>Fade กลับ + ซ่อนข้อความ</summary>
        public IEnumerator FadeOut()
        {
            if (floorText != null) floorText.gameObject.SetActive(false);
            if (fadePanel == null) yield break;
            yield return StartCoroutine(SetAlpha(1f, 0f, fadeDuration));
            fadePanel.gameObject.SetActive(false);
        }

        // ──────────────────────────────────────────────────────────────────────
        // Internal
        // ──────────────────────────────────────────────────────────────────────
        private IEnumerator SetAlpha(float from, float to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float a = Mathf.Lerp(from, to, t);
                if (fadePanel)
                {
                    Color c = fadePanel.color;
                    fadePanel.color = new Color(c.r, c.g, c.b, a);
                }
                yield return null;
            }
            if (fadePanel)
            {
                Color c = fadePanel.color;
                fadePanel.color = new Color(c.r, c.g, c.b, to);
            }
        }
    }
}
