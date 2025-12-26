using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

class MetalFXPass : CustomPass
{
    protected override void Execute(CustomPassContext ctx)
    {
#if UNITY_IOS && !UNITY_EDITOR
        RTHandle src = ctx.cameraColorBuffer;
        RTHandle dst = ctx.customColorBuffer.Value;

        MetalFXBridge.Encode(ctx.cmd, src, dst);
#endif
    }
}
