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

    public float DawnStart => dawnStart;
    public float DayStart => dayStart;
    public float DuskStart => duskStart;
    public float NightStart => nightStart;

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
            totalGameMinutes.Value = startHour * MinutesPerHour;
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

        minuteAccumulator += Time.deltaTime;
        // Debug.Log($"[WorldTimeManager] Accumulated {minuteAccumulator:F2} seconds towards next game minute.");

        if (minuteAccumulator >= realSecondsPerGameMinute)
        {
            minuteAccumulator -= realSecondsPerGameMinute;
            totalGameMinutes.Value++;
            // Debug.Log($"[WorldTimeManager] Time advanced: {CurrentHour:00}:{CurrentMinute:00} (Day {CurrentDay}, Month {CurrentMonth}, Year {CurrentYear})");
        }
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
        // Snap to next day at startHour
        int currentDayStart = (CurrentDay - 1) * MinutesPerHour * HoursPerDay;
        int nextDayMorning = currentDayStart + (HoursPerDay * MinutesPerHour) + (startHour * MinutesPerHour);
        totalGameMinutes.Value = nextDayMorning;
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
