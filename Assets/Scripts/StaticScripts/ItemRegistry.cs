using UnityEngine;

[CreateAssetMenu(fileName = "ItemRegistry", menuName = "Shop/Registry")]
public class ItemRegistry : ScriptableObject
{
    public ItemData[] items;
}