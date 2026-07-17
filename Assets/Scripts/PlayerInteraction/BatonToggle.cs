using UnityEngine;

public class BatonToggle : MonoBehaviour
{

    public PlayerTeam playerTeam;

    // Update is called once per frame
    void Update()
    {
        if (playerTeam != null)
        {
            if (playerTeam.IsCop == true)
            {
                Debug.Log("Hello!");
                gameObject.SetActive(true);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
        
    }
}
