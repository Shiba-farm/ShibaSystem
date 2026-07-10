using System;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.AI;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using System.Collections.Generic;

[Serializable]
[GeneratePropertyBag]
[NodeDescription(
    name: "Roam Random Waypoint",
    story: "[Agent] roams randomly among [WayPoints]",
    category: "Action/Navigation",
    id: "roam_random_waypoint"
)]
public partial class RoamRandomWaypointAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    [SerializeReference] public BlackboardVariable<List<GameObject>> WayPoints;
    [SerializeReference] public BlackboardVariable<float> StopDistance;
    [SerializeReference] public BlackboardVariable<float> WaitTime;

    private NavMeshAgent _navAgent;
    private Animator _animator;
    private Transform[] _points;
    private int _currentIndex = -1;
    private float _waitTimer = 0f;
    private bool _isWaiting = false;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");

    protected override Status OnStart()
    {
        if (Agent?.Value == null)
        {
            Debug.LogError("[RoamAction] Agent is null");
            return Status.Failure;
        }

        _navAgent = Agent.Value.GetComponent<NavMeshAgent>();
        if (_navAgent == null)
        {
            Debug.LogError("[RoamAction] No NavMeshAgent found");
            return Status.Failure;
        }

        if (!_navAgent.isOnNavMesh)
        {
            Debug.LogError($"[RoamAction] Agent not on NavMesh at {Agent.Value.transform.position}");
            return Status.Failure;
        }

        if (WayPoints?.Value == null || WayPoints.Value.Count == 0)
        {
            Debug.LogError("[RoamAction] WayPoints is null or empty");
            return Status.Failure;
        }

        // Get animator from agent or its children
        _animator = Agent.Value.GetComponent<Animator>();

        _points = new Transform[WayPoints.Value.Count];
        for (int i = 0; i < WayPoints.Value.Count; i++)
            _points[i] = WayPoints.Value[i].transform;

        MoveToNextWaypoint();
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (_navAgent == null) return Status.Failure;

        if (_isWaiting)
        {
            SetAnimatorSpeed(0f);   // idle while waiting

            _waitTimer -= Time.deltaTime;
            if (_waitTimer <= 0f)
            {
                _isWaiting = false;
                MoveToNextWaypoint();
            }
            return Status.Running;
        }

        // Drive animation from actual nav speed
        float speed = _navAgent.velocity.magnitude;
        SetAnimatorSpeed(speed);

        if (!_navAgent.pathPending &&
            _navAgent.remainingDistance <= (StopDistance?.Value ?? 0.5f))
        {
            _isWaiting = true;
            _waitTimer = WaitTime?.Value ?? 1f;
            _navAgent.ResetPath();
        }

        return Status.Running;
    }

    protected override void OnEnd()
    {
        SetAnimatorSpeed(0f);
        _navAgent?.ResetPath();
    }

    private void SetAnimatorSpeed(float speed)
    {
        if (_animator == null) return;
        _animator.SetFloat(SpeedHash, speed);
    }

    private void MoveToNextWaypoint()
    {
        if (_points == null || _points.Length == 0) return;

        int newIndex;
        do
        {
            newIndex = UnityEngine.Random.Range(0, _points.Length);
        } while (_points.Length > 1 && newIndex == _currentIndex);

        _currentIndex = newIndex;
        _navAgent.SetDestination(_points[_currentIndex].position);
    }
}