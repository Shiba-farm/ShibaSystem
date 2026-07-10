using UnityEngine;
using UnityEngine.AI;

public class SlimeAI : MonoBehaviour
{
    [Header("Roam")]
    [SerializeField] private float roamRadius = 6f;
    [SerializeField] private float idleTimeMin = 1f;
    [SerializeField] private float idleTimeMax = 3f;

    private NavMeshAgent _agent;
    private Animator _animator;
    private float _idleTimer;
    private bool _isIdle = true;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _animator = GetComponent<Animator>();
    }

    private void Start()
    {
        StartIdle();
    }

    private void Update()
    {
        float speed = _agent.velocity.magnitude;
        _animator.SetFloat("Speed", speed);

        if (_isIdle)
        {
            _idleTimer -= Time.deltaTime;
            if (_idleTimer <= 0f)
                PickRoamPoint();
        }
        else
        {
            // Arrived?
            if (!_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance)
                StartIdle();
        }
        float animationWalkSpeed = 2f; // tweak this
        _animator.speed = speed > 0.1f ? speed / animationWalkSpeed : 1f;
    }

    private void StartIdle()
    {
        _isIdle = true;
        _idleTimer = Random.Range(idleTimeMin, idleTimeMax);
        _agent.ResetPath();
    }

    private void PickRoamPoint()
    {
        for (int i = 0; i < 10; i++)
        {
            Vector3 randomDir = transform.position + Random.insideUnitSphere * roamRadius;

            if (NavMesh.SamplePosition(randomDir, out var hit, roamRadius, NavMesh.AllAreas))
            {
                _agent.SetDestination(hit.position);
                _isIdle = false;
                return;
            }
        }

        // No valid point found — try again after idle
        StartIdle();
    }
}
