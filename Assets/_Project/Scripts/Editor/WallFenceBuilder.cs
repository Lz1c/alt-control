// WallFenceBuilder.cs
// Surrounds the SM_Bld_Base_Wall_01 platform with a railing made from copies of the
// SM_Bld_Base_Floor_Combined_01 (2) tile, stood up and tiled along the platform's REAL
// (possibly rotated) top-face edges. Everything is parented under a "Wall_Fence" object
// whose pivot sits at the platform centre and whose orientation matches the platform, so it
// can be rotated / moved as a single unit.
//
// Tools > Vegetation > Build Wall Fence
// Tools > Vegetation > Clear Wall Fence

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class WallFenceBuilder
{
    const string WallName = "SM_Bld_Base_Wall_01";
    const string TemplateName = "SM_Bld_Base_Floor_Combined_01 (2)";
    const string FenceRoot = "Wall_Fence";

    const float PanelVerticalScale = 0.5f; // tile depth (2.5) * this = rail height (~1.25m)
    const float NominalPanelWidth = 2.5f;  // tile width along the run direction
    const float Inset = 0.2f;              // pull rail slightly inward from the edge

    [MenuItem("Tools/Vegetation/Build Wall Fence")]
    public static void Build()
    {
        Scene scene = SceneManager.GetActiveScene();
        GameObject wall = Find(scene, WallName);
        GameObject template = Find(scene, TemplateName);
        if (wall == null) { Debug.LogError($"[WallFence] '{WallName}' not found."); return; }
        if (template == null) { Debug.LogError($"[WallFence] template '{TemplateName}' not found."); return; }

        MeshFilter mf = wall.GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null) { Debug.LogError("[WallFence] wall has no mesh."); return; }

        // --- real top-face corners of the (rotated/scaled) platform ---
        Bounds lb = mf.sharedMesh.bounds;
        Vector3 c = lb.center, e = lb.extents;
        // thinnest local axis = thickness (vertical when laid flat)
        int ta = (e.x <= e.y && e.x <= e.z) ? 0 : (e.y <= e.z ? 1 : 2);
        // the two in-plane axis indices
        int a1 = (ta + 1) % 3, a2 = (ta + 2) % 3;

        Vector3 PlusFace(float s1, float s2, float st)
        {
            Vector3 v = c;
            v[a1] += s1 * e[a1];
            v[a2] += s2 * e[a2];
            v[ta] += st * e[ta];
            return wall.transform.TransformPoint(v);
        }
        // candidate corners on +thickness and -thickness faces; pick the higher (top) one.
        Vector3 topCenter = wall.transform.TransformPoint(c + AxisVec(ta, e[ta]));
        Vector3 botCenter = wall.transform.TransformPoint(c - AxisVec(ta, e[ta]));
        float st2 = (topCenter.y >= botCenter.y) ? 1f : -1f;

        Vector3 p00 = PlusFace(-1, -1, st2);
        Vector3 p10 = PlusFace(+1, -1, st2);
        Vector3 p11 = PlusFace(+1, +1, st2);
        Vector3 p01 = PlusFace(-1, +1, st2);

        Vector3 center = (p00 + p10 + p11 + p01) * 0.25f;
        Vector3 planeN = Vector3.Cross(p10 - p00, p01 - p00).normalized;
        if (planeN.y < 0) planeN = -planeN;

        // Root: pivot at platform centre, oriented to the platform (so it rotates nicely).
        GameObject prev = Find(scene, FenceRoot);
        if (prev != null) Undo.DestroyObjectImmediate(prev);
        GameObject root = new GameObject(FenceRoot);
        Vector3 e1dir = (p10 - p00); e1dir.y = 0; e1dir.Normalize();
        root.transform.position = center;
        root.transform.rotation = Quaternion.LookRotation(e1dir, Vector3.up);
        Undo.RegisterCreatedObjectUndo(root, "Build Wall Fence");

        int total = 0;
        total += TileEdge(root.transform, template, p00, p10, center, planeN);
        total += TileEdge(root.transform, template, p10, p11, center, planeN);
        total += TileEdge(root.transform, template, p11, p01, center, planeN);
        total += TileEdge(root.transform, template, p01, p00, center, planeN);

        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = root;
                    Debug.Log($"[WallFence] Built {total} rail panels around '{WallName}' (centre {center}).");
    }

    static int TileEdge(Transform parent, GameObject template, Vector3 start, Vector3 end, Vector3 center, Vector3 planeN)
    {
        Vector3 edge = end - start;
        Vector3 edgeDir = edge; edgeDir.y = 0f; float hlen = edgeDir.magnitude; edgeDir.Normalize();
        float len = edge.magnitude;
        int count = Mathf.Max(1, Mathf.RoundToInt(len / NominalPanelWidth));

        // inward horizontal normal (toward centre)
        Vector3 mid = (start + end) * 0.5f;
        Vector3 inward = center - mid; inward.y = 0f; inward.Normalize();

        // panel orientation: width(localX)->edgeDir, height(localZ)->up, thin(localY)->normal
        Vector3 faceNormal = Vector3.Cross(Vector3.up, edgeDir).normalized;
        Quaternion rot = Quaternion.LookRotation(Vector3.up, faceNormal);

        int made = 0;
        for (int i = 0; i < count; i++)
        {
            float t = (i + 0.5f) / count;
            Vector3 target = Vector3.Lerp(start, end, t) + inward * Inset;
            float surfaceY = PlaneY(center, planeN, target.x, target.z);

            GameObject p = (GameObject)Object.Instantiate(template, parent);
            p.name = "FencePanel";
            p.transform.rotation = rot;
            p.transform.localScale = new Vector3(1f, 1f, PanelVerticalScale);
            p.transform.position = new Vector3(target.x, surfaceY + 5f, target.z);

            if (TryGetWorldBounds(p, out Bounds b))
            {
                Vector3 pos = p.transform.position;
                pos.x += target.x - b.center.x;
                pos.z += target.z - b.center.z;
                pos.y += surfaceY - b.min.y;
                p.transform.position = pos;
            }
            Undo.RegisterCreatedObjectUndo(p, "Build Wall Fence");
            made++;
        }
        return made;
    }

    static float PlaneY(Vector3 p0, Vector3 n, float x, float z)
    {
        if (Mathf.Abs(n.y) < 1e-4f) return p0.y;
        return p0.y - (n.x * (x - p0.x) + n.z * (z - p0.z)) / n.y;
    }

    static Vector3 AxisVec(int axis, float v)
    {
        Vector3 r = Vector3.zero; r[axis] = v; return r;
    }

    [MenuItem("Tools/Vegetation/Clear Wall Fence")]
    public static void Clear()
    {
        GameObject go = Find(SceneManager.GetActiveScene(), FenceRoot);
        if (go == null) { Debug.Log("[WallFence] Nothing to clear."); return; }
        Undo.DestroyObjectImmediate(go);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    static bool TryGetWorldBounds(GameObject go, out Bounds b)
    {
        var rends = go.GetComponentsInChildren<Renderer>();
        if (rends.Length == 0) { b = new Bounds(go.transform.position, Vector3.zero); return false; }
        b = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
        return true;
    }

    static GameObject Find(Scene scene, string name)
    {
        foreach (GameObject go in scene.GetRootGameObjects())
            if (go.name == name) return go;
        return null;
    }
}
#endif
