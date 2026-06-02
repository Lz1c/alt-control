// PolyperfectMaterialFix.cs
// The Poly Universal Pack (polyperfect) ships HDRP/Lit materials. In this URP project their
// shader is missing, so they render magenta. This converts every such material under
// Assets/polyperfect to URP/Lit, migrating the albedo / normal / height textures, base color,
// metallic, smoothness and emission. Serialized texture/float/color data is read via
// SerializedObject so it survives the missing (error) shader.
//
// Tools > Fix > Polyperfect Materials to URP

#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class PolyperfectMaterialFix
{
    const string SearchFolder = "Assets/polyperfect";

    [MenuItem("Tools/Fix/Polyperfect Materials to URP")]
    public static void Convert()
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null) { Debug.LogError("[PolyFix] URP/Lit shader not found."); return; }

        string[] guids = AssetDatabase.FindAssets("t:Material", new[] { SearchFolder });
        int converted = 0, skipped = 0;
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) continue;

            string sh = m.shader != null ? m.shader.name : "";
            bool needs = m.shader == null || sh == "Hidden/InternalErrorShader" ||
                                                  sh.StartsWith("HDRP/") || sh.Contains("HDRenderPipeline") || sh.StartsWith("Standard") || sh == "Autodesk Interactive";
            if (!needs) { skipped++; continue; }

            // --- read serialized data (works even with a broken/missing shader) ---
            var so = new SerializedObject(m);
            var texEnvs = so.FindProperty("m_SavedProperties.m_TexEnvs");
            var floats = so.FindProperty("m_SavedProperties.m_Floats");
            var colors = so.FindProperty("m_SavedProperties.m_Colors");

            var tex = new Dictionary<string, Texture>();
            if (texEnvs != null)
            {
                for (int i = 0; i < texEnvs.arraySize; i++)
                {
                    var el = texEnvs.GetArrayElementAtIndex(i);
                    string name = el.FindPropertyRelative("first").stringValue;
                    var tProp = el.FindPropertyRelative("second.m_Texture");
                    if (!string.IsNullOrEmpty(name) && tProp != null && tProp.objectReferenceValue is Texture t)
                        tex[name] = t;
                }
            }
            var fl = new Dictionary<string, float>();
            if (floats != null)
                for (int i = 0; i < floats.arraySize; i++)
                {
                    var el = floats.GetArrayElementAtIndex(i);
                    fl[el.FindPropertyRelative("first").stringValue] = el.FindPropertyRelative("second").floatValue;
                }
            var col = new Dictionary<string, Color>();
            if (colors != null)
                for (int i = 0; i < colors.arraySize; i++)
                {
                    var el = colors.GetArrayElementAtIndex(i);
                    col[el.FindPropertyRelative("first").stringValue] = el.FindPropertyRelative("second").colorValue;
                }

            Texture albedo = Pick(tex, "_BaseColorMap", "_MainTex", "_BaseMap");
            Texture normal = Pick(tex, "_NormalMap", "_BumpMap");
            Texture height = Pick(tex, "_HeightMap", "_ParallaxMap");
            Texture metalMap = Pick(tex, "_MetallicGlossMap", "_MaskMap");
            Texture emisMap = Pick(tex, "_EmissiveColorMap", "_EmissionMap");
            Color baseColor = PickC(col, "_BaseColor", "_Color");
            Color emisColor = PickC(col, "_EmissiveColor", "_EmissionColor");
            float metallic = PickF(fl, 0f, "_Metallic");
            float smooth = PickF(fl, 0.5f, "_Smoothness", "_Glossiness");
            float bumpScale = PickF(fl, 1f, "_BumpScale", "_NormalScale");

            // --- switch shader and write URP properties ---
            m.shader = urpLit;
            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", albedo);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", baseColor.a == 0 && baseColor.maxColorComponent == 0 ? Color.white : baseColor);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);

            if (normal != null && m.HasProperty("_BumpMap"))
            {
                m.SetTexture("_BumpMap", normal);
                if (m.HasProperty("_BumpScale")) m.SetFloat("_BumpScale", bumpScale <= 0f ? 1f : bumpScale);
                m.EnableKeyword("_NORMALMAP");
            }
            if (height != null && m.HasProperty("_ParallaxMap"))
            {
                m.SetTexture("_ParallaxMap", height);
                m.EnableKeyword("_PARALLAXMAP");
            }
            if (metalMap != null && m.HasProperty("_MetallicGlossMap"))
            {
                m.SetTexture("_MetallicGlossMap", metalMap);
                m.EnableKeyword("_METALLICSPECGLOSSMAP");
            }
            if (emisMap != null || emisColor.maxColorComponent > 0f)
            {
                if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", emisColor);
                if (emisMap != null && m.HasProperty("_EmissionMap")) m.SetTexture("_EmissionMap", emisMap);
                m.EnableKeyword("_EMISSION");
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }

            EditorUtility.SetDirty(m);
            converted++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[PolyFix] Converted {converted} HDRP material(s) to URP/Lit under {SearchFolder} (skipped {skipped} already-ok).");
    }

    static Texture Pick(Dictionary<string, Texture> d, params string[] keys)
    {
        foreach (var k in keys) if (d.TryGetValue(k, out var t) && t != null) return t;
        return null;
    }
    static Color PickC(Dictionary<string, Color> d, params string[] keys)
    {
        foreach (var k in keys) if (d.TryGetValue(k, out var c)) return c;
        return Color.white;
    }
    static float PickF(Dictionary<string, float> d, float def, params string[] keys)
    {
        foreach (var k in keys) if (d.TryGetValue(k, out var f)) return f;
        return def;
    }
}
#endif
