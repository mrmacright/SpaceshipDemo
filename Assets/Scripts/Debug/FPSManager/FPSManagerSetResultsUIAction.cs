using GameplayIngredients;
using GameplayIngredients.Actions;
using UnityEngine;
using TMPro;
using GameOptionsUtility;
using UnityEngine.Rendering.HighDefinition;

public class FPSManagerSetResultsUIAction : ActionBase
{
    public TextMeshProUGUI Percentile5Value;
    public TextMeshProUGUI Percentile95Value;

    public TextMeshProUGUI QualityText;
    public TextMeshProUGUI OverallFPSText;
    public TextMeshProUGUI OverallMSText;
    public TextMeshProUGUI OverallMSPerMPixText;

    public TextMeshProUGUI WorstFPSText;
    public TextMeshProUGUI WorstMSText;
    public TextMeshProUGUI BestFPSText;
    public TextMeshProUGUI BestMSText;

    public TextMeshProUGUI CPUInfo;
    public TextMeshProUGUI GPUInfo;
    public TextMeshProUGUI RAMInfo;

    public override void Execute(GameObject instigator = null)
    {
        // --- Force selected AA onto the active (benchmark) camera ---
        var cam = Camera.main;
        var hd = cam ? cam.GetComponent<HDAdditionalCameraData>() : null;

        var aaOpt = GameOption.Get<HDRPAntiAliasingOption>();
        if (hd != null && aaOpt != null)
        {
            hd.antialiasing = aaOpt.antiAliasing;
        }

        // --- Normal benchmark logic below ---
        GraphicOption go = GameOption.Get<GraphicOption>();
        SpaceshipOptions o = GameOption.Get<SpaceshipOptions>();
        FPSManager fpsm = Manager.Get<FPSManager>();

        // Scaling label only (no resolution)
        int rr = (int)o.renderResolution;
        float scale = rr / 100f;

        string scaleLabel = rr switch
        {
            50 => "50% Scaling",
            75 => "75% Scaling",
            100 => "100% Scaling",
            _ => $"{rr}% Scaling"
        };

        // Anti-aliasing label (from selected option, not camera defaults)
        string aaLabel = aaOpt.antiAliasing switch
        {
            HDAdditionalCameraData.AntialiasingMode.None => "None",
            HDAdditionalCameraData.AntialiasingMode.FastApproximateAntialiasing => "FXAA",
            HDAdditionalCameraData.AntialiasingMode.SubpixelMorphologicalAntiAliasing => "SMAA",
            HDAdditionalCameraData.AntialiasingMode.TemporalAntialiasing => "TAA",
            _ => "Unknown"
        };

        QualityText.text =
            $"{scaleLabel} - {QualitySettings.names[QualitySettings.GetQualityLevel()]} Quality | AA - {aaLabel}";

        // FPS & frame time
        float avgMs   = fpsm.results.avgMs;
        float worstMs = fpsm.results.maxMs;
        float bestMs  = fpsm.results.minMs;

        OverallFPSText.text = (1000f / avgMs).ToString("F1", System.Globalization.CultureInfo.InvariantCulture);
        OverallMSText.text  = avgMs.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);

        // 95th percentile
        Percentile95Value.text =
            $"{(1000f / fpsm.results.p95Ms):F1} FPS / {fpsm.results.p95Ms:F2} ms";

        // 5th percentile
        Percentile5Value.text =
            $"{(1000f / fpsm.results.p5Ms):F1} FPS / {fpsm.results.p5Ms:F2} ms";

        // Worst & Best
        WorstFPSText.text = (1000f / worstMs).ToString("F1", System.Globalization.CultureInfo.InvariantCulture);
        WorstMSText.text  = worstMs.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);

        BestFPSText.text = (1000f / bestMs).ToString("F1", System.Globalization.CultureInfo.InvariantCulture);
        BestMSText.text  = bestMs.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);

        // ms per Mpix (derived internally, not shown as resolution)
        float mpix = (go.width * scale * go.height * scale) / 1_000_000f;

        if (mpix > 0f)
        {
            float msPerMpix = avgMs / mpix;
            OverallMSPerMPixText.text =
                msPerMpix.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
        }
        else
        {
            OverallMSPerMPixText.text = "N/A";
        }

        // Hardware info
        CPUInfo.text =
            $"{SystemInfo.processorType} ({SystemInfo.processorCount} threads) @ {(SystemInfo.processorFrequency / 1000f).ToString("F2")} GHz.";
        RAMInfo.text = $"Usable System Memory : {SystemInfo.systemMemorySize / 1000} GB";
        GPUInfo.text = SystemInfo.graphicsDeviceName;
    }
}
