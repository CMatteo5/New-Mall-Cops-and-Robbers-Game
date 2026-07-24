using System.Collections.Generic;
using UnityEngine;

namespace MapGeneration
{
    /// <summary>
    /// Goes on the ROOT of every room prefab. Links the prefab back to its
    /// RoomDefinition and exposes the physical anchors the generator needs:
    /// the entrance (which gets rotated to face the path) and the five loot
    /// spawn points. Designers build their room around these markers.
    /// </summary>
    public class RoomMarker : MonoBehaviour
    {
        [Header("Definition Link")]
        [Tooltip("The RoomDefinition describing this room's type/weight/loot.")]
        [SerializeField] private RoomDefinition definition;

        [Header("Entrance")]
        [Tooltip("Empty transform at the CENTER of the doorway, on the room's edge. Its +Z (blue arrow) must point OUT of the room, toward where the path will be. The generator rotates the whole room so this faces a path cell.")]
        [SerializeField] private Transform entrance;

        [Tooltip("Width of the doorway opening in meters (2-5m typical). The generator uses this to carve the matching gap in the path wall.")]
        [Min(1f)][SerializeField] private float entranceWidth = 3f;

        [Header("Loot Spawn Points")]
        [Tooltip("Exactly five empty transforms where loot can appear. Space them across the floor so grabbing loot means moving through the room.")]
        [SerializeField] private Transform[] lootSpawnPoints = new Transform[5];

        public RoomDefinition Definition => definition;
        public Transform Entrance => entrance;
        public float EntranceWidth => entranceWidth;
        public IReadOnlyList<Transform> LootSpawnPoints => lootSpawnPoints;

        public const int RequiredLootPoints = 5;

        /// <summary>
        /// Editor-time sanity check so designers catch mistakes before runtime.
        /// Draws the entrance direction and loot points in the Scene view.
        /// </summary>
        private void OnDrawGizmos()
        {
            if (entrance != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawSphere(entrance.position, 0.3f);
                Gizmos.DrawLine(entrance.position, entrance.position + entrance.forward * 2f);
            }

            if (lootSpawnPoints != null)
            {
                Gizmos.color = Color.yellow;
                foreach (var p in lootSpawnPoints)
                    if (p != null) Gizmos.DrawWireSphere(p.position, 0.4f);
            }
        }
    }
}