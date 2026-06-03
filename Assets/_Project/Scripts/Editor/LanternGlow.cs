// LanternGlow.cs
// Adds a small emissive "flame core" sphere inside every Lantern_01* in the scene so the
// lantern reads as lit (the glow shows through the glass), without touching the shared
// FantasyVillage_MAIN atlas material. Run again to refresh; "Clear" removes the cores.
//
// Tools > Lighting > Add Lantern Glow Cores
// Tools > Lighting > Clear Lantern Glow Cores

#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class LanternGlow
{
    const string LanternPrefix = "Lantern_01";
    const string CoreName = "GlowCore";
    const string MatPath = "Assets/_Project/Materials/LanternGlow.mat";
    static readonly Color GlowColor = new Color(1f, 0.62f, 0.25f);  // warm orange
    const float EmissionIntensity = 6f;   // HDR multiplier (bright, bloom-friendly)
    const float WorldDiameter = 0.32f;    // size of the glow core in meters

    [MenuItem("Tools/Lighting/Add Lantern Glow Cores")]
    public static void AddGlow()
    {
        Scene scene = SceneManager.GetActiveScene();
        Material glowMat = GetOrCreateMaterial();
        if (glowMat == null) return;

        int added = 0;
        foreach (GameObject lantern in FindLanterns(scene))
        {
            // refresh: remove old core first
            Transform old = lantern.transform.Find(CoreName);
            if (old != null) Undo.DestroyObjectImmediate(old.gameObject);

            var rends = lantern.GetComponentsInChildren<Renderer>();
            if (rends.Length == 0) continue;
            Bounds b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);

            GameObject core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            core.name = CoreName;
            Object.DestroyImmediate(core.GetComponent<Collider>());
            core.GetComponent<MeshRenderer>().sharedMaterial = glowMat;
            core.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Undo.RegisterCreatedObjectUndo(core, "Add Lantern Glow");

            core.transform.SetParent(lantern.transform, true);
            core.transform.position = b.center;                       // centre of the lantern body
            Vector3 ls = lantern.transform.lossyScale;
            float sx = WorldDiameter / Mathf.Max(1e-4f, Mathf.Abs(ls.x));
            float sy = WorldDiameter / Mathf.Max(1e-4f, Mathf.Abs(ls.y));
            float sz = WorldDiameter / Mathf.Max(1e-4f, Mathf.Abs(ls.z));
            core.transform.localScale = new Vector3(sx, sy, sz);
            core.transform.localRotation = Quaternion.identity;
            added++;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log($"[LanternGlow] Added glow cores to {added} lantern(s).");
    }

    static Material GetOrCreateMaterial()
    {
        Material m = AssetDatabase.LoadAssetAtPath<Material>(MatPath);
        if (m != null) return m;
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null) { Debug.LogError("[LanternGlow] URP/Lit shader not found."); return null; }
        m = new Material(urpLit);
        m.SetColor("_BaseColor", new Color(1f, 0.85f, 0.6f));
        m.EnableKeyword("_EMISSION");
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        m.SetColor("_EmissionColor", GlowColor * EmissionIntensity);
        string dir = "Assets/_Project/Materials";
        if (!AssetDatabase.IsValidFolder(dir)) AssetDatabase.CreateFolder("Assets/_Project", "Materials");
        AssetDatabase.CreateAsset(m, MatPath);
        AssetDatabase.SaveAssets();
        return m;
    }

    [MenuItem("Tools/Lighting/Clear Lantern Glow Cores")]
    public static void ClearGlow()
    {
        Scene scene = SceneManager.GetActiveScene();
        int removed = 0;
        foreach (GameObject lantern in FindLanterns(scene))
        {
            Transform old = lantern.transform.Find(CoreName);
            if (old != null) { Undo.DestroyObjectImmediate(old.gameObject); removed++; }
        }
        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log($"[LanternGlow] Removed {removed} glow core(s).");
    }

    static List<GameObject> FindLanterns(Scene scene)
    {
        var list = new List<GameObject>();
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t.name.StartsWith(LanternPrefix)) list.Add(t.gameObject);
        return list;
    }
}
#endif
