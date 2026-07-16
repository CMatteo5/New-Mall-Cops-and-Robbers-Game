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
        //Debug.Log($"BuyZone hit by: {collision.gameObject.name}");
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerCanBuy pcb = collision.gameObject.GetComponent<PlayerCanBuy>();
            pcb.lastLocation = itemSpawnLocation;
            pcb.flipPlayerCanBuy();
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