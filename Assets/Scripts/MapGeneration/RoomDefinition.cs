using UnityEngine;

namespace MapGeneration
{
    /// <summary>
    /// The generator-facing definition of a room. Held as a ScriptableObject so
    /// the generator can read type/weight/loot WITHOUT instantiating the prefab,
    /// and so designers can make many prefab variants that share one definition
    /// or tune balance without touching prefabs.
    ///
    /// Scene-space data (entrance position, loot point positions) is NOT here —
    /// that lives on the prefab via RoomMarker, because it's physical placement.
    /// </summary>
    [CreateAssetMenu(fileName = "RoomDefinition", menuName = "MapGeneration/Room Definition")]
    public class RoomDefinition : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("What kind of room this is. Drives placement rules.")]
        public RoomType roomType = RoomType.Store;

        [Tooltip("Human-readable name for designers/debugging, e.g. 'Electronics Store'.")]
        public string displayName = "New Room";

        [Header("Selection")]
        [Tooltip("Relative spawn weight among rooms of the same type. Higher = more common. Ignored for CopOffice (always exactly one).")]
        [Min(0.0001f)] public float selectionWeight = 1f;

        [Header("Prefab Variants")]
        [Tooltip("One or more prefab variations sharing this definition. The generator picks one deterministically. Each must have a RoomMarker.")]
        public GameObject[] prefabVariants;

        [Header("Store-only")]
        [Tooltip("Loot table this store draws from. Leave null for non-store rooms.")]
        public LootTable lootTable;
    }
}