using UnityEngine;
using System;

public enum DayPhase { Dawn, Day, Dusk, Night }

public class TimeOfDaySystem : MonoBehaviour
{
    public static TimeOfDaySystem Instance { get; private set; }

    [Header("Clock")]
    public float dayLengthInMinutes = 10f;
    [Range(0, 23)] public int startHour = 6;
    [Range(0, 59)] public int startMinute = 0;

    [Header("Sun (Day)")]
    public Light directionalLight;
    public Gradient sunColor;
    public AnimationCurve sunIntensity;

    [Header("Moon (Night)")]
    public Light moonLight;
    public Gradient moonColor;
    public AnimationCurve moonIntensity;

    [Header("Environment")]
    public Material skyboxMaterial; // �ѹ����͵�Ƿ��ж١������ (���Ԥ�ͷ�ͧ��ҵ͹���)

    // [��������] ����Ѻ��Ѻ Skybox
    [Header("Skybox Swapping")]
    public Material daySkybox;     // �ҡ Material ��ͧ��� "�͹���" �������
    public Material nightSkybox;   // �ҡ Material ��ͧ��� "�͹�׹" (����մ��) �������

    public Gradient skyTint;
    public Gradient groundTint;
    public Gradient ambientColor;
    public Gradient fogColor;
    public AnimationCurve fogDensity;

    [Header("Exposure")]
    [Range(0, 8)] public float skyExposureDay = 1.2f;
    [Range(0, 8)] public float skyExposureNight = 0.5f;

    [Header("Phase Thresholds")]
    [Range(0f, 1f)] public float dawnStart = 0.20f;
    [Range(0f, 1f)] public float dayStart = 0.30f;
    [Range(0f, 1f)] public float duskStart = 0.70f;
    [Range(0f, 1f)] public float nightStart = 0.80f;

    public event Action<DayPhase> OnPhaseChanged;

    [SerializeField, Range(0f, 1f)] private float time01;
    private DayPhase currentPhase;
    public float Time01 => time01;

    /// <summary>หยุดเวลาชั่วคราว (DayEndSystem ใช้ตอนแสดง summary)</summary>
    public bool IsPaused { get; set; } = false;

    public int Hour => Mathf.FloorToInt(time01 * 24f);
    public int Minute => Mathf.FloorToInt(((time01 * 24f) % 1) * 60f);

    /// <summary>
    /// จำนวน "ชั่วโมงในเกม" ที่ผ่านไปใน frame นี้
    /// ใช้สำหรับระบบที่ต้องโตตาม game-time เช่น พืช
    /// ตัวอย่าง: ถ้า dayLengthInMinutes = 8 → 1 วินาทีจริง = 24/(8*60) = 0.05 ชม.ในเกม
    /// </summary>
    public float GameHoursDelta
    {
        get
        {
            float secondsPerDay = Mathf.Max(1f, dayLengthInMinutes * 60f);
            float dayFraction = Time.deltaTime / secondsPerDay; // สัดส่วนของวันที่ผ่านไป
            return dayFraction * 24f; // แปลงเป็นชั่วโมงในเกม
        }
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Debug.LogWarning($"[TimeOfDaySystem] พบ Instance ซ้ำบน '{gameObject.name}' — ลบ Component"); Destroy(this); return; }
        Instance = this;
        time01 = ((startHour % 24) + startMinute / 60f) / 24f;
        currentPhase = GetPhase(time01);
    }

    void Update()
    {
        AdvanceTime();
        UpdateLighting();
        CheckPhaseChange();
    }

    void AdvanceTime()
    {
        if (IsPaused) return;
        float secondsPerDay = Mathf.Max(1f, dayLengthInMinutes * 60f);
        time01 += Time.deltaTime / secondsPerDay;
        if (time01 >= 1f) time01 -= 1f;
    }

    void UpdateLighting()
    {
        // 1. ��ع�ǧ�ҷԵ��
        float sunAngle = (time01 * 360f) - 90f;
        if (directionalLight != null)
        {
            directionalLight.transform.rotation = Quaternion.Euler(sunAngle, 170f, 0f);
            if (sunColor != null) directionalLight.color = sunColor.Evaluate(time01);
            if (sunIntensity != null) directionalLight.intensity = sunIntensity.Evaluate(time01);

            if (directionalLight.intensity <= 0.01f && directionalLight.shadows != LightShadows.None)
                directionalLight.shadows = LightShadows.None;
            else if (directionalLight.intensity > 0.01f && directionalLight.shadows == LightShadows.None)
                directionalLight.shadows = LightShadows.Soft;
        }

        // 2. ��ع�ǧ�ѹ���
        if (moonLight != null)
        {
            moonLight.transform.rotation = Quaternion.Euler(sunAngle - 180f, 170f, 0f);
            if (moonColor != null) moonLight.color = moonColor.Evaluate(time01);
            if (moonIntensity != null) moonLight.intensity = moonIntensity.Evaluate(time01);
        }

        // 3. [������] ��Ѻ Skybox ��л�Ѻ��
        bool isNight = (time01 >= nightStart || time01 < dawnStart);

        // ����շ�� 2 Ẻ �����Ѻ�������
        if (daySkybox != null && nightSkybox != null)
        {
            Material targetSky = isNight ? nightSkybox : daySkybox;
            if (RenderSettings.skybox != targetSky)
            {
                RenderSettings.skybox = targetSky;
                DynamicGI.UpdateEnvironment(); // ��� Unity �ѻവ�ʧ�з�͹�š����
            }
        }

        // 4. ��û�Ѻ�� Skybox (Tint) 
        // ��Ҩл�Ѻ��੾�е͹�� "��ҧ�ѹ" ���Ͷ���� Skybox ����
        // ���� Skybox ��ҧ�׹�ѡ�����ٻ�������������� ����ͧ������շѺ
        if (skyboxMaterial != null && !isNight)
        {
            if (skyTint != null) skyboxMaterial.SetColor("_SkyTint", skyTint.Evaluate(time01));
            if (groundTint != null) skyboxMaterial.SetColor("_GroundColor", groundTint.Evaluate(time01));

            // Exposure ��Ѻ�����ѹ��駤׹ ���ͤ������ҧ����������
            float exposureTarget = isNight ? skyExposureNight : skyExposureDay;
            // skyboxMaterial.SetFloat("_Exposure", exposureTarget); // ��÷Ѵ������餹�� Material �Ҩ������ͧ��Ѻ
        }
        else if (skyboxMaterial != null && isNight)
        {
            // �����ҡ����������ҧ�͹��ҧ�׹���� �����ç���
            // skyboxMaterial.SetFloat("_Exposure", skyExposureNight);
        }

        if (ambientColor != null) RenderSettings.ambientLight = ambientColor.Evaluate(time01);
        if (fogColor != null) RenderSettings.fogColor = fogColor.Evaluate(time01);
        if (fogDensity != null && fogDensity.length > 0) RenderSettings.fogDensity = fogDensity.Evaluate(time01);
    }

    void CheckPhaseChange()
    {
        var p = GetPhase(time01);
        if (p != currentPhase)
        {
            currentPhase = p;
            OnPhaseChanged?.Invoke(currentPhase);
        }
    }

    DayPhase GetPhase(float t)
    {
        if (t >= nightStart || t < dawnStart) return DayPhase.Night;
        if (t >= duskStart) return DayPhase.Dusk;
        if (t >= dayStart) return DayPhase.Day;
        return DayPhase.Dawn;
    }

    public void SetTime(int hour, int minute)
    {
        time01 = ((hour % 24) + minute / 60f) / 24f;
        UpdateLighting();
        CheckPhaseChange();
    }
}