using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class TreeObject : DestructibleObject
{
    [SerializeField] private int woodItemID;
    [SerializeField] private int woodAmount = 3;
    [SerializeField] private float fallDuration = 1.5f; // match your Tree fall clip length

    private Animator _animator;
    private static readonly int DepletedHash = Animator.StringToHash("Depleted");

    [Header("Audio")]
    [SerializeField] private AudioClip hitSFX;
    [SerializeField] private AudioClip fallSFX;
    [SerializeField] private float hitSFXDuration = 0.15f;
    [SerializeField] private float fallSFXDuration = 1f;

    private AudioSource _audioSource;

    private void Start()
    {
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
            _audioSource = gameObject.AddComponent<AudioSource>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        _animator = GetComponent<Animator>();
        Debug.Log($"Tree spawned with health: {currentHealth.Value}, animator found: {_animator != null}");
    }
    // runs on all clients — just visuals
    protected override void OnHealthChanged(int prev, int next)
    {
        // play shake/hit vfx locally on every client
        // if (shakeVFX != null)
        //     shakeVFX.SetActive(true);

        if (hitSFX != null)
            StartCoroutine(PlayClipBriefly(_audioSource, hitSFX, hitSFXDuration));
    }

    protected override void OnDepleted()
    {
        // Play fall animation on all clients before despawning
        PlayFallAnimationClientRpc();

        // Despawn after animation finishes
        StartCoroutine(DespawnAfterFall());
    }

    [ClientRpc]
    private void PlayFallAnimationClientRpc()
    {
        _animator?.SetBool(DepletedHash, true);
    }

    private System.Collections.IEnumerator DespawnAfterFall()
    {
        yield return new WaitForSeconds(fallDuration);
        NetworkItemSpawner.Instance.SpawnItem(
            woodItemID, woodAmount, transform.position);

        PlayFallSFXClientRpc();

        GetComponent<NetworkObject>().Despawn();
    }

    [ClientRpc]
    private void PlayFallSFXClientRpc()
    {
        if (fallSFX != null)
            StartCoroutine(PlayAtPoint(fallSFX, transform.position, fallSFXDuration));
    }

    // Spawns temporary AudioSource so sound survives after object despawns
    private IEnumerator PlayAtPoint(AudioClip clip, Vector3 pos, float duration)
    {
        GameObject tempAudio = new GameObject("TempTreeAudio");
        tempAudio.transform.position = pos;
        AudioSource src = tempAudio.AddComponent<AudioSource>();
        src.clip = clip;
        src.Play();

        yield return new WaitForSeconds(duration);

        src.time = clip.length - 0.01f;
        yield return new WaitForSeconds(0.05f);
        Destroy(tempAudio);
    }

    private IEnumerator PlayClipBriefly(AudioSource source, AudioClip clip, float duration)
    {
        source.PlayOneShot(clip);
        yield return new WaitForSeconds(duration);
        source.Stop();
    }
}