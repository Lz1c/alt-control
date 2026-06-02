// HierarchyOrganizer.cs
// Tidies the scene root by moving loose top-level objects into category group objects.
// Reparenting preserves world transforms (worldPositionStays = true), so nothing visually
// moves. Already-organised containers and system objects are left untouched.
//
// Tools > Vegetation > Organize Hierarchy

#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class HierarchyOrganizer
{
    // Roots that are already groups or are system objects -> leave alone.
    static readonly HashSet<string> Keep = new HashSet<string>
    {
        "Main Camera", "Directional Light", "Prototype_FPC", "PostProcessing",
        "original", "Monunts", "River", "ground", "another", "main",
        "Vegetation_Scatter", "Vegetation_Mound_Large", "Vegetation_GrassMounds",
        "Mountain_Stack_01", "Mountain_Stack_02", "Mountain_Stack_03", "Mountain_Stack_04",
        "Wall_Fence",
    };

    [MenuItem("Tools/Vegetation/Organize Hierarchy")]
    public static void Organize()
    {
        Scene scene = SceneManager.GetActiveScene();
        GameObject[] roots = scene.GetRootGameObjects(); // snapshot before creating groups

        var buckets = new Dictionary<string, List<GameObject>>();
        foreach (GameObject go in roots)
        {
            if (Keep.Contains(go.name)) continue;
            string cat = Categorize(go.name);
            if (cat == null) continue;
            if (!buckets.TryGetValue(cat, out var list)) { list = new List<GameObject>(); buckets[cat] = list; }
            list.Add(go);
        }

        int moved = 0;
        foreach (var kv in buckets)
        {
            // find or create the group root
            GameObject group = Find(scene, kv.Key);
            if (group == null)
            {
                group = new GameObject(kv.Key);
                Undo.RegisterCreatedObjectUndo(group, "Organize Hierarchy");
            }
            foreach (GameObject go in kv.Value)
            {
                Undo.SetTransformParent(go.transform, group.transform, "Organize Hierarchy");
                go.transform.SetParent(group.transform, true); // keep world transform
                moved++;
            }
            Debug.Log($"[Organize] {kv.Key}: {kv.Value.Count} objects");
        }

        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log($"[Organize] Done. Moved {moved} loose objects into {buckets.Count} groups. Roots now: {scene.GetRootGameObjects().Length}");
    }

    static string Categorize(string n)
    {
        if (n.StartsWith("rope_bridge")) return "Group_RopeBridges";
        if (n.StartsWith("SM_Prop_Fence")) return "Group_Fences";
        if (n.StartsWith("SM_Bld_")) return "Group_Buildings";
        if (n.StartsWith("SM_Terrain_Ground_Mound_Large_01")) return "Group_GrassTiles";
        if (n.StartsWith("SM_Terrain")) return "Group_GrassTiles";
        if (n.StartsWith("SM_Rock") || n.StartsWith("SM_Gen_Env")) return "Group_Rocks_Cliffs";
        if (n.StartsWith("Embers")) return "Group_VFX";
        if (n.Contains("树")) return "Group_Trees";
        return null; // unmatched -> leave at root
    }

    static GameObject Find(Scene scene, string name)
    {
        foreach (GameObject go in scene.GetRootGameObjects())
            if (go.name == name) return go;
        return null;
    }
}
#endif
