using UnityEngine;
using UnityEngine.Rendering;

public class DRDebugOverlay : MonoBehaviour
{
    public bool show = true;

    Rect area = new Rect(20, 20, 520, 170);

    Texture2D bg;

    int nativeW;
    int nativeH;

    void Start()
    {
        nativeW = Screen.currentResolution.width;
        nativeH = Screen.currentResolution.height;

        // Solid black background texture
        bg = new Texture2D(1, 1);
        bg.SetPixel(0, 0, new Color(0, 0, 0, 0.75f));
        bg.Apply();
    }

    void OnGUI()
    {
        if (!show) return;

        // HDRP returns Vector2
        Vector2 scaleVec = DynamicResolutionHandler.instance.GetResolvedScale();
        float scale = scaleVec.x;

        int scaledW = Mathf.RoundToInt(nativeW * scale);
        int scaledH = Mathf.RoundToInt(nativeH * scale);

        // Draw background
        GUI.DrawTexture(area, bg);

        // Draw text manually
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 22;
        style.richText = true;
        style.normal.textColor = Color.white;

        float y = area.y + 10;

        GUI.Label(new Rect(area.x + 10, y, area.width, 30),
                  "<b>Hardware Dynamic Resolution Debug</b>", style);
        y += 30;

        GUI.Label(new Rect(area.x + 10, y, area.width, 30),
                  "DR Scale: " + scale.ToString("0.00"), style);
        y += 30;

        GUI.Label(new Rect(area.x + 10, y, area.width, 30),
                  "Native Resolution: " + nativeW + " × " + nativeH, style);
        y += 30;

        GUI.Label(new Rect(area.x + 10, y, area.width, 30),
                  "Output Resolution: " + scaledW + " × " + scaledH, style);
        y += 30;

        GUI.Label(new Rect(area.x + 10, y, area.width, 30),
                  "HDRP Hardware DR: ACTIVE", style);
    }
}
