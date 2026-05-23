using Unity.Cinemachine;
using UnityEngine;

public class CameraOrbitController : MonoBehaviour
{
    [Header("Orbit Settings")]
    [SerializeField] private float orbitAngleStep = 45f;    // degrees per press
    [SerializeField] private float orbitSpeed     = 5f;     // smoothing speed

    [Header("References")]
    [SerializeField] private CinemachineCamera virtualCamera;

    private CinemachineOrbitalFollow _orbitalFollow;
    private float _targetAngle = 0f;

    private void Awake()
    {
        _orbitalFollow = virtualCamera.GetComponent<CinemachineOrbitalFollow>();

        if (_orbitalFollow == null)
            Debug.LogError("[CameraOrbit] CinemachineOrbitalFollow not found on VCam.");
    }

    private void OnEnable()
    {
        InputHandler.Singleton.OnOrbitLeftTriggered  += OrbitLeft;
        InputHandler.Singleton.OnOrbitRightTriggered += OrbitRight;
    }

    private void OnDisable()
    {
        if (InputHandler.Singleton == null) return;
        InputHandler.Singleton.OnOrbitLeftTriggered  -= OrbitLeft;
        InputHandler.Singleton.OnOrbitRightTriggered -= OrbitRight;
    }

    private void Update()
    {
        if (_orbitalFollow == null) return;

        // Smoothly rotate to target angle
        _orbitalFollow.HorizontalAxis.Value = Mathf.LerpAngle(
            _orbitalFollow.HorizontalAxis.Value,
            _targetAngle,
            orbitSpeed * Time.deltaTime
        );
    }

    private void OrbitLeft()
    {
        _targetAngle -= orbitAngleStep;
        Debug.Log($"Orbit Left: Target Angle = {_targetAngle}");
        NormalizeAngle();
    }

    private void OrbitRight()
    {
        _targetAngle += orbitAngleStep;
        Debug.Log($"Orbit Right: Target Angle = {_targetAngle}");
        NormalizeAngle();
    }

    private void NormalizeAngle()
    {
        // Keep angle within -180 to 180
        while (_targetAngle > 180f)  _targetAngle -= 360f;
        while (_targetAngle < -180f) _targetAngle += 360f;
    }
}
