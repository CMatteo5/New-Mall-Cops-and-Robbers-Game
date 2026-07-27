using System.Collections.Generic;
using System;

namespace MapGeneration
{
    /// <summary>
    /// A deterministic random-number source for map generation. Every random
    /// choice in the generator MUST go through an instance of this, created from
    /// the shared seed, so that the host and every client produce an identical
    /// mall. Never use UnityEngine.Random or time-based values in generation —
    /// those differ per machine and would desync the layout.
    /// </summary>
    public class SeededRng
    {
        private readonly Random _random;

        /// <summary>The seed this instance was created with (kept for debugging/logging).</summary>
        public int Seed { get; }

        public SeededRng(int seed)
        {
            Seed = seed;
            _random = new Random(seed);
        }

        /// <summary>Random int in [0, maxExclusive).</summary>
        public int Next(int maxExclusive) => _random.Next(maxExclusive);

        /// <summary>Random int in [minInclusive, maxExclusive).</summary>
        public int Next(int minInclusive, int maxExclusive) => _random.Next(minInclusive, maxExclusive);

        /// <summary>Random float in [0, 1). Use this to drive weighted picks like LootTable.Pick.</summary>
        public float NextFloat() => (float)_random.NextDouble();

        /// <summary>
        /// Pick an index from a list of weights, proportional to each weight.
        /// This is the core primitive for all weighted selection (rooms, paths,
        /// loot). Returns -1 only if the weights are empty or sum to zero.
        /// </summary>
        public int WeightedPick(IReadOnlyList<float> weights)
        {
            float total = 0f;
            for (int i = 0; i < weights.Count; i++) total += weights[i];
            if (total <= 0f) return -1;

            float target = NextFloat() * total;
            float running = 0f;
            for (int i = 0; i < weights.Count; i++)
            {
                running += weights[i];
                if (target < running) return i;
            }
            return weights.Count - 1; // float-safety fallback
        }
    }
}
