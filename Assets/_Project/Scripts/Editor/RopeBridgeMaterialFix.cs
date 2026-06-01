// RopeBridgeMaterialFix.cs
// One-shot editor utility: the Rope_Bridge_Set asset ships with Built-in "Standard" shader
// materials, which render magenta under URP. This converts every Standard material under
// Assets/Rope_Bridge_Set to URP/Lit, carrying over albedo texture/color, normal map,
// metallic/smoothness, occlusion and emission.
//
// Tools > Fix > Rope Bridge Materials to URP

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class RopeBridgeMaterialFix
{
    const string SearchFolder = "Assets/Rope_Bridge_Set";

    [MenuItem("Tools/Fix/Rope Bridge Materials to URP")]
    public static void Convert()
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null)
        {
            Debug.LogError("[RopeBridgeFix] Could not find 'Universal Render Pipeline/Lit' shader.");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:Material", new[] { SearchFolder });
        int converted = 0;
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null || m.shader == null) continue;
            if (m.shader.name != "Standard" && m.shader.name != "Standard (Specular setup)")
                continue;

            // Capture Standard properties before swapping the shader.
            Texture albedo = m.HasProperty("_MainTex") ? m.GetTexture("_MainTex") : null;
            Vector2 albedoScale = albedo != null ? m.GetTextureScale("_MainTex") : Vector2.one;
            Vector2 albedoOffset = albedo != null ? m.GetTextureOffset("_MainTex") : Vector2.zero;
            Color baseColor = m.HasProperty("_Color") ? m.GetColor("_Color") : Color.white;
            Texture normal = m.HasProperty("_BumpMap") ? m.GetTexture("_BumpMap") : null;
            float bumpScale = m.HasProperty("_BumpScale") ? m.GetFloat("_BumpScale") : 1f;
            Texture metallicMap = m.HasProperty("_MetallicGlossMap") ? m.GetTexture("_MetallicGlossMap") : null;
            float metallic = m.HasProperty("_Metallic") ? m.GetFloat("_Metallic") : 0f;
            float smoothness = m.HasProperty("_Glossiness") ? m.GetFloat("_Glossiness") : 0.5f;
            Texture occlusion = m.HasProperty("_OcclusionMap") ? m.GetTexture("_OcclusionMap") : null;
            Texture emissionMap = m.HasProperty("_EmissionMap") ? m.GetTexture("_EmissionMap") : null;
            Color emissionColor = m.HasProperty("_EmissionColor") ? m.GetColor("_EmissionColor") : Color.black;

            m.shader = urpLit;

            // Albedo
            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", albedo);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", baseColor);
            if (albedo != null && m.HasProperty("_BaseMap"))
            {
                m.SetTextureScale("_BaseMap", albedoScale);
                m.SetTextureOffset("_BaseMap", albedoOffset);
            }

            // Normal map
            if (normal != null && m.HasProperty("_BumpMap"))
            {
                m.SetTexture("_BumpMap", normal);
                m.SetFloat("_BumpScale", bumpScale);
                m.EnableKeyword("_NORMALMAP");
            }

            // Metallic / smoothness
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            if (metallicMap != null && m.HasProperty("_MetallicGlossMap"))
            {
                m.SetTexture("_MetallicGlossMap", metallicMap);
                m.EnableKeyword("_METALLICSPECGLOSSMAP");
            }

            // Occlusion
            if (occlusion != null && m.HasProperty("_OcclusionMap"))
            {
                m.SetTexture("_OcclusionMap", occlusion);
                m.EnableKeyword("_OCCLUSIONMAP");
            }

            // Emission
            if (emissionColor.maxColorComponent > 0f || emissionMap != null)
            {
                if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", emissionColor);
                if (emissionMap != null && m.HasProperty("_EmissionMap")) m.SetTexture("_EmissionMap", emissionMap);
                m.EnableKeyword("_EMISSION");
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }

            EditorUtility.SetDirty(m);
            converted++;
            Debug.Log($"[RopeBridgeFix] Converted to URP/Lit: {path}  (albedo={(albedo != null ? albedo.name : "none")})");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[RopeBridgeFix] Done. Converted {converted} material(s) under {SearchFolder}.");
    }
}
#endif
