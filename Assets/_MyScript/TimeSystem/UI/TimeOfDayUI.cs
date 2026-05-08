using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TimeOfDayUI : MonoBehaviour
{
    [Header("Signal")]
    [SerializeField] private WorldTimeSignal timeSignal;
    [Header("Refs")]
    public TimeOfDaySystem timeSystem;
    public TextMeshProUGUI timeLabel;
    public TextMeshProUGUI dayLabel;

    [Header("Clock Animation")]
    [Tooltip("The Animator component for the clock UI")]
    public Animator clockAnimator;

    [Tooltip("The animation state name for the day/night cycle")]
    public string animationStateName = "DayNightCycle";

    void Start()
    {
        if (timeSystem == null) timeSystem = TimeOfDaySystem.Instance;
    }

    void Update()
    {
        if (timeSystem == null) return;

        // if (timeLabel != null)
        //     timeLabel.text = $"{timeSystem.Hour:00}:{timeSystem.Minute:00}";

        if (clockAnimator != null)
        {
            clockAnimator.Play(animationStateName, 0, timeSignal.CurrentTime.Time01);
            clockAnimator.speed = 0f;
        }
    }
}