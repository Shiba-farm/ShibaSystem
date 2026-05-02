using TMPro;
using UnityEngine;

public class CalendarUI : MonoBehaviour
{
    public CalendarSystem calendar;
    public TextMeshProUGUI dateText;

    void Start()
    {
        if (!calendar) calendar = FindFirstObjectByType<CalendarSystem>();
        if (calendar) calendar.OnDateChanged += Refresh;
        Refresh(calendar != null ? calendar.date : new Date(1, 1, 1));
    }

    void OnDestroy()
    {
        if (calendar) calendar.OnDateChanged -= Refresh;
    }

    void Refresh(Date d)
    {
        if (!dateText) return;
        dateText.text = $"{d.day:00}/{d.month:00}  Y{d.year}";
    }
}
