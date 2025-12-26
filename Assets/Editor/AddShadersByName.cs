using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class AddShadersByName : EditorWindow
{
    private ShaderVariantCollection collection;
    private List<string> shaderNames = new List<string>();
    private Vector2 scroll;

    [MenuItem("Tools/Shader Variants/Add Shaders By Name")]
    static void Open()
    {
        GetWindow<AddShadersByName>("Add Shaders");
    }

    void OnGUI()
    {
        GUILayout.Label("ShaderVariantCollection", EditorStyles.boldLabel);
        collection = (ShaderVariantCollection)EditorGUILayout.ObjectField(collection, typeof(ShaderVariantCollection), false);

        GUILayout.Space(10);
        GUILayout.Label("Paste Shader Names (one per line)", EditorStyles.boldLabel);

        string input = "";
        if (shaderNames.Count > 0)
            input = string.Join("\n", shaderNames);

        string newInput = EditorGUILayout.TextArea(input, GUILayout.Height(200));

        if (newInput != input)
        {
            shaderNames = new List<string>(newInput.Split('\n'));
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Add All Shaders"))
        {
            AddAll();
        }
    }

    void AddAll()
    {
        if (collection == null)
        {
            Debug.LogError("No ShaderVariantCollection assigned!");
            return;
        }

        int added = 0;

        foreach (string name in shaderNames)
        {
            string trimmed = name.Trim();
            if (string.IsNullOrEmpty(trimmed))
                continue;

            Shader shader = Shader.Find(trimmed);

            if (shader == null)
            {
                Debug.LogWarning($"Shader NOT FOUND: {trimmed}");
                continue;
            }

            // Try all PassTypes safely without crashing Unity
            foreach (PassType pass in System.Enum.GetValues(typeof(PassType)))
            {
                try
                {
                    collection.Add(new ShaderVariantCollection.ShaderVariant(shader, pass, new string[0]));
                    added++;
                }
                catch
                {
                    // Ignore invalid pass types
                }
            }
        }

        Debug.Log($"Added approx {added} shader variants safely.");
    }
}
