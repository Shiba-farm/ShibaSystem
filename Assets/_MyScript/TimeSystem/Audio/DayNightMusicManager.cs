using UnityEngine;
using UnityEngine.Audio;
using System.Collections;

/// <summary>
/// DayNightMusicManager
/// --------------------
/// ระบบเพลงตามช่วงเวลา: Dawn / Day / Dusk / Night (สลับเพลงแบบ crossfade)
/// - ผูกกับ TimeOfDaySystem (ฟัง event OnPhaseChanged)
/// - มี 2 AudioSource สลับกันเพื่อทำ crossfade เนียน ๆ
/// - รองรับเริ่มเล่นเพลงให้ตรงเฟสปัจจุบันตอน Start
///
/// วิธีใช้:
/// 1) วางสคริปต์ไว้ในฉาก (เช่นบน GameManager) แล้วตั้งค่าใน Inspector:
///    - อ้างอิง TimeOfDaySystem (ถ้าเว้นว่างจะพยายาม FindObjectOfType)
///    - ใส่คลิปเพลงสำหรับ Dawn/Day/Dusk/Night (เว้นว่างได้ ถ้าไม่ใช้ช่วงนั้น)
///    - ใส่ AudioMixerGroup (ถ้ามี) และกำหนด fadeDuration, baseVolume
/// 2) กด Play ระบบจะเลือกเพลงให้ตรงเฟสปัจจุบัน และจะเปลี่ยนเพลงเมื่อเฟสเปลี่ยน
///
/// หมายเหตุ: ถ้าอยากให้เกมมีเพลงแค่ "ตอนเช้า" กับ "ตอนกลางคืน" อย่างเดียว
///           ก็ใส่คลิปเฉพาะ Day (หรือ Dawn) และ Night ส่วน Dusk ปล่อยว่างได้
/// </summary>
public class DayNightMusicManager : MonoBehaviour
{
    [Header("Refs")]
    public TimeOfDaySystem timeSystem; // จะถูกหาอัตโนมัติถ้าไม่ได้เซ็ต

    [Header("Clips by Phase")]
    public AudioClip dawnClip; // เพลงยามเช้าตรู่
    public AudioClip dayClip;  // เพลงกลางวัน
    public AudioClip duskClip; // เพลงยามเย็น
    public AudioClip nightClip;// เพลงกลางคืน

    [Header("Audio Output")]
    public AudioMixerGroup outputMixer; // ไม่จำเป็น
    [Range(0f, 1f)] public float baseVolume = 0.8f;

    [Header("Behavior")]
    [Tooltip("ให้เริ่มเล่นเพลงให้ตรงเฟสปัจจุบันทันทีตอน Start")] public bool playOnStart = true;
    [Tooltip("ระยะเวลา crossfade ต่อการเปลี่ยนเพลง")] public float fadeDuration = 1.5f;

    // runtime
    private AudioSource _a;
    private AudioSource _b;
    private AudioSource _active;   // ตัวที่กำลังดังอยู่
    private DayPhase _currentPhase;

    void Awake()
    {
        if (!timeSystem) timeSystem = FindObjectOfType<TimeOfDaySystem>();

        // เตรียม 2 AudioSource สำหรับ crossfade
        _a = CreateChildSource("Music_A");
        _b = CreateChildSource("Music_B");
        _active = _a;
    }

    void OnEnable()
    {
        if (timeSystem != null)
            timeSystem.OnPhaseChanged += OnPhaseChanged;
    }

    void OnDisable()
    {
        if (timeSystem != null)
            timeSystem.OnPhaseChanged -= OnPhaseChanged;
    }

    void Start()
    {
        if (playOnStart)
        {
            // คำนวณเฟส ณ ตอนเริ่ม และเริ่มเพลงให้ถูกต้อง
            _currentPhase = GetPhaseNow();
            var clip = GetClipForPhase(_currentPhase);
            if (clip)
            {
                _active.clip = clip;
                _active.volume = baseVolume;
                _active.Play();
            }
        }
    }

    void OnPhaseChanged(DayPhase phase)
    {
        if (phase == _currentPhase) return;
        _currentPhase = phase;
        var newClip = GetClipForPhase(phase);
        if (newClip == null) return; // ไม่ตั้งอะไรถ้าเฟสนี้ไม่ใช้เพลง
        CrossfadeTo(newClip, fadeDuration);
    }

    DayPhase GetPhaseNow()
    {
        if (timeSystem == null) return DayPhase.Day;
        float t = timeSystem.Time01;
        // เงื่อนไขเดียวกับ TimeOfDaySystem / TimeOfDayUI
        if (t >= timeSystem.nightStart || t < timeSystem.dawnStart) return DayPhase.Night;
        if (t >= timeSystem.duskStart) return DayPhase.Dusk;
        if (t >= timeSystem.dayStart) return DayPhase.Day;
        return DayPhase.Dawn;
    }

    AudioClip GetClipForPhase(DayPhase p)
    {
        switch (p)
        {
            case DayPhase.Dawn: return dawnClip ? dawnClip : (dayClip ? dayClip : null);
            case DayPhase.Day: return dayClip ? dayClip : dawnClip;
            case DayPhase.Dusk: return duskClip ? duskClip : dayClip;
            case DayPhase.Night: return nightClip;
        }
        return null;
    }

    AudioSource CreateChildSource(string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop = true;
        src.outputAudioMixerGroup = outputMixer;
        src.volume = 0f;
        return src;
    }

    public void CrossfadeTo(AudioClip nextClip, float duration)
    {
        if (nextClip == null) return;
        var inactive = (_active == _a) ? _b : _a;

        // ถ้าเป็นคลิปเดิมไม่ต้องทำอะไร
        if (_active.clip == nextClip) return;

        inactive.clip = nextClip;
        inactive.volume = 0f;
        inactive.Play();

        StopAllCoroutines();
        StartCoroutine(CoCrossfade(_active, inactive, duration));

        _active = inactive; // หลังเริ่มเฟดให้ inactive กลายเป็น active ใหม่
    }

    IEnumerator CoCrossfade(AudioSource from, AudioSource to, float duration)
    {
        float t = 0f;
        float fromStart = from ? from.volume : 0f;
        float toStart = to ? to.volume : 0f; // โดยปกติคือ 0
        while (t < duration)
        {
            t += Time.unscaledDeltaTime; // ใช้ unscaled เพื่อไม่กระทบจาก Time.timeScale
            float k = duration > 0f ? Mathf.Clamp01(t / duration) : 1f;
            if (from) from.volume = Mathf.Lerp(fromStart, 0f, k);
            if (to) to.volume = Mathf.Lerp(toStart, baseVolume, k);
            yield return null;
        }
        if (from)
        {
            from.volume = 0f;
            from.Stop();
        }
        if (to) to.volume = baseVolume;
    }

    // ----- Utilities / Debug -----
#if UNITY_EDITOR
    [ContextMenu("Debug/Play Dawn")]
    void DebugPlayDawn() { OnPhaseChanged(DayPhase.Dawn); }
    [ContextMenu("Debug/Play Day")]
    void DebugPlayDay() { OnPhaseChanged(DayPhase.Day); }
    [ContextMenu("Debug/Play Dusk")]
    void DebugPlayDusk() { OnPhaseChanged(DayPhase.Dusk); }
    [ContextMenu("Debug/Play Night")]
    void DebugPlayNight() { OnPhaseChanged(DayPhase.Night); }
#endif
}
