// DungeonDeathHandler.cs
// Phase B — Personal dungeon instancing.
//
// จัดการตาย: เล่น animation → Fade ดำ → ออกจาก dungeon ของผู้เล่นคนนี้ (ถ้าอยู่) → ฟื้น HP → Fade กลับ
//
// Previously this reloaded the whole scene via SceneManager.LoadScene() to
// "return" the player to the farm — that relied on the Dungeon being a
// separate NGO scene (Phase 1). The dungeon is now an always-loaded,
// per-player instance area within the SAME scene, so reloading the scene
// would incorrectly reset every system for every player on this client.
//
// Instead, death now:
//   1. Fades to black (local UI only).
//   2. Calls PlayerDungeonState.RequestExitDungeonServerRpc() — an
//      Owner→Server RPC that only ever affects THIS player: the server clears
//      their floor, marks them as no longer in the dungeon, and teleports
//      ONLY them back to their entry point via TeleportOwnerRpc. No other
//      connected player is moved, has their floor changed, or sees any UI.
//   3. Restores HP locally via PlayerHealth.Revive() (no scene reload to do
//      this for us any more).
//   4. Fades back in.
//
// If the player wasn't in a dungeon when they died, step 2 is skipped and
// they simply revive where they stand.

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using MyGame.Dungeon;

public class DungeonDeathHandler : MonoBehaviour
{
    [Header("Fade UI")]
    public Image  fadeImage;         // Image สีดำ เต็มจอ (ต้องสร้างใน Canvas)
    public float  fadeDuration = 1f;

    [Header("Timing")]
    [Tooltip("รอกี่วิหลังตายก่อนเริ่ม Fade")]
    public float  deathPauseDuration = 1.5f;

    [Tooltip("รอกี่วิหลัง fade ดำ ก่อนฟื้นและ fade กลับ (ให้เวลา teleport กลับถึงที่ก่อน)")]
    public float  reviveDelay = 0.5f;

    [Header("Animator Parameter")]
    [Tooltip("ชื่อ Trigger parameter ท่าตาย (ถ้ายังไม่มีให้ว่างไว้ได้)")]
    public string dieAnimTrigger = "Die";   // ← เพิ่ม trigger นี้ใน Animator ทีหลัง

    private Animator playerAnimator;
    private PlayerDungeonState dungeonState;

    // ──────────────────────────────────────────────────────────────────────
    void OnEnable()  => PlayerHealth.OnDeath += HandleDeath;
    void OnDisable() => PlayerHealth.OnDeath -= HandleDeath;

    void Start()
    {
        // ใช้ LocalPlayerUtil แทน FindGameObjectWithTag ตรงๆ — กันจับตัวละครผู้เล่นคนอื่นผิดตัวตอน Multiplayer
        var playerTransform = LocalPlayerUtil.GetLocalPlayerTransform();
        if (playerTransform != null)
        {
            playerAnimator = playerTransform.GetComponent<Animator>();
            dungeonState   = playerTransform.GetComponent<PlayerDungeonState>();
        }

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

        // 4. ออกจาก dungeon ของตัวเอง (ถ้าอยู่) — ไม่กระทบผู้เล่นคนอื่น
        if (dungeonState != null && dungeonState.IsInDungeon)
            dungeonState.RequestExitDungeonServerRpc();

        // รอให้ teleport กลับถึงที่ก่อนฟื้น
        yield return new WaitForSeconds(reviveDelay);

        // 5. ฟื้น HP
        PlayerHealth.Instance?.Revive();

        // 6. Fade กลับ
        t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            SetFadeAlpha(1f - Mathf.Clamp01(t / fadeDuration));
            yield return null;
        }
        SetFadeAlpha(0f);
    }

    void SetFadeAlpha(float a)
    {
        if (!fadeImage) return;
        var c = fadeImage.color;
        c.a = a;
        fadeImage.color = c;
    }
}
