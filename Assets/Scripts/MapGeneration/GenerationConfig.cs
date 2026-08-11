using UnityEngine;

namespace MapGeneration
{
    /// <summary>
    /// All the tunable numbers for mall generation in one asset, so fairness and
    /// density can be adjusted in the Inspector without code changes. The
    /// generator reads these; nothing writes them at runtime.
    /// </summary>
    [CreateAssetMenu(fileName = "GenerationConfig", menuName = "MapGeneration/Generation Config")]
    public class GenerationConfig : ScriptableObject
    {
        [Header("Grid Sizing")]
        [Tooltip("Higher = more cells per player = bigger malls. Grid side = ceil(sqrt(players * this)) + padding.")]
        [Min(0.5f)] public float cellsPerPlayer = 2.5f;

        [Tooltip("Extra cells added to each side after the sqrt sizing, for breathing room.")]
        [Min(0)] public int sizePadding = 2;

        [Tooltip("Grid never gets smaller than this on a side, regardless of player count.")]
        [Min(3)] public int minGridSide = 5;

        [Tooltip("Grid never gets larger than this on a side, to cap sprawl.")]
        [Min(3)] public int maxGridSide = 8;

        [Header("Extract Counts")]
        [Tooltip("Number of extract points when there are 1-2 robbers.")]
        [Min(1)] public int extractsFewRobbers = 2;

        [Tooltip("Number of extract points when there are 3+ robbers.")]
        [Min(1)] public int extractsManyRobbers = 3;

        [Tooltip("Robber count at or above which we use the 'many' extract count.")]
        [Min(1)] public int manyRobbersThreshold = 3;

        [Header("Fairness Distances (in grid cells, Manhattan)")]
        [Tooltip("Minimum distance from the cop office to any extract.")]
        [Min(1)] public int minOfficeToExtract = 3;

        [Tooltip("Minimum distance between any two extracts.")]
        [Min(1)] public int minExtractToExtract = 3;

        /// <summary>
        /// Compute the grid side length for a given player count, clamped to the
        /// configured min/max. Square grid, so this is one side.
        /// </summary>
        public int GridSideFor(int playerCount)
        {
            int fromPlayers = Mathf.CeilToInt(Mathf.Sqrt(playerCount * cellsPerPlayer)) + sizePadding;
            return Mathf.Clamp(fromPlayers, minGridSide, maxGridSide);
        }

        /// <summary>How many extracts for a given robber count.</summary>
        public int ExtractCountFor(int robberCount)
        {
            return robberCount >= manyRobbersThreshold ? extractsManyRobbers : extractsFewRobbers;
        }
    }
}
