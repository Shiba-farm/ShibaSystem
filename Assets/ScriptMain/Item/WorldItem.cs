using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class WorldItem : NetworkBehaviour, IPickupable
{
    public NetworkVariable<int> itemID = new(writePerm: NetworkVariableWritePermission.Server);
    public NetworkVariable<int> quantity = new(writePerm: NetworkVariableWritePermission.Server);

    private bool _isClaimed;      // prevents two players racing for the same item
    private bool _canBePulled = false;
    private Transform _pullTarget;
    private bool _reachedPlayer;
    [SerializeField] private float magnetSpeed = 1f;
    [SerializeField] private float stopDistance = 0.3f;
    [SerializeField] private float groundedDelay = 0.8f;

    public int ItemID => itemID.Value;
    public int Quantity => quantity.Value;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        // pop upward with random horizontal scatter
        if (TryGetComponent<Rigidbody>(out var rb))
        {
            Vector3 popForce = new Vector3(
                Random.Range(-2f, 2f),
                Random.Range(3f, 5f),    // upward force
                Random.Range(-2f, 2f));
            rb.AddForce(popForce, ForceMode.Impulse);
        }

        StartCoroutine(EnableMagnetAfterDelay());
    }

    private IEnumerator EnableMagnetAfterDelay()
    {
        yield return new WaitForSeconds(groundedDelay);
        _canBePulled = true;
    }

    public void OnMagnetPull(Transform target)
    {
        // purely visual on the local client — no authority needed
        if (!_canBePulled) return;
        if (_reachedPlayer) return;
        if (TryGetComponent<Rigidbody>(out var rb))
        {
            rb.isKinematic = true;   // ← stops gravity fighting the pull
            rb.linearVelocity = Vector3.zero;
        }

        _pullTarget = target;
    }

    private void Update()
    {
        if (_pullTarget == null || _reachedPlayer) return;

        // float dist = Vector3.Distance(transform.position, _pullTarget.position);

        // if (dist <= stopDistance)
        // {
        //     _reachedPlayer = true;
        //     _pullTarget = null;
        //     gameObject.SetActive(false);   // hide it, server will despawn shortly
        //     return;
        // }

        transform.position = Vector3.MoveTowards(
            transform.position, _pullTarget.position,
            magnetSpeed * Time.deltaTime);
    }

    public void OnPickedUp(ulong clientId)
    {
        // only the server calls this
        if (_isClaimed) return;
        _isClaimed = true;
        GetComponent<NetworkObject>().Despawn();
    }
}
