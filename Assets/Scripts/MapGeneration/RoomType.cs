namespace MapGeneration
{
    /// <summary>
    /// Category of a room cell. The generator uses this to decide placement
    /// rules (cop office is 2 cells, extracts must be distant, stores fill the
    /// remainder from the weighted pool).
    /// </summary>
    public enum RoomType
    {
        Store,       // weighted, fills the bulk of the mall
        CopOffice,   // exactly one, occupies 2 cells, holds the jail
        Extract      // 2-3 depending on robber count, distance-constrained
    }
}