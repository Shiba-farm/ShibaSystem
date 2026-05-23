using UnityEngine;

public class FootstepVFXController : MonoBehaviour
{
    [SerializeField] private ParticleSystem footstepsVFX;
    [SerializeField] private PlayerController playerController;

    private bool _isPlaying;

    private void Awake()
    {
        if (playerController == null)
            playerController = GetComponent<PlayerController>();
    }

    private void Update()
    {
        bool shouldPlay = playerController._isRunning && playerController.isGrounded;

        if (shouldPlay && !_isPlaying)
            StartVFX();
        else if (!shouldPlay && _isPlaying)
            StopVFX();
    }

    private void StartVFX()
    {
        _isPlaying = true;
        footstepsVFX.Play();
    }

    private void StopVFX()
    {
        _isPlaying = false;
        // StopEmitting lets existing particles finish naturally instead of cutting off abruptly
        footstepsVFX.Stop(false, ParticleSystemStopBehavior.StopEmitting);
    }
}