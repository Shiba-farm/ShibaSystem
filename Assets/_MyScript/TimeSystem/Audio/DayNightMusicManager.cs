using UnityEngine;
using UnityEngine.Audio;
using System.Collections;

/// <summary>
/// DayNightMusicManager
/// --------------------
/// �к��ŧ�����ǧ����: Dawn / Day / Dusk / Night (��Ѻ�ŧẺ crossfade)
/// - �١�Ѻ TimeOfDaySystem (�ѧ event OnPhaseChanged)
/// - �� 2 AudioSource ��Ѻ�ѹ���ͷ� crossfade ��¹ �
/// - �ͧ�Ѻ���������ŧ���ç�ʻѨ�غѹ�͹ Start
///
/// �Ը���:
/// 1) �ҧʤ�Ի�����㹩ҡ (�蹺� GameManager) ���ǵ�駤��� Inspector:
///    - ��ҧ�ԧ TimeOfDaySystem (��������ҧ�о����� FindObjectOfType)
///    - ����Ի�ŧ����Ѻ Dawn/Day/Dusk/Night (�����ҧ�� ���������ǧ���)
///    - ��� AudioMixerGroup (�����) ��С�˹� fadeDuration, baseVolume
/// 2) �� Play �к������͡�ŧ���ç�ʻѨ�غѹ ��Ш�����¹�ŧ�����������¹
///
/// �����˵�: �����ҡ��������ŧ�� "�͹���" �Ѻ "�͹��ҧ�׹" ���ҧ����
///           ������Ի੾�� Day (���� Dawn) ��� Night ��ǹ Dusk �������ҧ��
/// </summary>
public class DayNightMusicManager : MonoBehaviour
{
    [Header("Refs")]
    public TimeOfDaySystem timeSystem; // �ж١���ѵ��ѵԶ���������

    [Header("Clips by Phase")]
    public AudioClip dawnClip; // �ŧ�����ҵ���
    public AudioClip dayClip;  // �ŧ��ҧ�ѹ
    public AudioClip duskClip; // �ŧ������
    public AudioClip nightClip;// �ŧ��ҧ�׹

    [Header("Audio Output")]
    public AudioMixerGroup outputMixer; // ������
    [Range(0f, 1f)] public float baseVolume = 0.8f;

    [Header("Behavior")]
    [Tooltip("������������ŧ���ç�ʻѨ�غѹ�ѹ�յ͹ Start")] public bool playOnStart = true;
    [Tooltip("�������� crossfade ��͡������¹�ŧ")] public float fadeDuration = 1.5f;

    // runtime
    private AudioSource _a;
    private AudioSource _b;
    private AudioSource _active;   // ��Ƿ����ѧ�ѧ����
    private DayPhase _currentPhase;

    void Awake()
    {
        if (!timeSystem) timeSystem = FindObjectOfType<TimeOfDaySystem>();

        // ����� 2 AudioSource ����Ѻ crossfade
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
            // �ӹǳ�� � �͹����� ���������ŧ���١��ͧ
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
        if (newClip == null) return; // ��������ö���ʹ��������ŧ
        CrossfadeTo(newClip, fadeDuration);
    }

    DayPhase GetPhaseNow()
    {
        if (timeSystem == null) return DayPhase.Day;
        // float t = timeSystem.Time01;
        // // ���͹����ǡѺ TimeOfDaySystem / TimeOfDayUI
        // if (t >= timeSystem.nightStart || t < timeSystem.dawnStart) return DayPhase.Night;
        // if (t >= timeSystem.duskStart) return DayPhase.Dusk;
        // if (t >= timeSystem.dayStart) return DayPhase.Day;
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

        // ����繤�Ի�������ͧ������
        if (_active.clip == nextClip) return;

        inactive.clip = nextClip;
        inactive.volume = 0f;
        inactive.Play();

        StopAllCoroutines();
        StartCoroutine(CoCrossfade(_active, inactive, duration));

        _active = inactive; // ��ѧ�����࿴��� inactive ������ active ����
    }

    IEnumerator CoCrossfade(AudioSource from, AudioSource to, float duration)
    {
        float t = 0f;
        float fromStart = from ? from.volume : 0f;
        float toStart = to ? to.volume : 0f; // �»��Ԥ�� 0
        while (t < duration)
        {
            t += Time.unscaledDeltaTime; // �� unscaled ��������з��ҡ Time.timeScale
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
