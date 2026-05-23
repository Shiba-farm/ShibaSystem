using System;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.AI;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable]
[GeneratePropertyBag]
[NodeDescription(
    name: "Chase Player",
    story: "[Agent] chases [Target] until within [AttackRange] or loses sight at [LoseRange]",
    category: "Action/Combat",
    id: "chase_player"
)]
public partial class ChasePlayerAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    [SerializeReference] public BlackboardVariable<GameObject> Target;
    [SerializeReference] public BlackboardVariable<float>      AttackRange;
    [SerializeReference] public BlackboardVariable<float>      LoseRange;

    private NavMeshAgent _navAgent;
    private Animator     _animator;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");

    protected override Status OnStart()
    {
        if (Agent?.Value == null || Target?.Value == null)
            return Status.Failure;

        _navAgent = Agent.Value.GetComponent<NavMeshAgent>();
        _animator = Agent.Value.GetComponentInChildren<Animator>();

        if (_navAgent == null || !_navAgent.isOnNavMesh)
            return Status.Failure;

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (Target?.Value == null || _navAgent == null)
            return Status.Failure;

        float dist = Vector3.Distance(
            Agent.Value.transform.position,
            Target.Value.transform.position
        );

        // Lost the player — go back to roaming
        if (dist > (LoseRange?.Value ?? 20f))
        {
            Target.Value = null;
            SetSpeed(0f);
            return Status.Failure;
        }

        // Reached attack range — success signals attack can begin
        if (dist <= (AttackRange?.Value ?? 1.5f))
        {
            SetSpeed(0f);
            _navAgent.ResetPath();
            return Status.Success;
        }

        // Keep chasing
        _navAgent.SetDestination(Target.Value.transform.position);
        SetSpeed(_navAgent.velocity.magnitude);

        return Status.Running;
    }

    protected override void OnEnd()
    {
        SetSpeed(0f);
        _navAgent?.ResetPath();
    }

    private void SetSpeed(float speed)
    {
        _animator?.SetFloat(SpeedHash, speed);
    }
}