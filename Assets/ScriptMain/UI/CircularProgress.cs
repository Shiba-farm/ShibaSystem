using UnityEngine;
using UnityEngine.UI;

public class CircularProgress : MonoBehaviour
{
    [Header("References")]
    public Image fillImage;

    [Header("Settings")]
    [Range(0f, 1f)]
    public float fillAmount = 0f;
    [SerializeField] private float smoothSpeed = 5f;

    private float _targetFill;

    private void Update()
    {
        // smooth toward target instead of snapping
        fillImage.fillAmount = Mathf.Lerp(
            fillImage.fillAmount, _targetFill, 
            smoothSpeed * Time.deltaTime);
    }

    public void SetProgress(float current, float max)
    {
        if (max <= 0) return;
        _targetFill = Mathf.Clamp01(current / max);
    }
}
