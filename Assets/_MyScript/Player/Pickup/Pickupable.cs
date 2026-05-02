using System.Collections;
using UnityEngine;

/// <summary>
/// ติดไว้บน GameObject ที่เก็บได้จากพื้น
/// - มี ItemSO + จำนวน
/// - ลอยขึ้นลง (Floating animation)
/// - เล่น VFX + SFX ตอนเก็บ
///
/// [Fix] ถ้ามี Rigidbody → รอให้ physics หยุดก่อน (floatDelay วินาที)
/// แล้วค่อยทำ Kinematic + เริ่ม floating animation
/// ป้องกัน Log ลอยหนีไปเรื่อยๆ จาก velocity ที่ค้างอยู่
/// </summary>
public class Pickupable : MonoBehaviour
{
    [Header("Item")]
    public ItemSO itemData;
    [Min(1)]
    public int amount = 1;

    [Header("Floating Animation")]
    public bool enableFloat = true;
    public float floatAmplitude = 0.15f;   // ความสูงที่ลอยขึ้นลง
    public float floatSpeed     = 2f;       // ความเร็วการลอย
    public float rotateSpeed    = 90f;      // หมุนกี่องศาต่อวินาที (0 = ไม่หมุน)
    [Tooltip("รอกี่วินาทีก่อนเริ่ม Float (ให้ physics ตกพื้นก่อน)")]
    public float floatDelay     = 1.2f;

    [Header("VFX / SFX")]
    [Tooltip("Particle ที่เล่นตอนเก็บ — ถ้าว่างจะข้าม")]
    public GameObject pickupVFX;
    [Tooltip("เสียงตอนเก็บ")]
    public AudioClip pickupSFX;
    [Range(0f, 1f)]
    public float sfxVolume = 0.8f;

    // Runtime
    Vector3 _startPos;
    float   _floatTimer;
    bool    _floatReady = false;   // true = physics หยุดแล้ว เริ่ม float ได้

    void Start()
    {
        _floatTimer = Random.Range(0f, Mathf.PI * 2f);

        var rb = GetComponent<Rigidbody>();

        if (rb != null && enableFloat)
        {
            // มี Rigidbody → รอให้กระเด็นหยุดก่อน แล้วค่อย Kinematic + float
            _floatReady = false;
            StartCoroutine(WaitThenFloat(rb));
        }
        else
        {
            // ไม่มี Rigidbody → เริ่ม float ทันที
            _startPos   = transform.position;
            _floatReady = true;
        }
    }

    IEnumerator WaitThenFloat(Rigidbody rb)
    {
        // รอขั้นต่ำก่อนเริ่มตรวจ (ให้ explosion force ออกแรงก่อน)
        yield return new WaitForSeconds(0.3f);

        // รอจนกว่า Log หยุดนิ่งจริงๆ (velocity ≈ 0)
        // มี timeout 6 วินาทีป้องกันค้างไว้
        float timeout = 6f;
        float elapsed = 0f;

        while (elapsed < timeout)
        {
            if (rb == null) yield break;

            bool velocityStopped = rb.velocity.magnitude < 0.05f
                                && rb.angularVelocity.magnitude < 0.05f;
            if (velocityStopped) break;

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (rb == null) yield break;

        // หยุด velocity ทั้งหมด แล้วเปลี่ยนเป็น Kinematic
        rb.velocity        = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic     = true;

        // จำตำแหน่งที่ตกพื้นจริง (ไม่ใช่ตำแหน่งตอน spawn)
        _startPos   = transform.position;
        _floatReady = true;
    }

    void Update()
    {
        if (!enableFloat || !_floatReady) return;

        // ลอยขึ้นลง
        _floatTimer += Time.deltaTime * floatSpeed;
        float newY = _startPos.y + Mathf.Sin(_floatTimer) * floatAmplitude;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);

        // หมุน
        if (rotateSpeed != 0f)
            transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime);
    }

    /// <summary>เรียกจาก PlayerPickup ก่อน Destroy เพื่อเล่น VFX/SFX</summary>
    public void PlayPickupEffects()
    {
        if (pickupVFX != null)
            Instantiate(pickupVFX, transform.position, Quaternion.identity);

        if (pickupSFX != null)
            AudioSource.PlayClipAtPoint(pickupSFX, transform.position, sfxVolume);
    }
}
