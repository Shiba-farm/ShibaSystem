using System.Collections.Generic;
using UnityEngine;

public class SpawnPointManager : MonoBehaviour
{
    public static SpawnPointManager Instance { get; private set; }

    [System.Serializable]
    public struct DoorSpawnPoint
    {
        public string doorID;        // matches Door.doorID in the previous scene
        public Transform spawnPoint;
    }
    [Header("Door Spawn Points")]
    [SerializeField] private List<DoorSpawnPoint> doorSpawnPoints = new();
    [Header("Default Spawn Points")]
    [SerializeField] private List<Transform> spawnPoints = new();
    [SerializeField] private bool randomOrder = false;
    [SerializeField] private Transform dungeonSpawnPoint; // single override for dungeon

    private int _nextIndex = 0;
    private bool _usingDungeonOverride = false;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void SetDungeonSpawn(Vector3 position, Quaternion rotation)
    {
        if (dungeonSpawnPoint != null)
        {
            dungeonSpawnPoint.SetPositionAndRotation(position, rotation);
            _usingDungeonOverride = true;
        }
        else
        {
            Debug.LogWarning("[SpawnPoint] No dungeonSpawnPoint assigned — falling back to static points.");
            _usingDungeonOverride = false;
        }
    }

    public void ClearDungeonOverride()
    {
        _usingDungeonOverride = false;
    }

    public Vector3 GetNextPosition()
    {
        if (_usingDungeonOverride && dungeonSpawnPoint != null)
            return dungeonSpawnPoint.position;

        string doorID = SceneTransitionManager.LastUsedDoorID;

        if (!string.IsNullOrEmpty(doorID))
        {
            var doorSpawn = doorSpawnPoints.Find(d => d.doorID == doorID);
            if (doorSpawn.spawnPoint != null)
            {
                Debug.Log($"[SpawnPoint] Spawning at door point: {doorID}");
                return doorSpawn.spawnPoint.position;
            }
        }

        // Fall back to default spawn points
        if (spawnPoints.Count == 0) return Vector3.zero;

        int index = randomOrder
            ? Random.Range(0, spawnPoints.Count)
            : _nextIndex++ % spawnPoints.Count;

        return spawnPoints[index].position;
    }

    public Quaternion GetNextRotation()
    {
        if (_usingDungeonOverride && dungeonSpawnPoint != null)
            return dungeonSpawnPoint.rotation;

        string doorID = SceneTransitionManager.LastUsedDoorID;

        if (!string.IsNullOrEmpty(doorID))
        {
            var doorSpawn = doorSpawnPoints.Find(d => d.doorID == doorID);
            if (doorSpawn.spawnPoint != null)
                return doorSpawn.spawnPoint.rotation;
        }

        if (spawnPoints.Count == 0) return Quaternion.identity;

        int index = (_nextIndex - 1 + spawnPoints.Count) % spawnPoints.Count;
        return spawnPoints[index].rotation;
    }

    private void OnDrawGizmos()
    {
        if (spawnPoints == null) return;
        Gizmos.color = Color.green;
        foreach (var point in spawnPoints)
        {
            if (point == null) continue;
            Gizmos.DrawWireSphere(point.position, 0.5f);
            Gizmos.DrawRay(point.position, point.forward * 1.5f);
        }

        if (dungeonSpawnPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(dungeonSpawnPoint.position, 0.6f);
            Gizmos.DrawRay(dungeonSpawnPoint.position, dungeonSpawnPoint.forward * 2f);
        }

        Gizmos.color = Color.cyan;
        foreach (var door in doorSpawnPoints)
        {
            if (door.spawnPoint == null) continue;
            Gizmos.DrawWireSphere(door.spawnPoint.position, 0.5f);
            Gizmos.DrawRay(door.spawnPoint.position, door.spawnPoint.forward * 1.5f);
        }
    }
}