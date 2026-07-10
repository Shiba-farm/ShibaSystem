using UnityEngine;
using UnityEngine.AI;

public class SlimeDebug : MonoBehaviour
{
    [SerializeField] private Transform targetPoint;

    private void Update()
    {
        // Press Space to test NavMeshAgent directly
        if (Input.GetKeyDown(KeyCode.Space))
        {
            var agent = GetComponent<NavMeshAgent>();
            if (agent == null)
            {
                Debug.LogError("[SlimeDebug] No NavMeshAgent found");
                return;
            }

            if (!agent.isOnNavMesh)
            {
                Debug.LogError("[SlimeDebug] Agent is NOT on NavMesh");
                return;
            }

            if (targetPoint == null)
            {
                Debug.LogWarning("[SlimeDebug] No target point assigned");
                return;
            }

            bool result = agent.SetDestination(targetPoint.position);
            Debug.Log($"[SlimeDebug] SetDestination result: {result} " +
                      $"target: {targetPoint.position}");
        }
    }
}