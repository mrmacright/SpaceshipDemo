using GameOptionsUtility;
using GameplayIngredients;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using System.Runtime.InteropServices;

namespace GameOptionsUtility
{
    public class SpaceshipOptions : GameOption
    {
#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void MetalOverride_SetDrawableScale(float scale);

        [DllImport("__Internal")]
        private static extern void MetalOverride_ReportUnityScreenSize(int w, int h);

        [DllImport("__Internal")]
        private static extern void MetalFX_SetMode(int mode);
#endif

        public class Preferences
        {
            public const string prefix = GameOptions.Preferences.prefix + "Spaceship.";
            public const string screenPercentage = prefix + "ScreenPercentage";
            public const string keyboardScheme = prefix + "FPSKeyboardScheme";
            public const string renderResolution = prefix + "RenderResolution";
            public const string metalFXMode = prefix + "MetalFXMode";
        }

        public enum FPSKeyboardScheme { WASD, IJKL, ZQSD }
        public enum RenderResolution { Full = 100, Medium = 75, Half = 50 }
        public enum MetalFXMode { Off = 0, Performance = 1, Balanced = 2, Quality = 3 }

        public FPSKeyboardScheme fpsKeyboardScheme
        {
            get => (FPSKeyboardScheme)PlayerPrefs.GetInt(Preferences.keyboardScheme, 0);
            set => PlayerPrefs.SetInt(Preferences.keyboardScheme, (int)value);
        }

        public RenderResolution renderResolution
        {
            get => (RenderResolution)PlayerPrefs.GetInt(Preferences.renderResolution, 100);
            set => PlayerPrefs.SetInt(Preferences.renderResolution, (int)value);
        }

        int m_ScreenPercentage = -1;
        public int screenPercentage
        {
            get => m_ScreenPercentage == -1
                ? (m_ScreenPercentage = PlayerPrefs.GetInt(Preferences.screenPercentage, 100))
                : m_ScreenPercentage;

            set
            {
                m_ScreenPercentage = value;
                PlayerPrefs.SetInt(Preferences.screenPercentage, value);
            }
        }

        public MetalFXMode metalFXMode
        {
            get => (MetalFXMode)PlayerPrefs.GetInt(Preferences.metalFXMode, 0);
            set => PlayerPrefs.SetInt(Preferences.metalFXMode, (int)value);
        }

        float lastScale = -1f;

        // Locked native resolution (prevents compounding scale)
        int baseWidth = -1;
        int baseHeight = -1;

        public override void Apply()
        {
            // Capture true native resolution once
            if (baseWidth < 0 || baseHeight < 0)
            {
                baseWidth = Screen.width;
                baseHeight = Screen.height;

                Debug.Log($"[SpaceshipOptions] Base resolution locked to {baseWidth}x{baseHeight}");
            }

            ApplyRenderScale();
            UpdateFPSControlScheme();

            // Apply MetalFX immediately if present
            var fx = Object.FindObjectOfType<MetalFXDropdown>();
            if (fx != null)
                fx.PushToOptions();
        }

        public class FPSKeys
        {
            public readonly KeyCode forward, left, back, right;

            public FPSKeys(KeyCode f, KeyCode l, KeyCode b, KeyCode r)
            {
                forward = f;
                left = l;
                back = b;
                right = r;
            }
        }

        public FPSKeys fpsKeys { get; private set; }

        void UpdateFPSControlScheme()
        {
            fpsKeys = fpsKeyboardScheme switch
            {
                FPSKeyboardScheme.WASD => new FPSKeys(KeyCode.W, KeyCode.A, KeyCode.S, KeyCode.D),
                FPSKeyboardScheme.IJKL => new FPSKeys(KeyCode.I, KeyCode.J, KeyCode.K, KeyCode.L),
                FPSKeyboardScheme.ZQSD => new FPSKeys(KeyCode.Z, KeyCode.Q, KeyCode.S, KeyCode.D),
                _ => fpsKeys
            };
        }

        void ReportUnityScreen()
        {
#if UNITY_IOS && !UNITY_EDITOR
            MetalOverride_ReportUnityScreenSize(Screen.width, Screen.height);
#endif
        }

        public void ApplyMetalFX()
        {
#if UNITY_IOS && !UNITY_EDITOR
            Debug.Log($"[MetalFX] Applying mode: {metalFXMode}");
            MetalFX_SetMode((int)metalFXMode);
#else
            Debug.Log($"[MetalFX] Editor mode set to: {metalFXMode} (native disabled)");
#endif
        }

        public void ApplyRenderScale()
        {
            float scale = (int)renderResolution / 100f;

            if (Mathf.Approximately(scale, lastScale))
                return;

            lastScale = scale;

            // ALWAYS scale from locked native resolution
            int targetW = Mathf.RoundToInt(baseWidth * scale);
            int targetH = Mathf.RoundToInt(baseHeight * scale);

#if UNITY_IOS && !UNITY_EDITOR
            MetalOverride_SetDrawableScale(scale);
            ReportUnityScreen();
#else
            Screen.SetResolution(targetW, targetH, true);
#endif

            Debug.Log($"[SpaceshipOptions] Render scale = {scale}, buffer = {targetW}x{targetH}");
        }
    }
}
