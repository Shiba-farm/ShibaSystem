using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Signals/WorldTimeSignal")]
public class WorldTimeSignal : ScriptableObject
{
    public event Action<WorldTimeData> OnTimeChanged;
    public WorldTimeData CurrentTime { get; private set; }
    public event Action<DayPhase> OnPhaseChanged;
    public DayPhase CurrentPhase { get; private set; }

    public void UpdateTime(int hour, int minute, int day, int month, int year)
    {
        CurrentTime = new WorldTimeData(hour, minute, day, month, year);
        OnTimeChanged?.Invoke(CurrentTime);
    }

    public void UpdatePhase(DayPhase phase)
    {
        CurrentPhase = phase;
        OnPhaseChanged?.Invoke(phase);
    }
}

public struct WorldTimeData
{
    public int Hour;
    public int Minute;
    public int Day;
    public int Month;
    public int Year;
    public float Time01;

    public WorldTimeData(int hour, int minute, int day, int month, int year)
    {
        Hour = hour;
        Minute = minute;
        Day = day;
        Month = month;
        Year = year;
        Time01 = ((hour % 24) + minute / 60f) / 24f;
    }

    // Pre-formatted strings matching your UI exactly
    public string FormattedTime => $"{Hour:D2}:{Minute:D2}";           // 16:16
    public string FormattedDate => $"{Day:D2}/{Month:D2} Y{Year}";     // 27/01 Y4
}

public enum DayPhase { Dawn, Day, Dusk, Night }
