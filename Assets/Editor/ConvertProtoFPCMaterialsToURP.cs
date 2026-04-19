using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class ConvertProtoFPCMaterialsToURP
{
    [MenuItem("Tools/Convert Proto_FPC Materials to URP")]
    public static void Run()
    {
        const string searchRoot = "Assets/Proto_FPC/FPC_Resources/Materials";
        var urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null)
        {
            Debug.LogError("Universal Render Pipeline/Lit shader not found.");
            return;
        }

        var guids = AssetDatabase.FindAssets("t:Material", new[] { searchRoot });
        int converted = 0, skipped = 0;
        var report = new List<string>();

        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) continue;

            if (mat.shader == null || mat.shader.name != "Standard")
            {
                skipped++;
                continue;
            }

            Texture mainTex = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : null;
            Vector2 scale = mat.HasProperty("_MainTex") ? mat.GetTextureScale("_MainTex") : Vector2.one;
            Vector2 offset = mat.HasProperty("_MainTex") ? mat.GetTextureOffset("_MainTex") : Vector2.zero;
            Color color = mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white;
            Color emission = mat.HasProperty("_EmissionColor") ? mat.GetColor("_EmissionColor") : Color.black;
            float metallic = mat.HasProperty("_Metallic") ? mat.GetFloat("_Metallic") : 0f;
            float smoothness = mat.HasProperty("_Glossiness") ? mat.GetFloat("_Glossiness") : 0.5f;
            Texture normal = mat.HasProperty("_BumpMap") ? mat.GetTexture("_BumpMap") : null;
            float bumpScale = mat.HasProperty("_BumpScale") ? mat.GetFloat("_BumpScale") : 1f;

            Undo.RecordObject(mat, "Convert to URP/Lit");
            mat.shader = urpLit;

            if (mat.HasProperty("_BaseMap"))
            {
                mat.SetTexture("_BaseMap", mainTex);
                mat.SetTextureScale("_BaseMap", scale);
                mat.SetTextureOffset("_BaseMap", offset);
            }
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", emission);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            if (normal != null && mat.HasProperty("_BumpMap"))
            {
                mat.SetTexture("_BumpMap", normal);
                mat.SetFloat("_BumpScale", bumpScale);
                mat.EnableKeyword("_NORMALMAP");
            }
            if (emission.maxColorComponent > 0f)
                mat.EnableKeyword("_EMISSION");

            EditorUtility.SetDirty(mat);
            report.Add($"  OK  {path}");
            converted++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[URP Convert] Converted: {converted}, Skipped: {skipped}\n" + string.Join("\n", report));
    }
}
