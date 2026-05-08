using UnityEngine;

public class AutomaticLamp : MonoBehaviour
{
    [Header("Settings")]
    public Light lampLight; // �ҡ Point Light �����ç���

    [Header("Visuals (Optional)")]
    public MeshRenderer lanternRenderer; // ���������� (�����ҡ�������¹����ʴ�)
    public int materialIndex = 0;        // ��ʴت�ͧ�˹��������¹ (���Ԫ�ͧ 1 �����ǹ��ʹ�)
    public Material lightOnMat;          // ��ʴص͹俵Դ (Emission)
    public Material lightOffMat;         // ��ʴص͹俴Ѻ

    void Start()
    {
        // ��Ѥ��Ѻ���������Ҩҡ TimeOfDaySystem
        var timeSystem = TimeOfDaySystem.Instance;
        if (timeSystem != null)
        {
            timeSystem.OnPhaseChanged += UpdateLampState;

            // �����һѨ�غѹ�ѹ�յ͹�������
            CheckInitialState(timeSystem);
        }
    }

    void OnDestroy()
    {
        if (TimeOfDaySystem.Instance != null)
        {
            TimeOfDaySystem.Instance.OnPhaseChanged -= UpdateLampState;
        }
    }

    // �ѧ��ѹ�礵͹������� (�ӹǳ�ͧ���� GetPhase �� private)
    void CheckInitialState(TimeOfDaySystem timeSystem)
    {
        // float t = timeSystem.Time01;
        // DayPhase phase = DayPhase.Day; // ����������

        // if (t >= timeSystem.nightStart || t < timeSystem.dawnStart) phase = DayPhase.Night;
        // else if (t >= timeSystem.duskStart) phase = DayPhase.Dusk;
        // else if (t >= timeSystem.dayStart) phase = DayPhase.Day;
        // else phase = DayPhase.Dawn;

        // UpdateLampState(phase);
    }

    void UpdateLampState(DayPhase phase)
    {
        // ���͹�: �Դ�੾�е͹ "���" ���� "��ҧ�׹"
        bool isNightTime = (phase == DayPhase.Dusk || phase == DayPhase.Night);

        // 1. ����Դ/�Դ �ʧ
        if (lampLight != null)
        {
            lampLight.enabled = isNightTime;
        }

        // 2. (��) ����¹��ʴ��������ͧ�ʧ
        if (lanternRenderer != null && lightOnMat != null && lightOffMat != null)
        {
            Material[] mats = lanternRenderer.materials;
            if (materialIndex < mats.Length)
            {
                mats[materialIndex] = isNightTime ? lightOnMat : lightOffMat;
                lanternRenderer.materials = mats;
            }
        }
    }
}