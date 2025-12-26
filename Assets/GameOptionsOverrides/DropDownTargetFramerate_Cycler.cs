using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace GameOptionsUtility
{
    public class FPS_Cycler : MonoBehaviour
    {
        public TMP_Text display;
        public Button leftButton;
        public Button rightButton;

        // Allowed FPS values
        private readonly int[] values = { 30, 60, 120 };
        private int index = 0;

        void OnEnable()
        {
            // Load the saved FPS
            int saved = GameOption.Get<GraphicOption>().targetFrameRate;

            // Find closest match (fallback to 60 if missing)
            index = System.Array.IndexOf(values, saved);
            if (index < 0)
                index = System.Array.IndexOf(values, 60);

            if (index < 0)
                index = 0;

            ApplyRuntime(); // ensure runtime FPS is correct on enable

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
            index = (index - 1 + values.Length) % values.Length;
            ApplyRuntime();
        }

        public void Next()
        {
            index = (index + 1) % values.Length;
            ApplyRuntime();
        }

        void ApplyRuntime()
        {
            int fps = values[index];

            // Store setting
            var go = GameOption.Get<GraphicOption>();
            go.targetFrameRate = fps;

            // APPLY AT RUNTIME (this was missing)
            Application.targetFrameRate = fps;

            UpdateLabel();

            Debug.Log($"[FPS] targetFrameRate applied = {fps}");
        }

        void UpdateLabel()
        {
            if (display != null)
                display.text = values[index].ToString();
        }
    }
}
