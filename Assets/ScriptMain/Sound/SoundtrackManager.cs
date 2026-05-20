using UnityEngine;

public class SoundtrackManager : MonoBehaviour
{
    public static SoundtrackManager Instance { get; private set; }

    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Tracks")]
    [SerializeField] private AudioClip morningTrack;
    [SerializeField] private AudioClip afternoonTrack;
    [SerializeField] private AudioClip nightTrack;

    [Header("Signal")]
    [SerializeField] private WorldTimeSignal timeSignal;

    private void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable() => timeSignal.OnTimeChanged += OnHourChanged;
    private void OnDisable() => timeSignal.OnTimeChanged -= OnHourChanged;

    private void OnHourChanged(WorldTimeData data)
    {
        // AudioClip track = hour switch
        // {
        //     >= 6 and < 12  => morningTrack,
        //     >= 12 and < 18 => afternoonTrack,
        //     _              => nightTrack
        // };

        // SwitchTrack(track);
    }

    private void SwitchTrack(AudioClip track)
    {
        if (musicSource.clip == track) return;
        musicSource.clip = track;
        musicSource.Play();
    }

    void Start()
    {
        SwitchTrack(morningTrack);
    }

    // called by world objects, VFX etc.
    public void PlaySFX(AudioClip clip)
    {
        sfxSource.PlayOneShot(clip);
    }
}
