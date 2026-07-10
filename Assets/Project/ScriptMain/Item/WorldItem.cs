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
    [SerializeField] private float magnetSpeed = 8f;        // เพิ่มจาก 1f → ดูดเร็วขึ้น
    [SerializeField] private float stopDistance = 0.5f;     // ระยะ XZ ที่ถือว่าถึงผู้เล่น
    [SerializeField] private float groundedDelay = 1.2f;    // รอให้ตกถึงพื้นก่อน (เพิ่มจาก 0.8f)

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

        // ── เคลื่อนเฉพาะแกน XZ เพื่อไม่ให้ของลอยขึ้นหา handAnchor ──────────────
        // ใช้ Y ของ item เองเพื่อให้ลอยอยู่กับที่ในแนวตั้ง
        Vector3 groundTarget = new Vector3(
            _pullTarget.position.x,
            transform.position.y,
            _pullTarget.position.z);

        transform.position = Vector3.MoveTowards(
            transform.position, groundTarget,
            magnetSpeed * Time.deltaTime);

        // เช็คระยะแบบ XZ เท่านั้น — ไม่ใช้ Y (ของอยู่พื้น, Player สูงกว่า)
        float xzDist = Vector2.Distance(
            new Vector2(transform.position.x, transform.position.z),
            new Vector2(_pullTarget.position.x, _pullTarget.position.z));

        if (xzDist <= stopDistance)
        {
            _reachedPlayer = true;
            _pullTarget = null;
            gameObject.SetActive(false);   // server จะ Despawn ผ่าน RequestPickupServerRpc
            return;
        }
    }

    public void OnPickedUp(ulong clientId)
    {
        // only the server calls this
        if (_isClaimed) return;
        _isClaimed = true;
        GetComponent<NetworkObject>().Despawn();
    }
}
