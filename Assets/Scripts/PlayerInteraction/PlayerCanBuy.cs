using UnityEngine;

public class PlayerCanBuy : MonoBehaviour
{

    [SerializeField] public bool playerCanBuy;
    [SerializeField] public Transform lastLocation;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerCanBuy = false;
    }

    public void flipPlayerCanBuy()
    {
        playerCanBuy = !playerCanBuy;
    }
}
