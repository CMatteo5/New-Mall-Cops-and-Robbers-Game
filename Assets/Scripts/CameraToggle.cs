using UnityEngine;
using UnityEngine.InputSystem;

public class CameraToggle : MonoBehaviour
{
    private bool isFirstPerson = false;

    public void OnSwitchView(InputValue value)
    {
        if (value.isPressed)
            Toggle();
    }

    public void Toggle()
    {
        isFirstPerson = !isFirstPerson;

        if (isFirstPerson)
            CameraRegistry.SetFirstPerson();
        else
            CameraRegistry.SetThirdPerson();
    }
}