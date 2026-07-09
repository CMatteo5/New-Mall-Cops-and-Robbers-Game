using UnityEngine;

public class BuyZone : MonoBehaviour
{
    [SerializeField] private Transform itemSpawnLocation;
    [SerializeField] public BoxCollider BoxCollider;

    void Start()
    {
        
    }


    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            collision.gameObject.GetComponent<PlayerCanBuy>().flipPlayerCanBuy();
            collision.gameObject.GetComponent<PlayerCanBuy>().lastLocation = itemSpawnLocation;
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            collision.gameObject.GetComponent<PlayerCanBuy>().flipPlayerCanBuy();
            //collision.gameObject.GetComponent<PlayerCanBuy>().lastLocation = null;
        }
    }
}