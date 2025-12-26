using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace GameOptionsUtility
{
    public class DropDownRenderResolution : MonoBehaviour
    {
        static readonly int[] Values = { 100, 75, 50 };
        static readonly string[] Labels =
        {
            "100%",
            "75%",
            "50%"
        };

        public TMP_Text display;
        public Button leftButton;
        public Button rightButton;

        int index;

        void OnEnable()
        {
            var opts = GameOption.Get<SpaceshipOptions>();
            int currentPercent = (int)opts.renderResolution;

            index = System.Array.IndexOf(Values, currentPercent);
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
            index = (index - 1 + Values.Length) % Values.Length;
            Apply();
        }

        public void Next()
        {
            index = (index + 1) % Values.Length;
            Apply();
        }

        void Apply()
        {
            var opts = GameOption.Get<SpaceshipOptions>();
            opts.renderResolution =
                (SpaceshipOptions.RenderResolution)Values[index];

            UpdateLabel();

            opts.ApplyRenderScale();
        }


        void UpdateLabel()
        {
            if (display != null)
                display.text = Labels[index];
        }
    }
}
