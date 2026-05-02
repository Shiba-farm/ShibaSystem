using System;
using UnityEngine;

[Serializable]
public struct Date
{
    public int year, month, day;
    public Date(int y, int m, int d) { year = y; month = m; day = d; }
}

public class CalendarSystem : MonoBehaviour
{
    public static CalendarSystem Instance { get; private set; }

    [Header("Clock / Link")]
    [Tooltip("�ԧ��� TimeOfDaySystem ������黯ԷԹ�Թ���������� (���������)")]
    public TimeOfDaySystem timeOfDay;

    [Header("Date Config")]
    [Tooltip("�ӹǹ�ѹ�����͹Ẻ�����")]
    [Min(1)] public int daysPerMonth = 30;

    [Header("Initial Date")]
    public int startYear = 1;
    public int startMonth = 1;
    public int startDay = 1;

    // Runtime
    [SerializeField, Range(0f, 1f)] private float time01; // 0..1 �ͧ�����ѹ
    [SerializeField] private int year, month, day;

    public Date date => new Date(year, month, day);
    public float Time01 => time01;

    // Events
    public event Action<Date> OnDateChanged;  // �ԧ�ء���駷�� SetDate/SetTime01
    public event Action<Date> OnDayEnded;     // �ԧ������ѹ�� (���§�׹/�����ѹ)

    float lastTod01 = -1f;

    void Awake()
    {
        if (Instance != null && Instance != this) { Debug.LogWarning($"[CalendarSystem] พบ Instance ซ้ำบน '{gameObject.name}' — ลบ Component"); Destroy(this); return; }
        Instance = this;

        year = Mathf.Max(1, startYear);
        month = Mathf.Clamp(startMonth, 1, 12);
        day = Mathf.Clamp(startDay, 1, daysPerMonth);
        time01 = 0f; // ����� 00:00 (��� TimeOfDaySystem �繤���˹����������)
    }

    void Update()
    {
        // ����� TOD ��� sync ��е�Ǩ�Ѻ cross-midnight
        if (timeOfDay != null)
        {
            TickFromTOD(timeOfDay.Time01);
        }
    }

    /// <summary>���¡�ء���������� TOD: ��Ǩ�Ѻ�ѹ������� sync time01</summary>
    public void TickFromTOD(float tod01)
    {
        if (lastTod01 < 0f) lastTod01 = tod01;

        // ��Ǩ�Ѻ wrap: ��ǡ�͹ > ������� => �����ѹ
        bool crossedMidnight = lastTod01 > tod01;
        lastTod01 = tod01;

        SetTime01(tod01, raiseChanged: false);

        if (crossedMidnight)
        {
            NextDay();
            OnDayEnded?.Invoke(date);
            // �ԧ OnDateChanged �ա�ͺ��ѧ���ѹ
            OnDateChanged?.Invoke(date);
        }
        else
        {
            // �����ҧ�ѹ ��Ҥس��ҡ�ѻവ UI ���� ����ԧ੾������¹���� ��
            OnDateChanged?.Invoke(date);
        }
    }

    public void SetDate(int y, int m, int d)
    {
        year = Mathf.Max(1, y);
        month = Mathf.Clamp(m, 1, 12);
        day = Mathf.Clamp(d, 1, daysPerMonth);
        OnDateChanged?.Invoke(date);
    }

    public void SetTime01(float t, bool raiseChanged = true)
    {
        time01 = Mathf.Repeat(t, 1f);
        if (raiseChanged) OnDateChanged?.Invoke(date);
    }
    

    /// <summary>����͹��ѹ�Ѵ� (public ��������к�������¡��)</summary>
    public void NextDay()
    {
        day++;
        if (day > daysPerMonth)
        {
            day = 1;
            month++;
            if (month > 12) { month = 1; year++; }
        }
    }

    public bool IsLastDayOfMonth(int d) => d >= daysPerMonth;

    /// <summary>�����ѹ������ Date ����ҡѺ������� (��͹��Ŵ�� state �ҡ���)</summary>
    public void FastForwardTo(Date target)
    {
        year = target.year;
        month = Mathf.Clamp(target.month, 1, 12);
        day = Mathf.Clamp(target.day, 1, daysPerMonth);
        OnDateChanged?.Invoke(date);
    }
}
