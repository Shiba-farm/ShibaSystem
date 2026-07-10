using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("Settings")]
    [SerializeField] private float holdDuration = 1.5f;

    [Header("References")]
    [SerializeField] private Image progressRing;   // Radial360 filled image
    [SerializeField] private Button button;

    public event Action OnConfirmed;

    private bool _isHolding = false;
    private float _holdTimer = 0f;

    private void Awake()
    {
        if (progressRing != null)
            progressRing.fillAmount = 0f;
    }

    private void Update()
    {
        if (!_isHolding) return;

        _holdTimer += Time.deltaTime;
        float progress = Mathf.Clamp01(_holdTimer / holdDuration);
        progressRing.fillAmount = progress;

        if (_holdTimer >= holdDuration)
        {
            _isHolding = false;
            OnConfirmed?.Invoke();
            ResetProgress();
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!button.interactable) return;
        _isHolding = true;
        _holdTimer = 0f;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!_isHolding) return;
        _isHolding = false;
        ResetProgress();
    }

    private void ResetProgress()
    {
        _holdTimer = 0f;
        progressRing.fillAmount = 0f;
    }
}
