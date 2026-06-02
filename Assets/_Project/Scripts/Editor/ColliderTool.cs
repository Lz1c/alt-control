// ColliderTool.cs
// Adds missing colliders to the scene with a sensible policy:
//  - Structural meshes (bridges, cliffs, rocks, props...) -> MeshCollider (skips _LOD_ meshes)
//  - Trees -> ONE trunk CapsuleCollider per tree, at the correct hierarchy depth:
//      * Group_Trees            : each direct child (worldtrees)
//      * DeadTrees_Mountains    : each tree instance (root/<mountain>/<tree>)
//      * another                : each direct child whose name is a tree
//  - Skips foliage, water surfaces, particle VFX and the Vegetation_* roots.
// The tree pass first clears any CapsuleColliders already on those tree subtrees, so it is
// idempotent / self-healing.
//
// Tools > Colliders > Add Missing Colliders
// Tools > Colliders > Audit Missing Colliders

#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ColliderTool
{
    static readonly string[] Foliage = {
        "Plant", "Grass", "Fern", "Flower", "Bush", "Hedge", "Undergrowth", "Mushroom",
        "Reed", "Lillypad", "PalmBush", "Leaves", "Leaf", "Vine", "Ivy"
    };
    static readonly string[] Water = { "River_Plane", "WaterFall", "Water" };
    static readonly string[] SkipRoots = {
        "Vegetation_Scatter", "Vegetation_Mound_Large", "Vegetation_GrassMounds",
        "Group_VFX", "PostProcessing", "original", "Group_Trees", "DeadTrees_Mountains"
    };

    static bool Has(string n, string[] keys) { foreach (var k in keys) if (n.Contains(k)) return true; return false; }
    static bool IsTree(string n) { return n.Contains("Tree") || n.Contains("树"); }
    static string RootName(Transform t) { while (t.parent != null) t = t.parent; return t.name; }

    [MenuItem("Tools/Colliders/Add Missing Colliders")]
    public static void AddMissing()
    {
        Scene scene = SceneManager.GetActiveScene();

        // ---- 1) structural mesh colliders ----
        int mesh = 0;
        foreach (GameObject go in AllActive(scene))
        {
            if (go.GetComponent<MeshRenderer>() == null) continue;
            if (go.GetComponent<Collider>() != null) continue;
            if (go.GetComponent<ParticleSystem>() != null) continue;
            var mf = go.GetComponent<MeshFilter>(); if (mf == null || mf.sharedMesh == null) continue;
            string n = go.name;
            if (n.Contains("_LOD_")) continue;
            if (IsTree(n)) continue;                       // trees handled separately
            if (Has(n, Foliage) || Has(n, Water)) continue;
            if (System.Array.IndexOf(SkipRoots, RootName(go.transform)) >= 0) continue;
            Undo.AddComponent<MeshCollider>(go);
            mesh++;
        }

        // ---- 2) collect exact tree roots ----
        var trees = new List<GameObject>();
        GameObject gTrees = Find(scene, "Group_Trees");
        if (gTrees != null) foreach (Transform c in gTrees.transform) trees.Add(c.gameObject);
        GameObject gDead = Find(scene, "DeadTrees_Mountains");
        if (gDead != null) foreach (Transform mtn in gDead.transform) foreach (Transform tr in mtn) trees.Add(tr.gameObject);
        GameObject gAnother = Find(scene, "another");
        if (gAnother != null) foreach (Transform c in gAnother.transform) if (IsTree(c.name)) trees.Add(c.gameObject);

        // ---- 3) clear stray capsules on those subtrees, then add one trunk capsule each ----
        int caps = 0;
        foreach (GameObject tree in trees)
        {
            foreach (var cc in tree.GetComponentsInChildren<CapsuleCollider>(true))
                Undo.DestroyObjectImmediate(cc);
            if (AddTrunkCapsule(tree)) caps++;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log($"[ColliderTool] Added {mesh} MeshColliders (structural) + {caps} trunk CapsuleColliders ({trees.Count} trees).");
    }

    static bool AddTrunkCapsule(GameObject root)
    {
        var rends = root.GetComponentsInChildren<Renderer>();
        if (rends.Length == 0) return false;
        Bounds b = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);

        Vector3 ls = root.transform.lossyScale;
        float lx = Mathf.Max(1e-4f, Mathf.Abs(ls.x));
        float ly = Mathf.Max(1e-4f, Mathf.Abs(ls.y));
        float lz = Mathf.Max(1e-4f, Mathf.Abs(ls.z));

        var cap = Undo.AddComponent<CapsuleCollider>(root);
        cap.direction = 1; // Y
        cap.center = root.transform.InverseTransformPoint(b.center);
        cap.height = b.size.y / ly;
        cap.radius = (Mathf.Min(b.size.x, b.size.z) * 0.18f) / Mathf.Max(lx, lz);
        return true;
    }

    [MenuItem("Tools/Colliders/Audit Missing Colliders")]
    public static void Audit()
    {
        Scene scene = SceneManager.GetActiveScene();
        int missing = 0; var byRoot = new Dictionary<string, int>();
        foreach (GameObject go in AllActive(scene))
        {
            if (go.GetComponent<MeshRenderer>() == null) continue;
            if (go.GetComponent<Collider>() != null) continue;
            string root = RootName(go.transform);
            byRoot.TryGetValue(root, out int c); byRoot[root] = c + 1; missing++;
        }
        Debug.Log($"[ColliderTool] {missing} renderer objects still without a collider.");
        foreach (var kv in byRoot) Debug.Log($"[ColliderTool]   {kv.Key}: {kv.Value}");
    }

    static GameObject Find(Scene scene, string name)
    {
        foreach (GameObject go in scene.GetRootGameObjects()) if (go.name == name) return go;
        return null;
    }

    static IEnumerable<GameObject> AllActive(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Transform t in root.GetComponentsInChildren<Transform>(false))
                yield return t.gameObject;
    }
}
#endif
