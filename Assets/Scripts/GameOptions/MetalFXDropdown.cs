using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GameOptionsUtility;

public class MetalFXDropdown : MonoBehaviour
{
    public TMP_Text display;
    public Button leftButton;
    public Button rightButton;

    SpaceshipOptions opts;
    int index;

    // 0 = Off, 1 = Performance, 2 = Balanced, 3 = Quality
    readonly string[] names = { "Off", "Performance", "Balanced", "Quality" };

    void Start()
    {
        opts = FindObjectOfType<SpaceshipOptions>();
        if (!opts)
        {
            Debug.LogWarning("[MetalFX UI] No SpaceshipOptions found in scene.");
            return;
        }

        index = (int)opts.metalFXMode;
        if (index < 0 || index >= names.Length)
            index = 0;

        UpdateDisplay();
    }

    public void OnLeft()
    {
        if (names.Length == 0) return;

        index--;
        if (index < 0) index = names.Length - 1;

        UpdateDisplay();

    }
    public void OnRight()
    {
        if (names.Length == 0) return;

        index++;
        if (index >= names.Length) index = 0;

        UpdateDisplay();
    }

    void UpdateDisplay()
    {
        if (display != null && index >= 0 && index < names.Length)
            display.text = names[index];
    }

    public void PushToOptions()
    {
        if (!opts)
        {
            opts = FindObjectOfType<SpaceshipOptions>();
            if (!opts)
            {
                Debug.LogWarning("[MetalFX] PushToOptions called but no SpaceshipOptions in scene.");
                return;
            }
        }

        var newMode = (SpaceshipOptions.MetalFXMode)index;

        if (opts.metalFXMode == newMode)
        {
            Debug.Log($"[MetalFX] Mode unchanged ({newMode}), skipping apply.");
            return;
        }

        opts.metalFXMode = newMode;
        Debug.Log($"[MetalFX] Apply → Mode now = {opts.metalFXMode}");
        opts.ApplyMetalFX();
    }

    public void Apply()
    {
        PushToOptions();
    }
}
