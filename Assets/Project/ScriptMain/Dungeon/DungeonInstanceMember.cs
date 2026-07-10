// DungeonInstanceMember.cs
// Phase B — Personal dungeon instancing.
//
// Lightweight, NON-networked tag component attached at spawn time (server-only,
// via DungeonManager.SpawnObjectY) to every interactive object placed inside a
// player's personal dungeon instance: rocks, ores, enemies, the ladder.
//
// It records which spatial instance slot the object lives in and which player
// (by clientId) owns that instance. Gameplay callbacks that used to assume a
// single global dungeon (DungeonManager.OnOreHarvested / OnEnemyKilled /
// OnRockBroken / WorldToGrid) now need this information to resolve the correct
// player's PlayerDungeonState / DungeonFloorData.
//
// This is intentionally a plain MonoBehaviour (not a NetworkBehaviour): it is
// added only on the server-side instance of a spawned NetworkObject and is only
// ever read by server-only code (see SlimeEnemy.OnDepleted, DungeonOreNode).

using UnityEngine;

namespace MyGame.Dungeon
{
    public class DungeonInstanceMember : MonoBehaviour
    {
        /// <summary>Which of DungeonManager.instances[] this object belongs to.</summary>
        public int slot;

        /// <summary>The clientId of the player whose personal dungeon this object belongs to.</summary>
        public ulong ownerClientId;
    }
}
