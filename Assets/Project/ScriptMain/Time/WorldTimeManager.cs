using Unity.Netcode;
using UnityEngine;

public class WorldTimeManager : NetworkSaveableBehaviour
{
    public static WorldTimeManager Instance { get; private set; }
    [Header("Time Settings")]
    [SerializeField] private WorldTimeSignal timeSignal;
    [SerializeField] private int startHour = 6;
    [SerializeField] private float realSecondsPerGameMinute = 5f;

    [Header("Time of the day phase Thresholds")]
    [SerializeField] private float dawnStart = 0.20f;
    [SerializeField] private float dayStart = 0.30f;
    [SerializeField] private float duskStart = 0.70f;
    [SerializeField] private float nightStart = 0.80f;

    // ── Debug Time Scrubber (Host / Server only) ────────────────────────
    // Tick "Use Debug Time" to freeze the game clock and drag the hour/minute
    // sliders freely.  Every system that reads WorldTimeSignal (lighting,
    // crops, NPCs, UI) will respond immediately — exactly like real time flow.
    // Untick to resume the normal clock from wherever you left the slider.
    [Header("Debug Time Scrubber (Play Mode — Host Only)")]
    [Tooltip("Freeze the in-game clock and use the sliders below instead.")]
    public bool useDebugTime = false;

    [Range(0, 23)]
    [Tooltip("Hour to jump to (0–23).")]
    public int debugHour = 8;

    [Range(0, 59)]
    [Tooltip("Minute to jump to (0–59).")]
    public int debugMinute = 0;

    private NetworkVariable<int> totalGameMinutes = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private const int MinutesPerHour = 60;
    private const int HoursPerDay = 24;
    private const int DaysPerMonth = 30;
    private const int MonthsPerYear = 12;

    private float minuteAccumulator = 0f;
    private DayPhase currentPhase;
    public int CurrentMinute => totalGameMinutes.Value % MinutesPerHour;
    public int CurrentHour => (totalGameMinutes.Value / MinutesPerHour) % HoursPerDay;
    public int CurrentDay => (totalGameMinutes.Value / (MinutesPerHour * HoursPerDay) % DaysPerMonth) + 1;
    public int CurrentMonth => (totalGameMinutes.Value / (MinutesPerHour * HoursPerDay * DaysPerMonth) % MonthsPerYear) + 1;
    public int CurrentYear => (totalGameMinutes.Value / (MinutesPerHour * HoursPerDay * DaysPerMonth * MonthsPerYear)) + 1;

    /// <summary>
    /// เลขวันสะสมทั้งหมด (ไม่วนซ้ำรายเดือนเหมือน CurrentDay) — ใช้เทียบว่า "วันเดียวกันไหม"
    /// ข้ามเดือน/ปีได้ถูกต้อง เช่น ระบบจำกัดจำนวนของขวัญต่อวันของ RelationshipManager
    /// </summary>
    public int AbsoluteDayIndex =>
        (CurrentYear - 1) * MonthsPerYear * DaysPerMonth + (CurrentMonth - 1) * DaysPerMonth + CurrentDay;

    public float DawnStart => dawnStart;
    public float DayStart => dayStart;
    public float DuskStart => duskStart;
    public float NightStart => nightStart;
    //Raw minute counter — read by GameDataManager to bridge scene transitions.</summary>
    public int TotalGameMinutes => totalGameMinutes.Value;

    public override bool IsPlayerSaveable => false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Debug.Log($"[WorldTimeManager] Spawned — IsServer:{IsServer} IsHost:{IsHost} IsClient:{IsClient} IsOwner:{IsOwner}");
        // Set start time
        if (IsServer)
        {
            SaveLoadManager.Instance?.Register(this);
            // If GameDataManager captured time before a scene transition, restore it.
            // Otherwise this is a fresh session — use the configured start hour.
            int pending = GameDataManager.Instance?.PendingTransitionMinutes ?? -1;
            if (pending >= 0)
            {
                totalGameMinutes.Value = pending;
                GameDataManager.Instance.ClearTransitionTime();
                Debug.Log($"[WorldTimeManager] Restored from scene transition: {pending} minutes");
            }
            else
            {
                totalGameMinutes.Value = startHour * MinutesPerHour;
            }
        }

        totalGameMinutes.OnValueChanged += OnTimeChanged;

        // Fire immediately so UI gets initial value
        currentPhase = GetPhase(GetTime01());
        BroadcastTime();
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        totalGameMinutes.OnValueChanged -= OnTimeChanged;
        if (IsServer)
            SaveLoadManager.Instance?.Unregister(this);
    }

    void Update()
    {
        if (!IsServer) return;

        if (useDebugTime)
        {
            // Force the clock to the chosen hour:minute on the current calendar day.
            // All subscribers (lighting, crops, NPCs, UI) respond via WorldTimeSignal.
            int target = CurrentDayBaseMinutes() + debugHour * MinutesPerHour + debugMinute;
            if (totalGameMinutes.Value != target)
                totalGameMinutes.Value = target;
            return; // clock is frozen — don't accumulate real seconds
        }

        minuteAccumulator += Time.deltaTime;

        if (minuteAccumulator >= realSecondsPerGameMinute)
        {
            minuteAccumulator -= realSecondsPerGameMinute;
            totalGameMinutes.Value++;
        }
    }

    /// <summary>Returns the total game-minutes at the start of the current calendar day.</summary>
    private int CurrentDayBaseMinutes()
    {
        return (CurrentYear - 1) * MonthsPerYear * DaysPerMonth * HoursPerDay * MinutesPerHour
             + (CurrentMonth - 1) * DaysPerMonth * HoursPerDay * MinutesPerHour
             + (CurrentDay - 1) * HoursPerDay * MinutesPerHour;
    }

    private void OnTimeChanged(int previousValue, int newValue)
    {
        // Debug.Log($"[WorldTimeManager] Time changed: {CurrentHour:00}:{CurrentMinute:00} (Day {CurrentDay}, Month {CurrentMonth}, Year {CurrentYear})");
        BroadcastTime();
    }

    private void BroadcastTime()
    {
        timeSignal.UpdateTime(
            CurrentHour, CurrentMinute,
            CurrentDay, CurrentMonth, CurrentYear
        );
    }

    private void CheckPhaseChange()
    {
        DayPhase newPhase = GetPhase(GetTime01());
        if (newPhase == currentPhase) return;

        currentPhase = newPhase;
        timeSignal.UpdatePhase(currentPhase); // broadcast phase change
    }

    private float GetTime01()
    {
        return ((CurrentHour % 24) + CurrentMinute / 60f) / 24f;
    }

    private DayPhase GetPhase(float t)
    {
        if (t >= nightStart || t < dawnStart) return DayPhase.Night;
        if (t >= duskStart) return DayPhase.Dusk;
        if (t >= dayStart) return DayPhase.Day;
        return DayPhase.Dawn;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void SkipToMorningServerRpc()
    {
        // Derive the absolute start of the current day by rounding totalGameMinutes
        // down to the nearest full day. Using (CurrentDay-1)*minutesPerDay was wrong
        // because CurrentDay is the day-of-month (1-30), not the global day index —
        // from Month 2 onward the month offset was missing and the clock jumped back.
        int minutesPerDay = MinutesPerHour * HoursPerDay;
        int absoluteDayStart = (totalGameMinutes.Value / minutesPerDay) * minutesPerDay;
        totalGameMinutes.Value = absoluteDayStart + minutesPerDay + (startHour * MinutesPerHour);
    }
    public override void CaptureState(GameSaveData save, ulong clientId = 0)
    {
        save.world.currentYear = CurrentYear;
        save.world.currentMonth = CurrentMonth;
        save.world.currentDay = CurrentDay;
    }

    public override void RestoreState(GameSaveData save, ulong clientId = 0)
    {
        if (!IsServer) return;

        int restoredMinutes =
            (save.world.currentYear - 1) * MonthsPerYear * DaysPerMonth * HoursPerDay * MinutesPerHour +
            (save.world.currentMonth - 1) * DaysPerMonth * HoursPerDay * MinutesPerHour +
            (save.world.currentDay - 1) * HoursPerDay * MinutesPerHour +
            startHour * MinutesPerHour;

        totalGameMinutes.Value = restoredMinutes;
    }
}
