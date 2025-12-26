using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Rendering.HighDefinition;

namespace GameOptionsUtility
{
    public class HDRPAntiAliasing_Cycler : MonoBehaviour
    {
        public TMP_Text display;
        public Button leftButton;
        public Button rightButton;

        // Correct HDRP AA order
        readonly string[] Labels = { "None", "FXAA", "TAA", "SMAA" };

        readonly HDAdditionalCameraData.AntialiasingMode[] Modes =
        {
            HDAdditionalCameraData.AntialiasingMode.None,
            HDAdditionalCameraData.AntialiasingMode.FastApproximateAntialiasing,     // FXAA
            HDAdditionalCameraData.AntialiasingMode.TemporalAntialiasing,             // TAA
            HDAdditionalCameraData.AntialiasingMode.SubpixelMorphologicalAntiAliasing // SMAA
        };

        int index;

        void OnEnable()
        {
            var option = GameOption.Get<HDRPAntiAliasingOption>();
            var current = option.antiAliasing;

            // Find matching index
            index = System.Array.IndexOf(Modes, current);
            if (index < 0) index = 0;

            UpdateLabel();

            leftButton.onClick.AddListener(Prev);
            rightButton.onClick.AddListener(Next);
        }

        void OnDisable()
        {
            leftButton.onClick.RemoveListener(Prev);
            rightButton.onClick.RemoveListener(Next);
        }

        public void Prev()
        {
            index = (index - 1 + Modes.Length) % Modes.Length;
            Apply();
        }

        public void Next()
        {
            index = (index + 1) % Modes.Length;
            Apply();
        }

        void Apply()
        {
            var option = GameOption.Get<HDRPAntiAliasingOption>();
            option.antiAliasing = Modes[index];
            option.Apply();

            UpdateLabel();
        }

        void UpdateLabel()
        {
            display.text = Labels[index];
        }
    }
}
