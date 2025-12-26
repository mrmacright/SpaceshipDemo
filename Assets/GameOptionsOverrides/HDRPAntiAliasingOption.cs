using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace GameOptionsUtility
{
    public class HDRPAntiAliasingOption : GameOption
    {
        const string PREF_KEY = "HDRP_AA_Mode";

        public HDAdditionalCameraData.AntialiasingMode antiAliasing
        {
            get => (HDAdditionalCameraData.AntialiasingMode)
                PlayerPrefs.GetInt(PREF_KEY, (int)HDAdditionalCameraData.AntialiasingMode.None);

            set => PlayerPrefs.SetInt(PREF_KEY, (int)value);
        }

        public override void Apply()
        {
            if (!GameplayIngredients.Manager.TryGet<GameplayIngredients.VirtualCameraManager>(out var vcm))
                return;

            var data = vcm.GetComponent<HDAdditionalCameraData>();
            if (data == null)
                return;

            data.antialiasing = antiAliasing;

            Debug.Log("[HDRP AA] Applied: " + antiAliasing);
        }

    }
}
