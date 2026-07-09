using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Shop/Item")]
public class ItemData : ScriptableObject
{
    public string itemName;
    public int cost;
    public Sprite icon;
    public GameObject prefab;
    public string description;
}