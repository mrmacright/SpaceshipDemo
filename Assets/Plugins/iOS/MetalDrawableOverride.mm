#import <UIKit/UIKit.h>
#import <QuartzCore/CAMetalLayer.h>
#import <Metal/Metal.h>
#import <MetalFX/MetalFX.h>
#import <objc/message.h>
#include <sys/sysctl.h>

@interface UIDevice (ModelIdentifier)
@property (nonatomic, readonly) NSString *modelIdentifier;
@end

@implementation UIDevice (ModelIdentifier)
- (NSString *)modelIdentifier {
    static NSString *identifier;
    static dispatch_once_t onceToken;
    dispatch_once(&onceToken, ^{
        size_t size;
        sysctlbyname("hw.machine", NULL, &size, NULL, 0);
        char *machine = (char *)malloc(size > 0 ? size : 1);
        if (machine) {
            sysctlbyname("hw.machine", machine, &size, NULL, 0);
            identifier = [[NSString alloc] initWithUTF8String:machine];
            free(machine);
        } else {
            identifier = @"";
        }
    });
    return identifier;
}
@end

#undef HAS_METALFX
#define HAS_METALFX 0

extern "C" UIView* UnityGetGLView();
extern "C" void UnityRequestRenderingResolution(unsigned w, unsigned h);

static float g_lastScale = 1.0f;
static CAMetalLayer* g_lastLayer = nil;
static int g_unityReportedW = -1;
static int g_unityReportedH = -1;

static id<MTLFXTemporalScaler> g_mfxScaler = nil;
static int g_mfxMode = -1;

static float g_mfxInputScale = 1.0f; // 0.5, 0.75, 1.0 based on mode

static void RecreateMetalFXScalerIfNeeded(int mode);

static int g_mfxCfgInputW = -1;
static int g_mfxCfgInputH = -1;
static bool g_ipadSkipPresentOnce = false;

extern "C" void MetalOverride_ReportUnityScreenSize(int w, int h)
{
    g_unityReportedW = w;
    g_unityReportedH = h;
    NSLog(@"[MetalFX] Unity reports render size = %d x %d", w, h);
    // Recreate scaler if size changed while mode is same
    RecreateMetalFXScalerIfNeeded(g_mfxMode);
}

static void RecreateMetalFXScalerIfNeeded(int mode)
{
#if 1 // Force-disable MetalFX without removing code
    g_mfxScaler = nil;
    g_mfxMode = 0;
    NSLog(@"[MetalFX] Disabled (compile-time guard) – skipping scaler creation");
    return;
#endif

    if (mode == 0)
    {
        g_mfxScaler = nil;
        g_mfxMode = 0;
        NSLog(@"[MetalFX] Mode OFF → Scaler disabled");
        return;
    }

    float desiredScale = 1.0f;
    switch (mode)
    {
        case 1: desiredScale = 0.5f; break;     // Performance = 50%
        case 2: desiredScale = 0.75f; break;    // Balanced   = 75%
        case 3: desiredScale = 1.0f; break;     // Quality    = 100%
        default: desiredScale = 1.0f; break;
    }

    if (mode == g_mfxMode && g_mfxScaler != nil && g_mfxCfgInputW == (int)(g_unityReportedW * desiredScale) && g_mfxCfgInputH == (int)(g_unityReportedH * desiredScale))
    {
        NSLog(@"[MetalFX] Reuse scaler for mode %d (input %dx%d from output %dx%d scale=%.2f)", mode, g_mfxCfgInputW, g_mfxCfgInputH, g_unityReportedW, g_unityReportedH, desiredScale);
        return;
    }

    if (g_unityReportedW <= 0 || g_unityReportedH <= 0)
    {
        NSLog(@"[MetalFX] Cannot create scaler: invalid size");
        return;
    }

    g_mfxMode = mode;
    g_mfxScaler = nil;

    id<MTLDevice> device = MTLCreateSystemDefaultDevice();
    if (!device)
    {
        NSLog(@"[MetalFX] ERROR: No Metal device");
        return;
    }

    static dispatch_once_t sOnceSupport;
    dispatch_once(&sOnceSupport, ^{
        BOOL supportsApple7 = NO;
#if defined(__IPHONE_13_0)
        if ([device respondsToSelector:@selector(supportsFamily:)])
        {
            supportsApple7 = [device supportsFamily:MTLGPUFamilyApple7];
        }
#endif
        NSLog(@"[MetalFX] Device: %@, supports Apple7 family: %@", device.name, supportsApple7 ? @"YES" : @"NO");
    });

    MTLFXTemporalScalerDescriptor *desc = [MTLFXTemporalScalerDescriptor new];

    int inputW  = (int)llround((double)g_unityReportedW * (double)desiredScale);
    int inputH  = (int)llround((double)g_unityReportedH * (double)desiredScale);
    int outputW = g_unityReportedW;
    int outputH = g_unityReportedH;

    desc.inputWidth  = inputW;
    desc.inputHeight = inputH;
    desc.outputWidth = outputW;
    desc.outputHeight = outputH;

    g_mfxCfgInputW = inputW;
    g_mfxCfgInputH = inputH;
    g_mfxInputScale = desiredScale;

    // Match HDR pipeline (layer uses RGBA16Float) and keep configuration minimal
    desc.colorTextureFormat  = MTLPixelFormatRGBA16Float;
    desc.outputTextureFormat = MTLPixelFormatRGBA16Float;

    // Disable optional inputs unless explicitly provided elsewhere
    desc.depthTextureFormat  = MTLPixelFormatInvalid;
    desc.motionTextureFormat = MTLPixelFormatInvalid;

    // Do not require per-frame content properties when not provided
    desc.inputContentPropertiesEnabled = NO;

    // Keep scaling window tight until full integration is ready
    desc.inputContentMinScale = 1.0f;
    desc.inputContentMaxScale = 1.0f;

    g_mfxScaler = [desc newTemporalScalerWithDevice:device];

    if (g_mfxScaler)
    {
        UnityRequestRenderingResolution((unsigned)inputW, (unsigned)inputH);

        NSLog(@"[MetalFX] Scaler created for mode %d (input=%dx%d output=%dx%d scale=%.2f fmt=%lu)", mode, inputW, inputH, outputW, outputH, desiredScale, (unsigned long)desc.outputTextureFormat);
    }
    else
    {
        NSLog(@"[MetalFX] ERROR: Failed to create scaler (in=%dx%d out=%dx%d scale=%.2f colorFmt=%lu outFmt=%lu depth=%lu motion=%lu props=%@)",
              inputW, inputH, outputW, outputH, desiredScale,
              (unsigned long)desc.colorTextureFormat, (unsigned long)desc.outputTextureFormat,
              (unsigned long)desc.depthTextureFormat, (unsigned long)desc.motionTextureFormat,
              desc.inputContentPropertiesEnabled ? @"ON" : @"OFF");
    }
}

extern "C" void MetalFX_SetMode(int mode)
{
#if 1 // Force-disable MetalFX without removing code
    NSLog(@"[MetalFX] SetMode(%d) ignored – MetalFX disabled", mode);
    g_mfxMode = 0;
    g_mfxScaler = nil;
    return;
#endif

    NSLog(@"[MetalFX] SetMode called with %d", mode);
    g_mfxMode = mode;
    RecreateMetalFXScalerIfNeeded(mode);
}

// ---------------------------------------------------------------------
// Drawable scale (unchanged)
// ---------------------------------------------------------------------

static float GetBaseScaleForDevice(void)
{
    UIUserInterfaceIdiom idiom = UIDevice.currentDevice.userInterfaceIdiom;

    if (idiom == UIUserInterfaceIdiomPad)
        return 3.0f;   // iPad DPR
    else
        return 2.0f;   // iPhone DPR
}

static void ApplyDrawableScale(void)
{
    UIView* view = &UnityGetGLView ? UnityGetGLView() : nil;
    if (!view || !view.window) return;

    CALayer* base = view.layer;
    if (![base isKindOfClass:[CAMetalLayer class]]) return;

    CAMetalLayer* layer = (CAMetalLayer*)base;

    if (g_lastLayer != layer)
        g_lastLayer = layer;

    id<MTLDevice> device = MTLCreateSystemDefaultDevice();
    BOOL hdr = YES;

    layer.device = device;
    layer.framebufferOnly = NO;
    // Use a single SDR sRGB swapchain on all devices for consistent presentation
    layer.pixelFormat = MTLPixelFormatBGRA8Unorm_sRGB;
    layer.wantsExtendedDynamicRangeContent = NO;
    layer.colorspace = CGColorSpaceCreateWithName(kCGColorSpaceSRGB);

    UIScreen* screen = view.window.windowScene.screen;
    CGSize nativePx = screen.nativeBounds.size;

    UIInterfaceOrientation o = view.window.windowScene.effectiveGeometry.interfaceOrientation;

    BOOL landscape = (o == UIInterfaceOrientationLandscapeLeft ||
                      o == UIInterfaceOrientationLandscapeRight);

    if (landscape && nativePx.width < nativePx.height)
    {
        CGFloat t = nativePx.width;
        nativePx.width = nativePx.height;
        nativePx.height = t;
    }

    float baseScale = GetBaseScaleForDevice();
    float finalScale = g_lastScale * baseScale;

    CGSize targetPx;
    targetPx.width  = nativePx.width  * g_lastScale;
    targetPx.height = nativePx.height * g_lastScale;

    layer.drawableSize = targetPx;
    layer.contentsScale = finalScale;
    g_ipadSkipPresentOnce = true;

    // Defer Unity resolution change to next runloop so the compositor latches the new layer size first
    unsigned deferredW = (unsigned)targetPx.width;
    unsigned deferredH = (unsigned)targetPx.height;
    dispatch_async(dispatch_get_main_queue(), ^{
        UnityRequestRenderingResolution(deferredW, deferredH);
        // After Unity switches, commit once more so the first presented frame matches the new size
        [view setNeedsLayout];
        [view layoutIfNeeded];
        [layer setNeedsDisplay];
        [CATransaction flush];
    });

    // Force immediate drawable/layout commit after resolution change to avoid overscan until reactivation
    // Ensure the view and layer pick up the new size this frame
    [view setNeedsLayout];
    [view layoutIfNeeded];
    [layer setNeedsDisplay];
    [CATransaction flush];

    // Also re-commit on the next runloop to ensure compositor picks up size before next present
    dispatch_async(dispatch_get_main_queue(), ^{
        [view setNeedsLayout];
        [view layoutIfNeeded];
        [layer setNeedsDisplay];
        [CATransaction flush];
    });

    [view.window setNeedsLayout];
    [view.window layoutIfNeeded];
}

static void InstallListeners(void)
{
    NSNotificationCenter* nc = NSNotificationCenter.defaultCenter;

    [nc addObserverForName:UIDeviceOrientationDidChangeNotification
                    object:nil queue:nil
                usingBlock:^(NSNotification* n){ ApplyDrawableScale(); }];

    [nc addObserverForName:UIScreenModeDidChangeNotification
                    object:nil queue:nil
                usingBlock:^(NSNotification* n){ ApplyDrawableScale(); }];

    // FIX FOR UI BREAKING AFTER APP RESUME
    [nc addObserverForName:UIApplicationWillEnterForegroundNotification
                    object:nil queue:nil
                usingBlock:^(NSNotification* n){ ApplyDrawableScale(); }];

    [nc addObserverForName:UIApplicationDidBecomeActiveNotification
                    object:nil queue:nil
                usingBlock:^(NSNotification* n){ ApplyDrawableScale(); }];
}

extern "C" void MetalOverride_SetDrawableScale(float scale)
{
    // Normalize Unity's float → exact tiers
    if (scale >= 0.875f)      scale = 1.0f;    // 100%
    else if (scale >= 0.625f) scale = 0.75f;   // 75%
    else                      scale = 0.5f;    // 50%

    g_lastScale = scale;

    static dispatch_once_t once;
    dispatch_once(&once, ^{ InstallListeners(); });

    ApplyDrawableScale();
}

extern "C" void MetalFX_Encode(void* cmdPtr, void* colorInPtr, void* outPtr)
{
#if HAS_METALFX
    if (!g_mfxScaler || g_mfxMode == 0) return;

    id<MTLCommandBuffer> cmd = (__bridge id<MTLCommandBuffer>)cmdPtr;
    id<MTLTexture> src       = (__bridge id<MTLTexture>)colorInPtr;
    id<MTLTexture> dst       = (__bridge id<MTLTexture>)outPtr;

    // Debug: verify encode path and texture sizes
    //NSLog(@"[MetalFX] Encode src=%lux%lu dst=%lux%lu pf src=%lu dst=%lu", (unsigned long)src.width, (unsigned long)src.height, (unsigned long)dst.width, (unsigned long)dst.height, (unsigned long)src.pixelFormat, (unsigned long)dst.pixelFormat);

    // Call the encode method supported by this SDK at runtime without referencing unknown symbols
    if ([g_mfxScaler respondsToSelector:@selector(encodeToCommandBuffer:sourceTexture:destinationTexture:)])
    {
        typedef void (*EncodeFuncType)(id, SEL, id<MTLCommandBuffer>, id<MTLTexture>, id<MTLTexture>);
        EncodeFuncType send = (EncodeFuncType)objc_msgSend;
        send(g_mfxScaler, @selector(encodeToCommandBuffer:sourceTexture:destinationTexture:), cmd, src, dst);
        return;
    }
    if ([g_mfxScaler respondsToSelector:@selector(encodeToCommandBuffer:inputTexture:outputTexture:)])
    {
        typedef void (*EncodeFuncType)(id, SEL, id<MTLCommandBuffer>, id<MTLTexture>, id<MTLTexture>);
        EncodeFuncType send = (EncodeFuncType)objc_msgSend;
        send(g_mfxScaler, @selector(encodeToCommandBuffer:inputTexture:outputTexture:), cmd, src, dst);
        return;
    }
    if ([g_mfxScaler respondsToSelector:@selector(encodeToCommandBuffer:sourceTexture:outputTexture:)])
    {
        typedef void (*EncodeFuncType)(id, SEL, id<MTLCommandBuffer>, id<MTLTexture>, id<MTLTexture>);
        EncodeFuncType send = (EncodeFuncType)objc_msgSend;
        send(g_mfxScaler, @selector(encodeToCommandBuffer:sourceTexture:outputTexture:), cmd, src, dst);
        return;
    }
    // If no known selector exists, log once and skip
    static dispatch_once_t onceToken;
    dispatch_once(&onceToken, ^{
        NSLog(@"[MetalFX] No compatible encode selector found on this SDK");
    });
    (void)cmd; (void)src; (void)dst;
#else
    // MetalFX not available in this build; do nothing
    (void)cmdPtr; (void)colorInPtr; (void)outPtr;
#endif
}

extern "C" void SetMetalSurfaceScale(float scale)
{
    MetalOverride_SetDrawableScale(scale);
}

extern "C" bool Metal_ShouldSkipPresentOnce(void)
{
    if (!g_ipadSkipPresentOnce)
        return false;
    g_ipadSkipPresentOnce = false;
    return true;
}

