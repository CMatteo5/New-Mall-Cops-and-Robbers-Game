using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace MapGeneration
{
    /// <summary>
    /// Owns the map seed, keeps it synced across the network, and builds + draws a
    /// real generated mall whenever the seed changes. Generation runs identically
    /// on every machine from the shared seed. If a seed produces an invalid layout
    /// (fails connectivity), it retries with a derived seed until one validates.
    /// </summary>
    [RequireComponent(typeof(MallVisualizer))]
    public class MallGenerator : NetworkBehaviour
    {
        private readonly NetworkVariable<int> _seed = new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        public int Seed => _seed.Value;
        public MallGrid Grid { get; private set; }

        [Header("Config")]
        [SerializeField] private GenerationConfig config;

        [Header("Player / Robber Counts (temporary — wire to LobbyManager later)")]
        [Tooltip("Used to size the grid. Later this reads from your lobby.")]
        [SerializeField] private int testPlayerCount = 4;
        [Tooltip("Used to decide extract count. Later this reads from your lobby.")]
        [SerializeField] private int testRobberCount = 2;

        [Tooltip("How many seeds to try before giving up, if layouts fail validation.")]
        [SerializeField] private int maxGenerationAttempts = 25;

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

        /// <summary>
        /// Build and draw the mall from a seed. Runs identically on all machines.
        /// Retries with derived seeds if a layout fails connectivity, so the result
        /// is always a valid, fully-connected mall.
        /// </summary>
        private void Regenerate(int seed)
        {
            if (config == null)
            {
                Debug.LogError("[MallGenerator] No GenerationConfig assigned.");
                return;
            }

            for (int attempt = 0; attempt < maxGenerationAttempts; attempt++)
            {
                // Derive a distinct seed per attempt so retries differ, but stay
                // deterministic (same base seed -> same sequence of attempts).
                int attemptSeed = seed + attempt * 7919; // 7919 is prime, spreads seeds
                SeededRng rng = new SeededRng(attemptSeed);

                MallLayout layout = new MallLayout(config, rng);
                layout.CreateGrid(testPlayerCount);

                bool ok = layout.PlaceCopOffice()
                          && layout.PlaceExtracts(testRobberCount);

                if (ok)
                {
                    layout.PlacePathsAndStores();
                    if (layout.ValidateConnectivity())
                    {
                        Grid = layout.Grid;
                        _visualizer.Draw(Grid);
                        Debug.Log($"[MallGenerator] Generated valid mall from seed {seed} (attempt {attempt + 1}).");
                        return;
                    }
                }
            }

            Debug.LogWarning($"[MallGenerator] No valid layout after {maxGenerationAttempts} attempts for seed {seed}. Check config constraints.");
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