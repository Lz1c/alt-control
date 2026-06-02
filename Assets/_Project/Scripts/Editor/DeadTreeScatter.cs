// DeadTreeScatter.cs
// Sparsely scatters dead trees over the built mountains (Mountain_Stack_*). Each tree is
// raycast onto that mountain's own rock colliders, kept upright, spaced apart, and parented
// under "DeadTrees_Mountains" (world scale 1, so the mountains' scaling doesn't shrink them).
//
// Tools > Vegetation > Scatter Dead Trees On Mountains
// Tools > Vegetation > Clear Dead Trees

#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class DeadTreeScatter
{
    const string RootName = "DeadTrees_Mountains";
    const string MountainPrefix = "Mountain_Stack_";
    const string TreesDir = "Assets/PolygonNature/Prefabs/Trees/";

    const int MinPerMountain = 2, MaxPerMountain = 4; // sparse
    const float Spacing = 6f;
    const float MaxSlopeDot = 0.45f;   // allow fairly steep rock ledges
    const int AttemptMul = 250;

    static readonly string[] DeadTrees =
    {
        "SM_Tree_Dead_01", "SM_Tree_Dead_02", "SM_Tree_Dead_03",
        "SM_Tree_Generic_Dead_01", "SM_Tree_Pine_Dead_01", "SM_Tree_Birch_Dead_01",
    };

    [MenuItem("Tools/Vegetation/Scatter Dead Trees On Mountains")]
    public static void Scatter()
    {
        Scene scene = SceneManager.GetActiveScene();

        var mountains = new List<GameObject>();
        foreach (GameObject go in scene.GetRootGameObjects())
            if (go.name.StartsWith(MountainPrefix)) mountains.Add(go);
        if (mountains.Count == 0) { Debug.LogError("[DeadTrees] No Mountain_Stack_* found."); return; }

        GameObject prev = Find(scene, RootName);
        if (prev != null) Undo.DestroyObjectImmediate(prev);
        GameObject root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Scatter Dead Trees");

        int total = 0, idx = 0;
        foreach (GameObject mtn in mountains)
        {
            idx++;
            Random.InitState(4400 + idx * 97);

            var colliders = new HashSet<Collider>(mtn.GetComponentsInChildren<Collider>(true));
            if (colliders.Count == 0) continue;

            // combined world bounds for XZ sampling + top origin
            Bounds b = default; bool has = false;
            foreach (var r in mtn.GetComponentsInChildren<Renderer>())
            { if (!has) { b = r.bounds; has = true; } else b.Encapsulate(r.bounds); }
            if (!has) continue;

            GameObject group = new GameObject(mtn.name);
            group.transform.SetParent(root.transform);

            int target = Random.Range(MinPerMountain, MaxPerMountain + 1);
            var placed = new List<Vector3>();
            int placedCount = 0, attempts = 0, maxAttempts = target * AttemptMul;

            while (placedCount < target && attempts < maxAttempts)
            {
                attempts++;
                float x = Random.Range(b.min.x, b.max.x);
                float z = Random.Range(b.min.z, b.max.z);
                Vector3 origin = new Vector3(x, b.max.y + 30f, z);
                if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 600f, ~0, QueryTriggerInteraction.Ignore))
                    continue;
                if (!colliders.Contains(hit.collider)) continue;   // must hit THIS mountain
                if (hit.normal.y < MaxSlopeDot) continue;
                if (TooClose(hit.point, placed, Spacing)) continue;

                string rel = DeadTrees[Random.Range(0, DeadTrees.Length)];
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TreesDir + rel + ".prefab");
                if (prefab == null) { Debug.LogWarning($"[DeadTrees] Missing {rel}"); continue; }

                GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, group.transform);
                inst.transform.position = hit.point;
                inst.transform.rotation = Quaternion.Euler(Random.Range(-5f, 5f), Random.Range(0f, 360f), Random.Range(-5f, 5f));
                float s = Random.Range(1.0f, 1.7f);
                inst.transform.localScale = new Vector3(s, s, s);
                Undo.RegisterCreatedObjectUndo(inst, "Scatter Dead Trees");
                placed.Add(hit.point);
                placedCount++;
                total++;
            }
            Debug.Log($"[DeadTrees] {mtn.name}: {placedCount} dead trees");
        }

        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = root;
        Debug.Log($"[DeadTrees] Done. {total} dead trees across {mountains.Count} mountains.");
    }

    static bool TooClose(Vector3 p, List<Vector3> pts, float min)
    {
        float sq = min * min;
        foreach (var q in pts) { float dx = q.x - p.x, dz = q.z - p.z; if (dx * dx + dz * dz < sq) return true; }
        return false;
    }

    [MenuItem("Tools/Vegetation/Clear Dead Trees")]
    public static void Clear()
    {
        GameObject go = Find(SceneManager.GetActiveScene(), RootName);
        if (go == null) { Debug.Log("[DeadTrees] Nothing to clear."); return; }
        Undo.DestroyObjectImmediate(go);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    static GameObject Find(Scene scene, string name)
    {
        foreach (GameObject go in scene.GetRootGameObjects())
            if (go.name == name) return go;
        return null;
    }
}
#endif
