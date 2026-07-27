using System.Collections.Generic;
using UnityEngine;

namespace MapGeneration
{
    /// <summary>
    /// One cell of the grid. Knows what it is, and — if it's a room — which
    /// RoomType and which logical room it belongs to (so multi-cell rooms like
    /// the 12x24 cop office are tracked as a single unit across two cells).
    /// </summary>
    public class Cell
    {
        public CellType Type = CellType.Empty;
        public RoomType RoomType;      // meaningful only when Type == Room
        public int RoomId = -1;        // -1 = not part of any room; shared by all cells of one room

        public bool IsEmpty => Type == CellType.Empty;
    }

    /// <summary>
    /// The abstract mall layout: a 2D grid of cells, addressed by (x, y) column
    /// and row. This is pure data — no GameObjects. The layout algorithm (Task 3)
    /// writes into it; the visualizer (Step 4) reads it to draw placeholder cubes.
    /// A logical "room" may span multiple cells; every cell of that room shares a
    /// RoomId, which is how the two-cell cop office stays one unit.
    /// </summary>
    public class MallGrid
    {
        public int Width { get; }
        public int Height { get; }

        private readonly Cell[,] _cells;
        private int _nextRoomId = 0;

        public MallGrid(int width, int height)
        {
            Width = width;
            Height = height;
            _cells = new Cell[width, height];
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    _cells[x, y] = new Cell();
        }

        public bool InBounds(int x, int y) => x >= 0 && x < Width && y >= 0 && y < Height;

        public Cell Get(int x, int y) => InBounds(x, y) ? _cells[x, y] : null;

        /// <summary>Hand out a fresh unique room id. Every logical room gets one.</summary>
        public int NewRoomId() => _nextRoomId++;

        /// <summary>
        /// Mark a set of cells as one room of the given type, all sharing a single
        /// new RoomId. Pass one coordinate for a normal room, or two (or more) for
        /// a multi-cell room like the cop office. Returns the RoomId assigned, or
        /// -1 if any target cell is out of bounds or already occupied.
        /// </summary>
        public int PlaceRoom(RoomType roomType, IReadOnlyList<Vector2Int> cells)
        {
            // Validate first — don't half-place a room then fail.
            foreach (var c in cells)
            {
                if (!InBounds(c.x, c.y)) return -1;
                if (!_cells[c.x, c.y].IsEmpty) return -1;
            }

            int id = NewRoomId();
            foreach (var c in cells)
            {
                Cell cell = _cells[c.x, c.y];
                cell.Type = CellType.Room;
                cell.RoomType = roomType;
                cell.RoomId = id;
            }
            return id;
        }

        /// <summary>Mark a single cell as a path. Returns false if out of bounds or occupied.</summary>
        public bool PlacePath(int x, int y)
        {
            if (!InBounds(x, y)) return false;
            if (!_cells[x, y].IsEmpty) return false;
            _cells[x, y].Type = CellType.Path;
            return true;
        }

        /// <summary>Convert a grid coordinate to a world position (cell centers), using GridConstants.CellSize.</summary>
        public Vector3 CellToWorld(int x, int y)
        {
            float size = GridConstants.CellSize;
            return new Vector3(x * size, 0f, y * size);
        }
    }
}