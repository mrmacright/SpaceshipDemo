using UnityEngine;

public class MainMenuCircleButton : MonoBehaviour
{
    [SerializeField] GameObject onReturnToMenu;

    public void TriggerReturnToMenu()
    {
        if (onReturnToMenu != null)
            onReturnToMenu.SetActive(true);
    }
}
