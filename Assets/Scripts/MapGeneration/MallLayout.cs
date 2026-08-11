using System.Collections.Generic;
using UnityEngine;
using System.Collections.Generic;

namespace MapGeneration
{
    /// <summary>
    /// Builds a mall layout into a MallGrid, step by step, using a seeded RNG and
    /// a GenerationConfig. Each Task 3 step adds a stage here. This file currently
    /// covers grid creation and cop-office placement; extracts, stores, paths, and
    /// validation are added in the following steps.
    ///
    /// All randomness flows through the passed-in SeededRng so every client builds
    /// the identical layout.
    /// </summary>
    public class MallLayout
    {
        private readonly GenerationConfig _config;
        private readonly SeededRng _rng;

        public MallGrid Grid { get; private set; }

        // Remembered as we go, because later stages depend on them.
        public List<Vector2Int> OfficeCells { get; private set; } = new List<Vector2Int>();

        public MallLayout(GenerationConfig config, SeededRng rng)
        {
            _config = config;
            _rng = rng;
        }

        /// <summary>Create the empty grid sized for the given player count.</summary>
        public void CreateGrid(int playerCount)
        {
            int side = _config.GridSideFor(playerCount);
            Grid = new MallGrid(side, side);
        }

        /// <summary>
        /// Place the 2-cell cop office, biased toward the grid center. Tries
        /// candidate positions scored by closeness to center, picking among the
        /// best few with the seeded RNG so it's central but not always identical.
        /// Returns true on success.
        /// </summary>
        public bool PlaceCopOffice()
        {
            if (Grid == null) return false;

            Vector2 center = new Vector2((Grid.Width - 1) / 2f, (Grid.Height - 1) / 2f);

            // Gather every legal 2-cell office placement (horizontal or vertical),
            // each scored by how close its center is to the grid center.
            List<(List<Vector2Int> cells, float score)> candidates =
                new List<(List<Vector2Int>, float)>();

            for (int x = 0; x < Grid.Width; x++)
            {
                for (int y = 0; y < Grid.Height; y++)
                {
                    // Vertical pair: (x,y) and (x,y+1)
                    TryAddOfficeCandidate(candidates, center,
                        new Vector2Int(x, y), new Vector2Int(x, y + 1));

                    // Horizontal pair: (x,y) and (x+1,y)
                    TryAddOfficeCandidate(candidates, center,
                        new Vector2Int(x, y), new Vector2Int(x + 1, y));
                }
            }

            if (candidates.Count == 0) return false;

            // Sort by score ascending (lower = closer to center = better).
            candidates.Sort((a, b) => a.score.CompareTo(b.score));

            // Pick among the best handful so it's central but varied by seed.
            int poolSize = Mathf.Min(4, candidates.Count);
            int choice = _rng.Next(poolSize);
            List<Vector2Int> chosen = candidates[choice].cells;

            int id = Grid.PlaceRoom(RoomType.CopOffice, chosen);
            if (id < 0) return false;

            OfficeCells = chosen;
            return true;
        }

        private void TryAddOfficeCandidate(
            List<(List<Vector2Int>, float)> candidates,
            Vector2 center, Vector2Int a, Vector2Int b)
        {
            if (!Grid.InBounds(a.x, a.y) || !Grid.InBounds(b.x, b.y)) return;

            // Score = summed distance of both cells from center. Lower is better.
            float score = Vector2.Distance(new Vector2(a.x, a.y), center)
                        + Vector2.Distance(new Vector2(b.x, b.y), center);

            candidates.Add((new List<Vector2Int> { a, b }, score));
     
       }
        // Remembered for later stages (path carving connects these).
        public List<Vector2Int> ExtractCells { get; private set; } = new List<Vector2Int>();

        /// <summary>
        /// Place the extract points. Count comes from the config based on robber
        /// count. Each extract is biased toward the grid perimeter and must satisfy
        /// the minimum distances from the office and from every already-placed
        /// extract. Returns true if all requested extracts were placed.
        /// </summary>
        public bool PlaceExtracts(int robberCount)
        {
            if (Grid == null || OfficeCells.Count == 0) return false;

            int wanted = _config.ExtractCountFor(robberCount);
            ExtractCells.Clear();

            for (int i = 0; i < wanted; i++)
            {
                Vector2Int? spot = FindExtractSpot();
                if (spot == null) return false; // couldn't satisfy constraints

                Grid.PlaceRoom(RoomType.Extract, new List<Vector2Int> { spot.Value });
                ExtractCells.Add(spot.Value);
            }

            return true;
        }

        /// <summary>
        /// Find one legal extract cell: empty, far enough from the office and from
        /// all placed extracts, scored by closeness to the nearest edge (perimeter
        /// bias). Returns null if no cell satisfies the hard constraints.
        /// </summary>
        private Vector2Int? FindExtractSpot()
        {
            List<(Vector2Int cell, float score)> candidates =
                new List<(Vector2Int, float)>();

            for (int x = 0; x < Grid.Width; x++)
            {
                for (int y = 0; y < Grid.Height; y++)
                {
                    if (!Grid.Get(x, y).IsEmpty) continue;

                    Vector2Int cell = new Vector2Int(x, y);

                    // Hard constraint: distance from office.
                    if (ManhattanToNearest(cell, OfficeCells) < _config.minOfficeToExtract)
                        continue;

                    // Hard constraint: distance from other extracts.
                    if (ExtractCells.Count > 0 &&
                        ManhattanToNearest(cell, ExtractCells) < _config.minExtractToExtract)
                        continue;

                    // Soft bias: closeness to nearest edge (lower = closer to edge = better).
                    int distToEdge = Mathf.Min(x, Grid.Width - 1 - x, y, Grid.Height - 1 - y);
                    candidates.Add((cell, distToEdge));
                }
            }

            if (candidates.Count == 0) return null;

            candidates.Sort((a, b) => a.score.CompareTo(b.score));
            int poolSize = Mathf.Min(5, candidates.Count);
            return candidates[_rng.Next(poolSize)].cell;
        }

        /// <summary>Smallest Manhattan distance from a cell to any cell in a set.</summary>
        private int ManhattanToNearest(Vector2Int from, List<Vector2Int> set)
        {
            int best = int.MaxValue;
            foreach (var c in set)
            {
                int d = Mathf.Abs(from.x - c.x) + Mathf.Abs(from.y - c.y);
                if (d < best) best = d;
            }
            return best;
        }

        /// <summary>
        /// Carve a connected path network, then fill path-adjacent empty cells with
        /// stores. Paths are carved to connect the office and all extracts into one
        /// network; stores only go where they can open onto a path. Call after the
        /// office and extracts are placed.
        /// </summary>
        public void PlacePathsAndStores()
        {
            CarvePathSpine();
            FillStores();
        }

        /// <summary>
        /// Connect the office and every extract with paths using simple L-shaped
        /// routes (horizontal then vertical). Every carved cell that's empty becomes
        /// a path; cells already occupied by rooms are stepped over (the route still
        /// counts as connecting through them via adjacency). Result is one connected
        /// path network touching all key locations.
        /// </summary>
        private void CarvePathSpine()
        {
            // Anchor point near the office to route everything to/from.
            Vector2Int hub = AdjacentEmptyOrSelf(OfficeCells[0]);

            foreach (var extract in ExtractCells)
            {
                Vector2Int target = AdjacentEmptyOrSelf(extract);
                CarveL(hub, target);
            }

            // Also link extracts to each other so the network is well-connected,
            // not just a star from the office.
            for (int i = 0; i < ExtractCells.Count - 1; i++)
            {
                Vector2Int a = AdjacentEmptyOrSelf(ExtractCells[i]);
                Vector2Int b = AdjacentEmptyOrSelf(ExtractCells[i + 1]);
                CarveL(a, b);
            }
        }

        /// <summary>Carve an L-shaped path from a to b: horizontal run, then vertical run.</summary>
        private void CarveL(Vector2Int a, Vector2Int b)
        {
            int x = a.x, y = a.y;

            int stepX = b.x > x ? 1 : -1;
            while (x != b.x)
            {
                Grid.PlacePath(x, y); // no-op if occupied; fine
                x += stepX;
            }

            int stepY = b.y > y ? 1 : -1;
            while (y != b.y)
            {
                Grid.PlacePath(x, y);
                y += stepY;
            }

            Grid.PlacePath(x, y);
        }

        /// <summary>
        /// Return an empty cell adjacent to the given cell, or the cell itself if
        /// none is empty. Used to anchor path routes just outside rooms rather than
        /// trying to carve through them.
        /// </summary>
        private Vector2Int AdjacentEmptyOrSelf(Vector2Int cell)
        {
            foreach (var n in Neighbors(cell))
                if (Grid.InBounds(n.x, n.y) && Grid.Get(n.x, n.y).IsEmpty)
                    return n;
            return cell;
        }

        /// <summary>
        /// Fill every empty cell that touches a path with a store, so each store can
        /// open onto that path. Empty cells with no path neighbor are left empty
        /// (they become gaps — Step 5 validates reachability).
        /// </summary>
        private void FillStores()
        {
            for (int x = 0; x < Grid.Width; x++)
            {
                for (int y = 0; y < Grid.Height; y++)
                {
                    Cell cell = Grid.Get(x, y);
                    if (!cell.IsEmpty) continue;

                    if (HasPathNeighbor(new Vector2Int(x, y)))
                        Grid.PlaceRoom(RoomType.Store, new List<Vector2Int> { new Vector2Int(x, y) });
                }
            }
        }

        private bool HasPathNeighbor(Vector2Int cell)
        {
            foreach (var n in Neighbors(cell))
            {
                if (!Grid.InBounds(n.x, n.y)) continue;
                if (Grid.Get(n.x, n.y).Type == CellType.Path) return true;
            }
            return false;
        }

        /// <summary>The four orthogonal neighbors of a cell.</summary>
        private IEnumerable<Vector2Int> Neighbors(Vector2Int c)
        {
            yield return new Vector2Int(c.x + 1, c.y);
            yield return new Vector2Int(c.x - 1, c.y);
            yield return new Vector2Int(c.x, c.y + 1);
            yield return new Vector2Int(c.x, c.y - 1);
        }

        /// <summary>
        /// Verify every room and path cell is reachable from the office through
        /// the path network (rooms count as reachable if they're adjacent to a
        /// reachable path). Flood-fills from an office-adjacent path cell. Returns
        /// true only if the whole mall is one connected space.
        /// </summary>
        public bool ValidateConnectivity()
        {
            if (Grid == null) return false;

            // Find a path cell adjacent to the office to start the flood from.
            Vector2Int start = new Vector2Int(-1, -1);
            foreach (var oc in OfficeCells)
            {
                foreach (var n in Neighbors(oc))
                {
                    if (Grid.InBounds(n.x, n.y) && Grid.Get(n.x, n.y).Type == CellType.Path)
                    {
                        start = n;
                        break;
                    }
                }
                if (start.x >= 0) break;
            }
            if (start.x < 0) return false; // office isn't even touching a path

            // Flood-fill across path cells.
            bool[,] visited = new bool[Grid.Width, Grid.Height];
            Queue<Vector2Int> queue = new Queue<Vector2Int>();
            queue.Enqueue(start);
            visited[start.x, start.y] = true;

            while (queue.Count > 0)
            {
                Vector2Int cur = queue.Dequeue();
                foreach (var n in Neighbors(cur))
                {
                    if (!Grid.InBounds(n.x, n.y)) continue;
                    if (visited[n.x, n.y]) continue;
                    if (Grid.Get(n.x, n.y).Type != CellType.Path) continue;

                    visited[n.x, n.y] = true;
                    queue.Enqueue(n);
                }
            }

            // Every ROOM cell must be adjacent to a visited path cell, and every
            // PATH cell must have been visited (no isolated path islands).
            for (int x = 0; x < Grid.Width; x++)
            {
                for (int y = 0; y < Grid.Height; y++)
                {
                    Cell cell = Grid.Get(x, y);

                    if (cell.Type == CellType.Path && !visited[x, y])
                        return false; // isolated path island

                    if (cell.Type == CellType.Room)
                    {
                        bool touchesReachablePath = false;
                        foreach (var n in Neighbors(new Vector2Int(x, y)))
                        {
                            if (Grid.InBounds(n.x, n.y) && visited[n.x, n.y])
                            {
                                touchesReachablePath = true;
                                break;
                            }
                        }
                        if (!touchesReachablePath) return false; // unreachable room
                    }
                }
            }

            return true;
        }
    }

}