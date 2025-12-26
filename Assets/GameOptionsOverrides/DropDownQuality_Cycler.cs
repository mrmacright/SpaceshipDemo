using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace GameOptionsUtility
{
    public class DropDownQuality_Cycler : MonoBehaviour
    {
        public TMP_Text display;
        public Button leftButton;
        public Button rightButton;

        string[] qualityNames;
        int index;

        void OnEnable()
        {
            qualityNames = QualitySettings.names;

            index = GameOption.Get<GraphicOption>().quality;
            if (index < 0 || index >= qualityNames.Length)
                index = 0;

            // Apply immediately on enable so runtime matches saved value
            ApplyRuntime();

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
            index = (index - 1 + qualityNames.Length) % qualityNames.Length;
            ApplyRuntime();
        }

        public void Next()
        {
            index = (index + 1) % qualityNames.Length;
            ApplyRuntime();
        }

        void ApplyRuntime()
        {
            var go = GameOption.Get<GraphicOption>();
            go.quality = index;

            // APPLY GRAPHICS LEVEL AT RUNTIME
            QualitySettings.SetQualityLevel(index, true);

            UpdateLabel();

            Debug.Log($"[Graphics] Quality applied = {qualityNames[index]}");
        }

        void UpdateLabel()
        {
            if (display != null)
                display.text = qualityNames[index];
        }
    }
}
