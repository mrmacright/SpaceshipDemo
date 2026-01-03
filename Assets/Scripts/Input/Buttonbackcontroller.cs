using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class CancelToBack : MonoBehaviour
{
    public GameObject backButton;

    void Update()
    {
        if (Keyboard.current?.escapeKey.wasPressedThisFrame == true)
            GoBack();

        if (Gamepad.current?.buttonEast.wasPressedThisFrame == true) // O / Circle
            GoBack();
    }

    void GoBack()
    {
        if (!backButton) return;

        EventSystem.current.SetSelectedGameObject(backButton);
        ExecuteEvents.Execute(backButton, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
    }
}
