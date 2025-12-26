using UnityEngine;

public class ForceUnityViewport : MonoBehaviour
{
    Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void OnPreCull()
    {
        if (!cam) return;

        // Always force Unity’s viewport to match full display area
        cam.rect = new Rect(0f, 0f, 1f, 1f);
    }
}
