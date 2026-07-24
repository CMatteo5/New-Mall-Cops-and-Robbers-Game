namespace MapGeneration
{
    /// <summary>
    /// Shared spatial constants for the whole generation system. Rooms, the
    /// generator, and the path carver all measure against these, so there's
    /// exactly one place to change the scale.
    /// </summary>
    public static class GridConstants
    {
        /// <summary>Size of one square grid cell, in meters. Rooms and path cells are all this size.</summary>
        public const float CellSize = 12f;

        /// <summary>The cop office spans this many cells (a 1x2 rectangle = 12x24m).</summary>
        public const int CopOfficeCellCount = 2;

        /// <summary>Every store exposes exactly this many loot spawn points.</summary>
        public const int LootPointsPerStore = 5;
    }
}