using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace MapGeneration
{
    /// <summary>
    /// Owns the map seed, keeps it synced across the network, and builds + draws
    /// the grid whenever the seed changes. This step fills the grid with a simple
    /// TEST pattern (a two-cell cop office, a couple of stores, an extract, and a
    /// path) just to prove the whole pipeline works and is visible. The real
    /// layout algorithm replaces BuildTestGrid in Task 3.
    /// </summary>
    [RequireComponent(typeof(MallVisualizer))]
    public class MallGenerator : NetworkBehaviour
    {
        private readonly NetworkVariable<int> _seed = new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        public int Seed => _seed.Value;
        public SeededRng Rng { get; private set; }
        public MallGrid Grid { get; private set; }

        [Header("Test Grid Size (temporary — Task 3 computes this from player count)")]
        [SerializeField] private int testWidth = 5;
        [SerializeField] private int testHeight = 5;

        private MallVisualizer _visualizer;

        private void Awake()
        {
            _visualizer = GetComponent<MallVisualizer>();
        }

        public override void OnNetworkSpawn()
        {
            _seed.OnValueChanged += OnSeedChanged;

            if (_seed.Value != 0)
                Regenerate(_seed.Value);

            if (IsServer)
                PickNewSeed();
        }

        public override void OnNetworkDespawn()
        {
            _seed.OnValueChanged -= OnSeedChanged;
        }

        private void OnSeedChanged(int previous, int current)
        {
            Regenerate(current);
        }

        /// <summary>Build the RNG, build the grid, draw it. Runs on every machine identically.</summary>
        private void Regenerate(int seed)
        {
            Rng = new SeededRng(seed);
            Grid = BuildTestGrid(Rng);
            _visualizer.Draw(Grid);
            Debug.Log($"[MallGenerator] Regenerated from seed {seed}");
        }

        /// <summary>
        /// TEMPORARY test layout. Places a 2-cell cop office, two stores, an
        /// extract, and a short path — enough to confirm colors, the multi-cell
        /// office, and world positioning all work. Task 3 replaces this entirely.
        /// </summary>
        private MallGrid BuildTestGrid(SeededRng rng)
        {
            MallGrid grid = new MallGrid(testWidth, testHeight);

            // Two-cell cop office at the bottom-left (cells (0,0) and (0,1)).
            grid.PlaceRoom(RoomType.CopOffice, new List<Vector2Int>
            {
                new Vector2Int(0, 0),
                new Vector2Int(0, 1)
            });

            // A couple of stores.
            grid.PlaceRoom(RoomType.Store, new List<Vector2Int> { new Vector2Int(2, 0) });
            grid.PlaceRoom(RoomType.Store, new List<Vector2Int> { new Vector2Int(2, 2) });

            // An extract in the far corner.
            grid.PlaceRoom(RoomType.Extract, new List<Vector2Int> { new Vector2Int(4, 4) });

            // A short path connecting toward the middle.
            grid.PlacePath(1, 0);
            grid.PlacePath(2, 1);
            grid.PlacePath(3, 3);

            return grid;
        }

        public void PickNewSeed()
        {
            if (!IsServer) return;
            int newSeed = System.Environment.TickCount ^ (int)(Time.realtimeSinceStartup * 1000f);
            if (newSeed == 0) newSeed = 1;
            _seed.Value = newSeed;
        }

        public void SetSeed(int seed)
        {
            if (!IsServer) return;
            if (seed == 0) seed = 1;
            _seed.Value = seed;
        }

        // Press G in Play mode (host) to regenerate with a new seed and watch it redraw.
        private void Update()
        {
            if (!IsServer) return;
            if (UnityEngine.InputSystem.Keyboard.current != null &&
                UnityEngine.InputSystem.Keyboard.current.gKey.wasPressedThisFrame)
            {
                PickNewSeed();
            }
        }
    }
}