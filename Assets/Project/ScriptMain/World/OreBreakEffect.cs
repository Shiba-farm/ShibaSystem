// OreBreakEffect.cs
// รองรับ 2 โหมด:
//   - Animation Mode : ใช้ AnimationClip ที่มีอยู่แล้ว (เล่นผ่าน Animator)
//   - Physics Mode   : ยิง debris pieces ด้วย Rigidbody (fallback)
// วาง component นี้บน Prefab ที่ assign ใน RockObject → Destroy VFX

using System.Collections;
using UnityEngine;

public class OreBreakEffect : MonoBehaviour
{
    // ────────────────────────────────────────────────────────────────────────────
    [Header("=== Animation Mode (แนะนำ) ===")]
    [Tooltip("Animator ของ GameObject นี้ (หรือ Child) ที่มี Clip แร่แตก")]
    [SerializeField] private Animator breakAnimator;
    [Tooltip("ชื่อ State หรือ Trigger ใน Animator Controller  (เช่น 'Break')")]
    [SerializeField] private string   breakStateName = "Break";
    [Tooltip("ความยาว Animation เป็นวินาที — ใช้หา Destroy timing อัตโนมัติ (0 = อ่านจาก Clip)")]
    [SerializeField] private float    animDuration   = 0f;

    // ────────────────────────────────────────────────────────────────────────────
    [Header("=== Physics Mode (ใช้เมื่อไม่มี Animator) ===")]
    [Tooltip("ลาก Child GameObject ชิ้นส่วนแร่มาใส่  (ต้องมี Rigidbody)")]
    [SerializeField] private GameObject[] debrisPieces;
    [SerializeField] private float minForce     = 2f;
    [SerializeField] private float maxForce     = 5f;
    [SerializeField] private float upwardBias   = 0.6f;
    [SerializeField] private float torqueForce  = 8f;
    [SerializeField] private float debrisLife   = 2.5f;
    [SerializeField] private float fadeStartAt  = 1.5f;

    // ────────────────────────────────────────────────────────────────────────────
    [Header("=== Dust Particle (ใส่ทั้ง 2 โหมด) ===")]
    [Tooltip("ParticleSystem สำหรับฝุ่น — ใส่หรือไม่ใส่ก็ได้")]
    [SerializeField] private ParticleSystem dustParticle;

    // ────────────────────────────────────────────────────────────────────────────
    private void Awake() => PlayBreak();

    public void PlayBreak()
    {
        // เล่น Particle ฝุ่นเสมอ (ถ้ามี)
        if (dustParticle != null)
        {
            dustParticle.transform.SetParent(null); // ถอด parent → ไม่หายพร้อม Destroy
            dustParticle.Play();
            Destroy(dustParticle.gameObject, dustParticle.main.duration + 0.5f);
        }

        if (breakAnimator != null)
            StartCoroutine(AnimationMode());
        else
            StartCoroutine(PhysicsMode());
    }

    // ────────────────────────────────────────────────────────────────────────────
    // Animation Mode
    // ────────────────────────────────────────────────────────────────────────────
    private IEnumerator AnimationMode()
    {
        // เล่น State โดยตรง (ใช้ Play แทน SetTrigger เพื่อความแน่นอน)
        breakAnimator.Play(breakStateName, 0, 0f);

        // คำนวณ duration จาก Clip ถ้าไม่ได้ตั้งค่าไว้
        float duration = animDuration;
        if (duration <= 0f)
        {
            // รอ 1 frame ให้ Animator อัปเดต state info
            yield return null;
            var info = breakAnimator.GetCurrentAnimatorStateInfo(0);
            duration = info.length;
        }

        yield return new WaitForSeconds(duration);
        Destroy(gameObject);
    }

    // ────────────────────────────────────────────────────────────────────────────
    // Physics Mode (fallback)
    // ────────────────────────────────────────────────────────────────────────────
    private IEnumerator PhysicsMode()
    {
        foreach (var piece in debrisPieces)
        {
            if (piece == null) continue;
            piece.SetActive(true);
            piece.transform.SetParent(null);

            if (piece.TryGetComponent<Rigidbody>(out var rb))
            {
                Vector3 dir = new Vector3(
                    Random.Range(-1f, 1f),
                    upwardBias + Random.Range(0f, 0.4f),
                    Random.Range(-1f, 1f)).normalized;
                rb.AddForce(dir * Random.Range(minForce, maxForce), ForceMode.Impulse);
                rb.AddTorque(Random.insideUnitSphere * torqueForce, ForceMode.Impulse);
            }
            StartCoroutine(FadeAndDestroy(piece));
        }

        yield return new WaitForSeconds(debrisLife + 0.2f);
        Destroy(gameObject);
    }

    private IEnumerator FadeAndDestroy(GameObject piece)
    {
        yield return new WaitForSeconds(fadeStartAt);

        var matList = new System.Collections.Generic.List<Material>();
        foreach (var r in piece.GetComponentsInChildren<Renderer>())
            foreach (var mat in r.materials)
            {
                mat.SetFloat("_Mode", 3);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.renderQueue = 3000;
                matList.Add(mat);
            }

        float elapsed = 0f;
        float fadeDur = debrisLife - fadeStartAt;
        while (elapsed < fadeDur)
        {
            float a = Mathf.Lerp(1f, 0f, elapsed / fadeDur);
            foreach (var mat in matList) { Color c = mat.color; c.a = a; mat.color = c; }
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (piece != null) Destroy(piece);
    }
}
