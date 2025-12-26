using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Rendering;
using TMPro;
using UnityEngine.VFX;
using System.Collections;
using System.Collections.Generic;

public class ShaderWarmupController : MonoBehaviour
{
    [Header("Required")]
    [Tooltip("Merged ShaderVariantCollection with all your variants")]
    public ShaderVariantCollection shaderVariants;

    public Slider progressBar;
    public TMP_Text statusLabel;
    public string nextSceneName = "MainMenu";

    [Header("HDRP / URP pipeline assets (optional)")]
    public RenderPipelineAsset[] pipelineAssetsToWarm;

    [Header("VFX Graph warmup (optional)")]
    [Tooltip("Prefabs or scene objects that contain VisualEffect components to warm.")]
    public GameObject[] vfxPrefabsOrObjects;

    [Header("Behaviour")]
    public bool onlyOnFirstRun = true;
    public float fakeProgressDuration = 4f;

    const string PrefKey = "ShadersWarmed";

    void Awake()
    {
        if (onlyOnFirstRun && PlayerPrefs.GetInt(PrefKey, 0) == 1)
        {
            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            DontDestroyOnLoad(gameObject);
        }
    }

    void Start()
    {
        if (!onlyOnFirstRun || PlayerPrefs.GetInt(PrefKey, 0) == 0)
        {
            StartCoroutine(WarmupRoutine());
        }
    }

    // Warm VFX under the CURRENT pipeline + CURRENT quality level.
    // Runs each VFX for two frames to trigger more shader/kernel compilation on iOS.
    IEnumerator WarmVFXOneCombo()
    {
        if (vfxPrefabsOrObjects == null || vfxPrefabsOrObjects.Length == 0)
            yield break;

        var tempRoot = new GameObject("VFXWarmupRoot");
        DontDestroyOnLoad(tempRoot);

        foreach (var go in vfxPrefabsOrObjects)
        {
            if (go == null) continue;

            GameObject instance = go;

            // If it's a prefab, instantiate it
            if (!instance.scene.IsValid())
                instance = Instantiate(go, tempRoot.transform);

            var vfxList = instance.GetComponentsInChildren<VisualEffect>(true);
            foreach (var vfx in vfxList)
            {
                if (vfx == null) continue;

                vfx.Reinit();
                vfx.Play();
            }

            // Let VFX run for TWO frames (important on iOS)
            yield return null;
            yield return null;

            // Clean up instantiated prefabs
            if (instance != null && instance != go)
                Destroy(instance);
        }

        Destroy(tempRoot);
    }

    IEnumerator WarmupRoutine()
    {
        if (progressBar != null) progressBar.value = 0f;
        if (statusLabel != null) statusLabel.text = "Preparing shaders. This may take a while...";

        yield return null;

        // Fake progress bar while the player stares at the loading screen
        float t = 0f;
        while (t < fakeProgressDuration)
        {
            t += Time.unscaledDeltaTime;
            float normalized = Mathf.Clamp01(t / fakeProgressDuration);
            if (progressBar != null)
                progressBar.value = normalized * 0.5f; // first half
            yield return null;
        }

        // 1) Warm pipeline + quality + ShaderVariantCollection + VFX
        if (statusLabel != null) statusLabel.text = "Prewarming materials and VFX...";

        if (shaderVariants != null)
        {
            var originalAsset = GraphicsSettings.renderPipelineAsset;
            int originalQuality = QualitySettings.GetQualityLevel();

            if (pipelineAssetsToWarm != null && pipelineAssetsToWarm.Length > 0)
            {
                foreach (var asset in pipelineAssetsToWarm)
                {
                    if (asset == null) continue;

                    // Switch pipeline (Low / High / Ultra)
                    GraphicsSettings.renderPipelineAsset = asset;

                    // Let HDRP settle/bind keywords
                    yield return null;

                    // Warm each quality level (Low / High / Ultra)
                    for (int q = 0; q < QualitySettings.names.Length; q++)
                    {
                        QualitySettings.SetQualityLevel(q, true);

                        // Let quality settle
                        yield return null;

                        // Warm shader variants under this pipeline + quality
                        shaderVariants.WarmUp();

                        // Warm VFX under this pipeline + quality
                        yield return StartCoroutine(WarmVFXOneCombo());
                    }
                }

                // Restore pipeline + quality
                GraphicsSettings.renderPipelineAsset = originalAsset;
                QualitySettings.SetQualityLevel(originalQuality, true);
                yield return null;
            }
            else
            {
                // No pipeline list provided: just warm current pipeline across all quality levels
                int originalQ = QualitySettings.GetQualityLevel();

                for (int q = 0; q < QualitySettings.names.Length; q++)
                {
                    QualitySettings.SetQualityLevel(q, true);
                    yield return null;

                    shaderVariants.WarmUp();
                    yield return StartCoroutine(WarmVFXOneCombo());
                }

                QualitySettings.SetQualityLevel(originalQ, true);
                yield return null;
            }
        }

        if (progressBar != null) progressBar.value = 1f;
        if (statusLabel != null) statusLabel.text = "Done";

        if (onlyOnFirstRun)
        {
            PlayerPrefs.SetInt(PrefKey, 1);
            PlayerPrefs.Save();
        }

        yield return new WaitForSecondsRealtime(0.25f);

        SceneManager.LoadScene(nextSceneName);
    }
}
