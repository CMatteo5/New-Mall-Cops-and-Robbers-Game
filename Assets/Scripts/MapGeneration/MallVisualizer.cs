using System.Collections.Generic;
using UnityEngine;

namespace MapGeneration
{
    /// <summary>
    /// Draws a MallGrid using placeholder cubes so we can SEE the layout while
    /// building the generator, before any real room prefabs exist. One cube per
    /// occupied cell, tinted by what the cell is. Purely visual — it reads the
    /// grid, never writes to it. Real room prefabs replace this in a later task.
    /// </summary>
    public class MallVisualizer : MonoBehaviour
    {
        [Tooltip("The flat cube tile used for every cell (the PlaceholderRoom prefab).")]
        [SerializeField] private GameObject placeholderPrefab;

        [Header("Cell Colors")]
        [SerializeField] private Color storeColor = new Color(0.3f, 0.6f, 1f);
        [SerializeField] private Color copOfficeColor = new Color(1f, 0.4f, 0.3f);
        [SerializeField] private Color extractColor = new Color(0.4f, 1f, 0.5f);
        [SerializeField] private Color pathColor = new Color(0.7f, 0.7f, 0.7f);

        // Everything we spawn is parented here and tracked so we can clear it
        // before redrawing (regeneration reuses the same visualizer).
        private readonly List<GameObject> _spawned = new List<GameObject>();

        /// <summary>Remove every cube from the previous draw. Called before each redraw.</summary>
        public void Clear()
        {
            foreach (var go in _spawned)
                if (go != null) Destroy(go);
            _spawned.Clear();
        }

        /// <summary>
        /// Draw the given grid. Spawns one tinted cube per non-empty cell at its
        /// world position. Empty cells are skipped (they show as gaps).
        /// </summary>
        public void Draw(MallGrid grid)
        {
            Clear();
            if (grid == null || placeholderPrefab == null) return;

            for (int x = 0; x < grid.Width; x++)
            {
                for (int y = 0; y < grid.Height; y++)
                {
                    Cell cell = grid.Get(x, y);
                    if (cell == null || cell.IsEmpty) continue;

                    Vector3 localPos = grid.CellToWorld(x, y);
                    GameObject go = Instantiate(placeholderPrefab, transform.position + localPos, Quaternion.identity, transform);
                    Tint(go, ColorFor(cell));
                    _spawned.Add(go);
                }
            }
        }

        private Color ColorFor(Cell cell)
        {
            if (cell.Type == CellType.Path) return pathColor;

            switch (cell.RoomType)
            {
                case RoomType.CopOffice: return copOfficeColor;
                case RoomType.Extract: return extractColor;
                default: return storeColor;
            }
        }

        private void Tint(GameObject go, Color color)
        {
            Renderer r = go.GetComponentInChildren<Renderer>();
            if (r != null)
            {
                // MaterialPropertyBlock tints without creating a material instance
                // per cube — cheap, and doesn't leak materials.
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                r.GetPropertyBlock(block);
                block.SetColor("_BaseColor", color); // URP lit
                block.SetColor("_Color", color);     // fallback for built-in/other shaders
                r.SetPropertyBlock(block);
            }
        }
    }
}