using System.Collections.Generic;
using UnityEngine;

namespace MapGeneration
{
    /// <summary>
    /// A weighted list of loot that can spawn at a store's spawn points.
    /// Each store's RoomDefinition points at one of these. Weights are relative:
    /// an entry of weight 3 is three times as likely as an entry of weight 1.
    /// </summary>
    [CreateAssetMenu(fileName = "LootTable", menuName = "MapGeneration/Loot Table")]
    public class LootTable : ScriptableObject
    {
        [System.Serializable]
        public struct LootEntry
        {
            [Tooltip("The item prefab to spawn. Must have a NetworkObject — loot is networked.")]
            public GameObject itemPrefab;

            [Tooltip("Relative weight. Higher = more common. Must be > 0.")]
            [Min(0.0001f)] public float weight;
        }

        [SerializeField] private List<LootEntry> entries = new List<LootEntry>();

        public IReadOnlyList<LootEntry> Entries => entries;

        /// <summary>Total of all weights, used by the generator to roll a pick.</summary>
        public float TotalWeight
        {
            get
            {
                float sum = 0f;
                foreach (var e in entries) sum += e.weight;
                return sum;
            }
        }

        /// <summary>
        /// Deterministically pick a loot prefab given a roll in [0, 1).
        /// The generator supplies the roll from its seeded RNG so results are
        /// identical on every client. Returns null only if the table is empty.
        /// </summary>
        public GameObject Pick(float roll01)
        {
            float total = TotalWeight;
            if (total <= 0f || entries.Count == 0) return null;

            float target = roll01 * total;
            float running = 0f;

            foreach (var e in entries)
            {
                running += e.weight;
                if (target < running) return e.itemPrefab;
            }

            return entries[entries.Count - 1].itemPrefab; // float-safety fallback
        }
    }
}