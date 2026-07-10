using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable]
[GeneratePropertyBag]
[NodeDescription(
    name: "Detect Nearest Player",
    story: "[Agent] detects nearest player within [DetectRange] and sets [Target]",
    category: "Action/Detection",
    id: "detect_nearest_player"
)]
public partial class DetectNearestPlayerAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    [SerializeReference] public BlackboardVariable<float> DetectRange;
    [SerializeReference] public BlackboardVariable<GameObject> Target;

    protected override Status OnStart()
    {
        if (Agent?.Value == null) return Status.Failure;

        var players = GameObject.FindGameObjectsWithTag("Player");
        Debug.Log($"[Detect] Found {players.Length} players with tag 'Player'");

        var nearest = FindNearestPlayer();
        Debug.Log($"[Detect] Nearest player: {(nearest != null ? nearest.name : "none")} " +
                  $"range:{DetectRange?.Value}");

        if (nearest == null)
        {
            Target.Value = null;
            return Status.Failure;
        }

        Target.Value = nearest;
        return Status.Success;
    }

    private GameObject FindNearestPlayer()
    {
        float bestDist = DetectRange?.Value ?? 10f;
        GameObject bestPlayer = null;

        // Find all players by tag
        var players = GameObject.FindGameObjectsWithTag("Player");
        foreach (var player in players)
        {
            float dist = Vector3.Distance(
                Agent.Value.transform.position,
                player.transform.position
            );

            if (dist < bestDist)
            {
                bestDist = dist;
                bestPlayer = player;
            }
        }

        return bestPlayer;
    }
}