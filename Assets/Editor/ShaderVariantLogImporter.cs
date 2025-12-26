using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class ShaderVariantLogImporter : EditorWindow
{
    [Header("Inputs")]
    public TextAsset shaderLog;                       // shaderlog.txt
    public ShaderVariantCollection variantCollection; // NewShaderVariants

    [Header("Options")]
    public bool treatAllAsSRP = true;  // good for HDRP/URP projects

    private Vector2 _scroll;

    [MenuItem("Tools/Shader/Import Variants From Log")]
    private static void ShowWindow()
    {
        var wnd = GetWindow<ShaderVariantLogImporter>();
        wnd.titleContent = new GUIContent("Shader Variant Log Importer");
        wnd.Show();
    }

    private void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        EditorGUILayout.LabelField("Shader Variant Importer from Player Log", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        shaderLog = (TextAsset)EditorGUILayout.ObjectField("Shader log (txt)", shaderLog, typeof(TextAsset), false);
        variantCollection = (ShaderVariantCollection)EditorGUILayout.ObjectField("Variant Collection", variantCollection, typeof(ShaderVariantCollection), false);

        treatAllAsSRP = EditorGUILayout.Toggle("Treat all as SRP (HDRP/URP)", treatAllAsSRP);

        EditorGUILayout.Space();

        GUI.enabled = shaderLog != null && variantCollection != null;
        if (GUILayout.Button("Parse Log And Add Variants"))
        {
            ParseAndAdd();
        }
        GUI.enabled = true;

        EditorGUILayout.EndScrollView();
    }

    private struct VariantKey : IEquatable<VariantKey>
    {
        public string shaderName;
        public PassType passType;
        public string[] keywords;

        public bool Equals(VariantKey other)
        {
            if (shaderName != other.shaderName || passType != other.passType) return false;
            if (keywords.Length != other.keywords.Length) return false;
            for (int i = 0; i < keywords.Length; i++)
                if (keywords[i] != other.keywords[i])
                    return false;
            return true;
        }

        public override bool Equals(object obj) => obj is VariantKey other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = shaderName != null ? shaderName.GetHashCode() : 0;
                hash = (hash * 397) ^ (int)passType;
                for (int i = 0; i < keywords.Length; i++)
                    hash = (hash * 397) ^ keywords[i].GetHashCode();
                return hash;
            }
        }
    }

    private static readonly Regex LineRegex = new Regex(
        @"Uploaded shader variant to the GPU driver:\s*(.+?)\s*\(instance.*?\),\s*pass:\s*(.+?),\s*keywords\s*(.+?),\s*time:",
        RegexOptions.Compiled);

    private void ParseAndAdd()
    {
        string path = AssetDatabase.GetAssetPath(shaderLog);
        string raw = File.ReadAllText(path);

        var keys = new HashSet<VariantKey>();
        int addedCount = 0;
        int missingShaders = 0;

        using (var reader = new StringReader(raw))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                var match = LineRegex.Match(line);
                if (!match.Success)
                    continue;

                string shaderName = match.Groups[1].Value.Trim();
                string passNameRaw = match.Groups[2].Value.Trim();
                string keywordRaw = match.Groups[3].Value.Trim();

                // Strip <no keywords>
                string[] keywords = Array.Empty<string>();
                if (!keywordRaw.Contains("<no keywords>"))
                {
                    keywords = keywordRaw.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    Array.Sort(keywords, StringComparer.Ordinal);
                }

                Shader shader = Shader.Find(shaderName);
                if (shader == null)
                {
                    missingShaders++;
                    continue;
                }

                PassType passType = GuessPassType(shaderName, passNameRaw);

                var key = new VariantKey
                {
                    shaderName = shaderName,
                    passType = passType,
                    keywords = keywords
                };

                if (keys.Contains(key))
                    continue;

                keys.Add(key);

                try
                {
                    var variant = new ShaderVariantCollection.ShaderVariant(shader, passType, keywords);
                    variantCollection.Add(variant);
                    addedCount++;
                }
                catch (Exception ex)
                {  
                    Debug.LogWarning($"[ShaderVariantImporter] SKIPPED shader '{shaderName}' passType {passType} because Unity rejected it.\nReason: {ex.Message}");
                    continue;
                }

            }
        }

        EditorUtility.SetDirty(variantCollection);
        AssetDatabase.SaveAssets();

        Debug.Log($"[ShaderVariantLogImporter] Added {addedCount} new variants to '{variantCollection.name}'. " +
                  $"Skipped {missingShaders} lines due to missing shaders or parse issues. Unique variants seen: {keys.Count}");
    }

    private PassType GuessPassType(string shaderName, string passNameRaw)
{
    string pass = passNameRaw.Trim();

    // 1. Shadow caster
    if (pass.IndexOf("ShadowCaster", StringComparison.OrdinalIgnoreCase) >= 0)
        return PassType.ShadowCaster;

    // 2. Depth-related passes (HDRP/URP)
    if (shaderName.StartsWith("HDRP/", StringComparison.OrdinalIgnoreCase) ||
        shaderName.StartsWith("Hidden/HDRP", StringComparison.OrdinalIgnoreCase) ||
        shaderName.StartsWith("Universal Render Pipeline", StringComparison.OrdinalIgnoreCase) ||
        shaderName.StartsWith("Hidden/Universal", StringComparison.OrdinalIgnoreCase))
    {
        return PassType.ScriptableRenderPipeline;
    }

    // 3. VFX Graph auto-generated shaders (all SRP)
    if (shaderName.StartsWith("Hidden/VFX/", StringComparison.OrdinalIgnoreCase))
        return PassType.ScriptableRenderPipeline;

    // 4. Shader Graphs (ForwardOnly, DepthOnly, etc)
    if (shaderName.StartsWith("Shader Graphs/", StringComparison.OrdinalIgnoreCase))
        return PassType.ScriptableRenderPipeline;

    // 5. TextMeshPro
    if (shaderName.StartsWith("TextMeshPro/", StringComparison.OrdinalIgnoreCase))
        return PassType.Normal;

    // 6. Unity UI + UIR
    if (shaderName.StartsWith("UI/", StringComparison.OrdinalIgnoreCase) ||
        shaderName.StartsWith("Hidden/Internal-UIR", StringComparison.OrdinalIgnoreCase))
        return PassType.Normal;

    // 7. Generic Unity built-in shaders (Hidden/BlitCopy, GUITexture, UIEffects, etc)
    if (shaderName.StartsWith("Hidden/", StringComparison.OrdinalIgnoreCase))
        return PassType.Normal;

    // 8. Fallback
    return PassType.Normal;
}

}
