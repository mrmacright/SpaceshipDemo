using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class AddConsoleShadersToCollection : EditorWindow
{
    // IMPORTANT: use the project-relative path, not the full /Users/... path
    // This should match where your NewShaderVariants.shadervariants actually is.
    const string CollectionPath = "Assets/Scenes/ShaderWarmup/NewShaderVariants.shadervariants";

    // 1) Put your full shader name list here.
    //    For now I’ve filled in the ones I can see from your log.
    //    After this script, I’ll explain how to paste the entire column from Xcode.
    static readonly string[] ShaderNames = new string[]
    {
        "ARUI-Opaque",
        "ARUI-Outline",
        "ARUI/ARUI-CylinderScan",
        "ARUI/ARUI-Wire",
        "Console/UIGradient",
        "CustomRenderTexture/Rotate Cube",
        "CustomRenderTexture/Scroll Texture 2D",
        "CustomRenderTexture/Scroll Texture 3D",
        "Fake Volumetrics/GlowSphereMKII",
        "HDRP/Decal",
        "HDRP/DefaultFogVolume",
        "HDRP/Lit",
        "HDRP/Unlit",
        "Hidden/BlitCopy",
        "Hidden/ColorPyramidPS",
        "Hidden/Core/FallbackError",
        // ******* paste the rest of your shader names below this line *******
        // e.g.
        // "Monitor",
        // "Shader Graphs/ARUI-Sun",
        // "Shader Graphs/ARUI-Textured",
        // "Shader Graphs/FakeSky",
        // "Shader Graphs/HoloTable-MultiParallax",
        // "Shader Graphs/Outliner-Fill",
        // "Shader Graphs/Outliner-Stroke",
        // "Skybox/Cubemap",
        // "Sprites/Default",
        // "TextMeshPro/Distance Field",
        // "TextMeshPro/Mobile/Distance Field",
        // "UI/Default",
        // "UI/Loading",
        // "VFX Demo/CoreRoomGlass",
        // "VFX/BurningFuelDecal",
    };

    [MenuItem("Tools/Shader Warmup/Add Console Shaders To Collection")]
    static void ShowWindow()
    {
        GetWindow<AddConsoleShadersToCollection>("Add Console Shaders");
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("Target collection:", CollectionPath);

        if (GUILayout.Button("Add all listed shaders to collection"))
        {
            AddAll();
        }
    }

    static void AddAll()
    {
        var collection = AssetDatabase.LoadAssetAtPath<ShaderVariantCollection>(CollectionPath);
        if (collection == null)
        {
            Debug.LogError("Could not load ShaderVariantCollection at: " + CollectionPath);
            return;
        }

        int shadersFound = 0;
        int variantsAdded = 0;

        foreach (var shaderName in ShaderNames)
        {
            if (string.IsNullOrWhiteSpace(shaderName))
                continue;

            var shader = Shader.Find(shaderName);

            if (shader == null)
            {
                Debug.LogWarning($"Shader not found: '{shaderName}'");
                continue;
            }

            shadersFound++;

            // Try every PassType; skip anything that throws (like your EasyHDRP/Simple Texture Fade issue)
            foreach (PassType pass in Enum.GetValues(typeof(PassType)))
            {
                try
                {
                    var variant = new ShaderVariantCollection.ShaderVariant(shader, pass, Array.Empty<string>());
                    if (!collection.Contains(variant))
                    {
                        collection.Add(variant);
                        variantsAdded++;
                    }
                }
                catch (ArgumentException)
                {
                    // This shader doesn’t support this pass type – ignore it.
                }
            }
        }

        EditorUtility.SetDirty(collection);
        AssetDatabase.SaveAssets();

        Debug.Log($"[AddConsoleShadersToCollection] Added {variantsAdded} variants for {shadersFound} shaders into '{CollectionPath}'.");
    }
}
