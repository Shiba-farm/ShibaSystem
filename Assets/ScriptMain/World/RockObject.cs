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

    [Header("Hit Feel")]
    [SerializeField] private float shakeDuration = 0.15f;
    [SerializeField] private float shakeMagnitude = 0.06f;

    private Vector3 _originPos;
    private Coroutine _shakeCoroutine;

    private void Start()
    {
        // Cache origin so shake always returns to exact position
        _originPos = transform.position;
    }

    protected override bool CanBeDamagedBy(ToolAction tool)
        => tool == ToolAction.Mine;

    // Runs on all clients when health changes — safe for visuals
    protected override void OnHealthChanged(int prev, int next)
    {
        if (_shakeCoroutine != null)
            StopCoroutine(_shakeCoroutine);
        _shakeCoroutine = StartCoroutine(ShakeRoutine());
    }

    protected override void OnDepleted()
    {
        foreach (var drop in drops)
        {
            int amount = Random.Range(drop.minAmount, drop.maxAmount + 1);
            NetworkItemSpawner.Instance.SpawnItem(
                drop.itemID, amount, transform.position);
        }

        // Tell all clients to play VFX before despawning
        PlayDestroyVFXClientRpc();

        GetComponent<NetworkObject>().Despawn(false);
        gameObject.SetActive(false);
    }

    [ClientRpc]
    private void PlayDestroyVFXClientRpc()
    {
        if (destroyVFX == null) return;

        destroyVFX.transform.SetParent(null);
        destroyVFX.SetActive(true);

        // Manually play all particle systems since Play On Awake is disabled
        foreach (var ps in destroyVFX.GetComponentsInChildren<ParticleSystem>(true))
            ps.Play();
    }

    // Subtle left-right sine shake — magnitude intentionally small
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