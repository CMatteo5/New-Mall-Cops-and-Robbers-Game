using UnityEngine;
using UnityEngine.UI;

public class ShopCloseButton : MonoBehaviour
{
    private Button button;
    private CustomPlayerMovement playerMovement;

    private void Start()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnCloseClicked);
    }

    // Called when this UI is opened - registers which player owns it
    public void Initialize(CustomPlayerMovement movement)
    {
        playerMovement = movement;
    }

    private void OnCloseClicked()
    {
        if (playerMovement != null)
            playerMovement.CloseShop();
    }
}