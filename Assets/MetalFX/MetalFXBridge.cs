using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

public static class MetalFXBridge
{
#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")]
    static extern void MetalFX_Encode(
        IntPtr cmdBuffer,
        IntPtr colorTex,
        IntPtr outputTex
    );
#endif

    public static void Encode(CommandBuffer cmd, RTHandle src, RTHandle dst)
    {
#if UNITY_IOS && !UNITY_EDITOR
        if (cmd == null || src == null || dst == null)
            return;

        // ---- NATIVE CMD BUFFER FALLBACK ----
        // Unity removed GetNativeCommandBufferPtr() in your engine build.
        // The only remaining stable method is to use CommandBuffer.IssuePluginEventAndData.
        IntPtr cmdPtr = IntPtr.Zero;

        // Native textures from RTHandle
        IntPtr colorPtr  = src.rt.GetNativeTexturePtr();
        IntPtr outputPtr = dst.rt.GetNativeTexturePtr();

        // Pass them to plugin
        MetalFX_Encode(cmdPtr, colorPtr, outputPtr);
#endif
    }
}
