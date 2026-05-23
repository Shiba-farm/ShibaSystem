// SlimeInitializer.cs — add this to your Slime prefab
using Unity.Behavior;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

public class SlimeInitializer : NetworkBehaviour
{
    private NavMeshAgent _navMeshAgent;
    private BehaviorGraphAgent _behaviorAgent;

    private void Awake()
    {
        // Get from same GameObject — no inspector wiring needed
        _navMeshAgent   = GetComponent<NavMeshAgent>();
        _behaviorAgent  = GetComponent<BehaviorGraphAgent>();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        _navMeshAgent.enabled  = false;
        _behaviorAgent.enabled = false;

        StartCoroutine(WaitForNavMesh());
    }

    private System.Collections.IEnumerator WaitForNavMesh()
    {
        _navMeshAgent.enabled = true;

        float timeout = 5f;
        float elapsed = 0f;

        while (!_navMeshAgent.isOnNavMesh && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (!_navMeshAgent.isOnNavMesh)
        {
            Debug.LogWarning($"[Slime] Failed to place on NavMesh at {transform.position}");
            yield break;
        }

        Debug.Log($"[Slime] On NavMesh ✅ enabling behavior");
        _behaviorAgent.enabled = true;
    }
}