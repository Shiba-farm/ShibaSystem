using UnityEngine;
using System;

public class TimeOfDaySystem : MonoBehaviour
{
    public static TimeOfDaySystem Instance { get; private set; }

    [Header("Signal")]
    [SerializeField] private WorldTimeSignal timeSignal;

    [Header("Sun (Day)")]
    public Light directionalLight;
    public Gradient sunColor;
    public AnimationCurve sunIntensity;

    [Header("Moon (Night)")]
    public Light moonLight;
    public Gradient moonColor;
    public AnimationCurve moonIntensity;

    [Header("Environment")]
    public Material skyboxMaterial;

    [Header("Skybox Swapping")]
    public Material daySkybox;
    public Material nightSkybox;

    public Gradient skyTint;
    public Gradient groundTint;
    public Gradient ambientColor;
    public Gradient fogColor;
    public AnimationCurve fogDensity;

    [Header("Exposure")]
    [Range(0, 8)] public float skyExposureDay = 1.2f;
    [Range(0, 8)] public float skyExposureNight = 0.5f;

    [SerializeField] private float nightStart = 0.80f;
    [SerializeField] private float dawnStart = 0.20f;

    private float time01;

    public event Action<DayPhase> OnPhaseChanged;

    void Start()
    {

        timeSignal.OnTimeChanged -= HandleTimeChanged;
        timeSignal.OnPhaseChanged -= HandlePhaseChanged;

        timeSignal.OnTimeChanged += HandleTimeChanged;
        timeSignal.OnPhaseChanged += HandlePhaseChanged;

        HandleTimeChanged(timeSignal.CurrentTime);
    }

    void OnDestroy()
    {
        timeSignal.OnTimeChanged -= HandleTimeChanged;
        timeSignal.OnPhaseChanged -= HandlePhaseChanged;
    }

    private void HandleTimeChanged(WorldTimeData data)
    {
        time01 = data.Time01; 
        UpdateLighting();     
    }

    private void HandlePhaseChanged(DayPhase phase)
    {
        OnPhaseChanged?.Invoke(phase); 
    }

    void UpdateLighting()
    {
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

        if (moonLight != null)
        {
            moonLight.transform.rotation = Quaternion.Euler(sunAngle - 180f, 170f, 0f);
            if (moonColor != null) moonLight.color = moonColor.Evaluate(time01);
            if (moonIntensity != null) moonLight.intensity = moonIntensity.Evaluate(time01);
        }

        bool isNight = time01 >= nightStart || time01 < dawnStart;

        if (daySkybox != null && nightSkybox != null)
        {
            Material targetSky = isNight ? nightSkybox : daySkybox;
            if (RenderSettings.skybox != targetSky)
            {
                RenderSettings.skybox = targetSky;
                DynamicGI.UpdateEnvironment();
            }
        }

        if (skyboxMaterial != null && !isNight)
        {
            if (skyTint != null) skyboxMaterial.SetColor("_SkyTint", skyTint.Evaluate(time01));
            if (groundTint != null) skyboxMaterial.SetColor("_GroundColor", groundTint.Evaluate(time01));

            float exposureTarget = isNight ? skyExposureNight : skyExposureDay;
        }
        else if (skyboxMaterial != null && isNight)
        {
        }

        if (ambientColor != null) RenderSettings.ambientLight = ambientColor.Evaluate(time01);
        if (fogColor != null) RenderSettings.fogColor = fogColor.Evaluate(time01);
        if (fogDensity != null && fogDensity.length > 0) RenderSettings.fogDensity = fogDensity.Evaluate(time01);
    }
}