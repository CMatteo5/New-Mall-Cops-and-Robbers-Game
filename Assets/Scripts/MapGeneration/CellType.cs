namespace MapGeneration
{
    /// <summary>
    /// What occupies a single grid cell. This is the coarse classification the
    /// generator works with; a Room cell additionally carries a RoomType (Store,
    /// CopOffice, Extract) via the cell's data. Paths get their own designs later.
    /// </summary>
    public enum CellType
    {
        Empty,   // nothing placed yet — the default state of every cell
        Room,    // occupied by a room (see the cell's RoomType for which kind)
        Path     // walkable corridor connecting rooms
    }
}