using UnityEngine;

/// <summary>
/// Attach to the root of every GrowthStage.visualPrefab.
/// Gives the farming system a typed handle so it can drive visual state
/// without doing runtime GetComponent lookups or hardcoding material names.
///
/// Implement the hook methods however you like — swap materials, toggle
/// child objects, play animations, trigger particle systems, etc.
/// </summary>
public class CropVisual : MonoBehaviour
{
    [Header("Watered State")]
    [Tooltip("Renderer(s) whose material gets swapped when watered. Leave empty to skip material swap.")]
    [SerializeField] private Renderer[] renderers;
    [SerializeField] private Material   wateredMaterial;
    [SerializeField] private Material   dryMaterial;

    [Header("Harvest FX")]
    [Tooltip("Optional particle system played when the crop is harvested.")]
    [SerializeField] private ParticleSystem harvestParticles;

    // ── Runtime API ───────────────────────────────────────────────────────────

    /// <summary>
    /// Called by the farming system whenever watered state changes for this stage.
    /// </summary>
    public void SetWatered(bool watered)
    {
        if (renderers == null || renderers.Length == 0) return;

        Material mat = watered ? wateredMaterial : dryMaterial;
        if (mat == null) return;

        foreach (var r in renderers)
            if (r != null) r.material = mat;
    }

    /// <summary>
    /// Called by the farming system at the moment the crop is harvested.
    /// Play a pop/sparkle/shake here.
    /// </summary>
    public void PlayHarvestFX()
    {
        if (harvestParticles != null)
            harvestParticles.Play();
    }
}
