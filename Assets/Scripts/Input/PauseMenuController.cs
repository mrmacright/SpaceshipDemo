using UnityEngine;
using UnityEngine.InputSystem;

public class PauseInputListener : MonoBehaviour
{
    // Drag these in PLAY MODE or via a runtime finder (see note below)
    public GameObject onManagePause;
    public GameObject pauseMenu;

    void Update()
    {
        if (Gamepad.current == null)
            return;

        if (Gamepad.current.startButton.wasPressedThisFrame)
        {
            if (!onManagePause || !pauseMenu)
            {
                Debug.LogWarning("[PauseInputListener] Missing references");
                return;
            }

            bool paused = !onManagePause.activeSelf;

            // 1. Toggle pause driver
            onManagePause.SetActive(paused);

            // 2. Toggle actual menu
            pauseMenu.SetActive(paused);

            // 3. Safety time scale
            Time.timeScale = paused ? 0f : 1f;

            Debug.Log($"[PauseInputListener] Pause = {paused}");
        }
    }
}
