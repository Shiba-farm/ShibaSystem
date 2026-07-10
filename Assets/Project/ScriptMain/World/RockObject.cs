using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class RockObject : DestructibleObject
{
    [System.Serializable]
    public class OreDrop
    {
        public int itemID;
        public int minAmount;
        public int maxAmount;
    }

    [Header("Rock Settings")]
    [SerializeField] private List<OreDrop> drops;
    [SerializeField] private GameObject destroyVFX;
    [SerializeField] private AudioClip destroySFX;
    [SerializeField] private AudioClip hitSFX;

    [Header("Hit Feel")]
    [SerializeField] private float shakeDuration = 0.15f;
    [SerializeField] private float shakeMagnitude = 0.06f;

    [Header("Audio")]
    [SerializeField] private float hitSFXDuration = 0.15f;
    [SerializeField] private float destroySFXDuration = 0.5f;

    private Vector3 _originPos;
    private Coroutine _shakeCoroutine;
    private AudioSource _audioSource;

    private void Start()
    {
        _originPos = transform.position;
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
            _audioSource = gameObject.AddComponent<AudioSource>();
    }

    protected override bool CanBeDamagedBy(ToolAction tool)
        => tool == ToolAction.Mine;

    protected override void OnHealthChanged(int prev, int next)
    {
        if (_shakeCoroutine != null)
            StopCoroutine(_shakeCoroutine);
        _shakeCoroutine = StartCoroutine(ShakeRoutine());

        if (next > 0 && hitSFX != null)
            StartCoroutine(PlayClipBriefly(hitSFX, hitSFXDuration));
    }

    protected override void OnDepleted()
    {
        foreach (var drop in drops)
        {
            int amount = Random.Range(drop.minAmount, drop.maxAmount + 1);
            NetworkItemSpawner.Instance.SpawnItem(
                drop.itemID, amount, transform.position);
        }

        PlayDestroyVFXClientRpc();
        GetComponent<NetworkObject>().Despawn(false);
    }

    [ClientRpc]
    private void PlayDestroyVFXClientRpc()
    {
        if (destroyVFX != null)
        {
            // Spawn VFX prefab ที่ตำแหน่งแร่ (ถ้าเป็น Prefab ที่มี OreBreakEffect จะ Play อัตโนมัติ)
            var vfxInstance = Instantiate(destroyVFX, transform.position, transform.rotation);

            // Particle ที่เป็น child (ถ้ามี) — backward compat
            foreach (var ps in vfxInstance.GetComponentsInChildren<ParticleSystem>(true))
                ps.Play();
        }

        if (destroySFX != null)
            StartCoroutine(PlaySoundThenHide(destroySFX, destroySFXDuration));
        else
            gameObject.SetActive(false);
    }

    // Hide object after sound finishes — keeps coroutine alive until done
    private IEnumerator PlaySoundThenHide(AudioClip clip, float duration)
    {
        _audioSource.PlayOneShot(clip);
        yield return new WaitForSeconds(duration);
        _audioSource.Stop();
        gameObject.SetActive(false);
    }

    // Plays a clip for a set duration then stops — avoids full-length playback
    private IEnumerator PlayClipBriefly(AudioClip clip, float duration)
    {
        _audioSource.PlayOneShot(clip);
        yield return new WaitForSeconds(duration);
        _audioSource.Stop();
    }

    private IEnumerator ShakeRoutine()
    {
        float elapsed = 0f;
        while (elapsed < shakeDuration)
        {
            float x = Mathf.Sin(elapsed / shakeDuration * Mathf.PI * 8f) * shakeMagnitude;
            transform.position = _originPos + new Vector3(x, 0f, 0f);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = _originPos;
        _shakeCoroutine = null;
    }
}