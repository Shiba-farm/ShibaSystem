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

    [Header("Transition")]
    [Tooltip("How long (in 0–1 time fraction) the dusk/dawn fade takes. " +
             "0.05 ≈ 1.2 in-game hours. Increase for a longer, more gradual fade.")]
    [Range(0.01f, 0.15f)]
    [SerializeField] private float transitionDuration = 0.05f;

    private float time01;

    public event Action<DayPhase> OnPhaseChanged;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

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

    /// <summary>
    /// Force an immediate lighting refresh using the current time value.
    /// Call this after manually overriding ambient to re-sync everything.
    /// </summary>
    public void ForceUpdateLighting() => UpdateLighting();

    void UpdateLighting()
    {
        // skyBlend: 0 = full day, 1 = full night.
        // Smoothly transitions across the dusk and dawn windows instead of
        // flipping at a hard boundary, giving a gradual sunset/sunrise feel.
        float skyBlend = CalculateSkyBlend(time01);

        float sunAngle = (time01 * 360f) - 90f;

        // ── Sun ─────────────────────────────────────────────────────────
        if (directionalLight != null)
        {
            directionalLight.transform.rotation = Quaternion.Euler(sunAngle, 170f, 0f);
            if (sunColor != null) directionalLight.color = sunColor.Evaluate(time01);

            // Multiply curve value by (1−blend) so the sun always fades
            // smoothly to 0 across the dusk window, even if the
            // AnimationCurve has a sharp edge at nightStart.
            float intensity = (sunIntensity != null ? sunIntensity.Evaluate(time01) : 1f)
                              * (1f - skyBlend);
            directionalLight.intensity = intensity;

            if (intensity <= 0.01f && directionalLight.shadows != LightShadows.None)
                directionalLight.shadows = LightShadows.None;
            else if (intensity > 0.01f && directionalLight.shadows == LightShadows.None)
                directionalLight.shadows = LightShadows.Soft;
        }

        // ── Moon ────────────────────────────────────────────────────────
        if (moonLight != null)
        {
            moonLight.transform.rotation = Quaternion.Euler(sunAngle - 180f, 170f, 0f);
            if (moonColor != null) moonLight.color = moonColor.Evaluate(time01);

            // Multiply by blend so the moon fades IN across dusk/dawn
            // mirror to the sun fading OUT.
            moonLight.intensity = (moonIntensity != null ? moonIntensity.Evaluate(time01) : 1f)
                                  * skyBlend;
        }

        // ── Skybox swap ─────────────────────────────────────────────────
        // Swap at the MIDPOINT of the blend window (skyBlend = 0.5) so the
        // material swap happens when the sun is already half-faded — much
        // less jarring than swapping exactly at nightStart / dawnStart.
        if (daySkybox != null && nightSkybox != null)
        {
            Material targetSky = skyBlend >= 0.5f ? nightSkybox : daySkybox;
            if (RenderSettings.skybox != targetSky)
            {
                RenderSettings.skybox = targetSky;
                DynamicGI.UpdateEnvironment();
            }
        }

        // ── Skybox material tints (procedural/gradient sky only) ────────
        if (skyboxMaterial != null)
        {
            if (skyTint    != null) skyboxMaterial.SetColor("_SkyTint",     skyTint.Evaluate(time01));
            if (groundTint != null) skyboxMaterial.SetColor("_GroundColor", groundTint.Evaluate(time01));
            // Blend exposure between day and night values.
            skyboxMaterial.SetFloat("_Exposure", Mathf.Lerp(skyExposureDay, skyExposureNight, skyBlend));
        }

        if (ambientColor != null) RenderSettings.ambientLight = ambientColor.Evaluate(time01);
        if (fogColor     != null) RenderSettings.fogColor     = fogColor.Evaluate(time01);
        if (fogDensity   != null && fogDensity.length > 0)
            RenderSettings.fogDensity = fogDensity.Evaluate(time01);
    }

    /// <summary>
    /// Returns 0 (full day) → 1 (full night) with smooth-step easing across
    /// two transition windows centred on <see cref="dawnStart"/> and
    /// <see cref="nightStart"/>.  Width of each window = transitionDuration * 2.
    /// </summary>
    private float CalculateSkyBlend(float t)
    {
        float d = transitionDuration;

        // ── Dusk window: [nightStart-d … nightStart+d] ──────────────────
        float duskA = nightStart - d;
        float duskB = nightStart + d;
        if (t >= duskA && t < duskB)
            return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(duskA, duskB, t));

        // ── Dawn window: [dawnStart-d … dawnStart+d] ───────────────────
        float dawnA = dawnStart - d;
        float dawnB = dawnStart + d;
        if (t >= dawnA && t < dawnB)
            return Mathf.SmoothStep(1f, 0f, Mathf.InverseLerp(dawnA, dawnB, t));

        // ── Full night or full day ───────────────────────────────────────
        bool isNight = t >= nightStart || t < dawnStart;
        return isNight ? 1f : 0f;
    }
}