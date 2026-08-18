using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Shop/Item")]
public class ItemData : ScriptableObject
{
    public string itemName;
    public int cost;
    [Tooltip("Money credited per unit when sold at a SellZone.")]
    public int sellPrice;
    public Sprite icon;
    public GameObject prefab;
    public string description;
}