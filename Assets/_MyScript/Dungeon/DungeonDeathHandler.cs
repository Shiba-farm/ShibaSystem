// DungeonDeathHandler.cs
// จัดการตาย: เล่น animation → Fade ดำ → กลับ Scene ฟาร์ม
// วางไว้ใน Dungeon Scene (ร่วมกับ DungeonManager)

using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DungeonDeathHandler : MonoBehaviour
{
    [Header("Fade UI")]
    public Image  fadeImage;         // Image สีดำ เต็มจอ (ต้องสร้างใน Canvas)
    public float  fadeDuration = 1f;

    [Header("Timing")]
    [Tooltip("รอกี่วิหลังตายก่อนเริ่ม Fade")]
    public float  deathPauseDuration = 1.5f;

    [Header("Animator Parameter")]
    [Tooltip("ชื่อ Trigger parameter ท่าตาย (ถ้ายังไม่มีให้ว่างไว้ได้)")]
    public string dieAnimTrigger = "Die";   // ← เพิ่ม trigger นี้ใน Animator ทีหลัง

    private Animator playerAnimator;

    // ──────────────────────────────────────────────────────────────────────
    void OnEnable()  => PlayerHealth.OnDeath += HandleDeath;
    void OnDisable() => PlayerHealth.OnDeath -= HandleDeath;

    void Start()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player) playerAnimator = player.GetComponent<Animator>();

        // Reset fade image ให้ใสตอนเริ่ม
        SetFadeAlpha(0f);
    }

    // ──────────────────────────────────────────────────────────────────────
    void HandleDeath()
    {
        StartCoroutine(DeathSequence());
    }

    IEnumerator DeathSequence()
    {
        // 1. เล่น animation ตาย (ถ้ามี trigger)
        if (playerAnimator && !string.IsNullOrEmpty(dieAnimTrigger))
        {
            // ตรวจว่า parameter มีอยู่ใน Animator ไหม
            foreach (var p in playerAnimator.parameters)
            {
                if (p.name == dieAnimTrigger && p.type == AnimatorControllerParameterType.Trigger)
                {
                    playerAnimator.SetTrigger(dieAnimTrigger);
                    break;
                }
            }
        }

        // 2. รอ
        yield return new WaitForSeconds(deathPauseDuration);

        // 3. Fade to black
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            SetFadeAlpha(Mathf.Clamp01(t / fadeDuration));
            yield return null;
        }

        // 4. บันทึกว่าตายมาจาก dungeon
        DungeonReturnData.returnFromDeath = true;

        // 5. กลับ Farm Scene
        SceneManager.LoadScene(DungeonReturnData.farmScene);
    }

    void SetFadeAlpha(float a)
    {
        if (!fadeImage) return;
        var c = fadeImage.color;
        c.a = a;
        fadeImage.color = c;
    }
}
