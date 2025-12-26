using System.Runtime.InteropServices;
using UnityEngine;

public static class MetalSurfaceScaler
{
#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void SetMetalSurfaceScale(float scale);
#else
    private static void SetMetalSurfaceScale(float scale) { }
#endif

    public static void ApplyScale(float scale)
    {
        scale = Mathf.Clamp(scale, 0.1f, 1f);
        Debug.Log($"[MetalSurfaceScaler] Request scale {scale}");
        SetMetalSurfaceScale(scale);
    }
}
