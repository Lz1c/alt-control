// MountainBuilder.cs
// Builds tall mountains by stacking PolygonNature SM_Rock_Cluster_* prefabs into peaks,
// each parented under its own "Mountain_Stack_##" object. Placement is bounds-aware: each
// rock's real height is measured, then dropped so it embeds into the layer below, tapering
// to a spire. Multiple shape variants + per-mountain scale/yaw/seed give variety. One
// mountain can also get an "Observation_Deck" (flat stone platform + fence railing) on top.
//
// Tools > Vegetation > Build Mountain Behind     - rebuilds the single Mountain_Stack_01
// Tools > Vegetation > Build Extra Mountains      - builds varied Mountain_Stack_02..04 (one with a deck)
// Tools > Vegetation > Clear Mountain             - removes Mountain_Stack_01
// Tools > Vegetation > Clear Extra Mountains      - removes Mountain_Stack_02..04

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MountainBuilder
{
    const string PrefabDir = "Assets/PolygonNature/Prefabs/Rocks/";
    const string PropDir = "Assets/PolygonNature/Prefabs/Props/";

    struct Step
    {
        public string prefab; public float scale; public Vector2 offset; public float yaw;
        public bool stack; public float embed;
        public Step(string p, float s, float ox, float oz, float yaw, bool stack, float embed)
        { prefab = p; scale = s; offset = new Vector2(ox, oz); this.yaw = yaw; this.stack = stack; this.embed = embed; }
    }

    static readonly Step[] VariantSpire =
    {
        new Step("SM_Rock_Cluster_Large_05", 2.3f,  0f,   0f,   20f,  false, 0f),
        new Step("SM_Rock_Cluster_Large_04", 2.1f, -7f,   3f,  140f,  false, 0f),
        new Step("SM_Rock_Cluster_Large_06", 2.0f,  6f,  -4f,  250f,  false, 0f),
        new Step("SM_Rock_Cluster_Large_02", 1.8f,  2f,   7f,   60f,  false, 0f),
        new Step("SM_Rock_Cluster_Large_05", 1.9f,  1f,  -1f,  200f,  true,  0.55f),
        new Step("SM_Rock_Cluster_Large_04", 1.7f, -3f,   1f,   30f,  true,  0.5f),
        new Step("SM_Rock_Cluster_Large_03", 1.8f,  0f,   0f,  300f,  true,  0.5f),
        new Step("SM_Rock_Cluster_Large_04", 1.5f,  1.5f, 1f,  110f,  true,  0.45f),
        new Step("SM_Rock_Cluster_Large_03", 1.5f,  0f,   0f,   75f,  true,  0.4f),
    };

    static readonly Step[] VariantBroad =
    {
        new Step("SM_Rock_Cluster_Large_06", 2.4f, -3f,   0f,    0f,  false, 0f),
        new Step("SM_Rock_Cluster_Large_05", 2.2f,  7f,   2f,  120f,  false, 0f),
        new Step("SM_Rock_Cluster_Large_04", 2.0f,  0f,  -7f,   60f,  false, 0f),
        new Step("SM_Rock_Cluster_Large_02", 1.8f, -6f,   5f,  300f,  false, 0f),
        new Step("SM_Rock_Cluster_Large_04", 1.8f, -4f,   1f,  200f,  true,  0.5f),
        new Step("SM_Rock_Cluster_Large_06", 1.7f,  5f,  -1f,   30f,  true,  0.5f),
        new Step("SM_Rock_Cluster_Large_03", 1.6f, -3f,   0f,   90f,  true,  0.45f),
        new Step("SM_Rock_Cluster_Large_04", 1.5f,  5f,   0f,  250f,  true,  0.45f),
        new Step("SM_Rock_Cluster_Large_03", 1.3f,  1f,   0f,  160f,  true,  0.4f),
    };

    static readonly Step[] VariantBlocky =
    {
        new Step("SM_Rock_Cluster_Large_05", 2.1f,  0f,   0f,   45f,  false, 0f),
        new Step("SM_Rock_Cluster_Large_02", 1.9f, -5f,   4f,  160f,  false, 0f),
        new Step("SM_Rock_Cluster_Large_06", 1.8f,  5f,   3f,  300f,  false, 0f),
        new Step("SM_Rock_Cluster_Large_05", 1.7f,  0f,  -2f,   90f,  true,  0.6f),
        new Step("SM_Rock_Cluster_Large_04", 1.6f,  2f,   2f,   20f,  true,  0.55f),
        new Step("SM_Rock_Cluster_Large_02", 1.4f, -1f,   0f,  210f,  true,  0.5f),
        new Step("SM_Rock_Cluster_Large_03", 1.4f,  0f,   0f,  120f,  true,  0.45f),
    };

    struct MountainCfg
    {
        public string name; public Vector2 xz; public float scale; public float yaw; public int seed;
        public Step[] variant; public bool deck;
        public MountainCfg(string n, float x, float z, float s, float yaw, int seed, Step[] v, bool deck)
        { name = n; xz = new Vector2(x, z); scale = s; this.yaw = yaw; this.seed = seed; variant = v; this.deck = deck; }
    }

    static readonly MountainCfg Single =
        new MountainCfg("Mountain_Stack_01", -188f, -74f, 0.52f, 0f, 20260602, VariantSpire, false);

    static readonly MountainCfg[] Extra =
    {
        new MountainCfg("Mountain_Stack_02", -226f, -92f,  0.62f,  35f, 111, VariantBroad,  false),
        new MountainCfg("Mountain_Stack_03", -158f, -120f, 0.50f, 210f, 222, VariantBlocky, true),
        new MountainCfg("Mountain_Stack_04", -238f, -120f, 0.46f, 110f, 333, VariantSpire,  false),
    };

    [MenuItem("Tools/Vegetation/Build Mountain Behind")]
    public static void BuildSingle() { BuildOne(Single); MarkDirty(); }

    [MenuItem("Tools/Vegetation/Build Extra Mountains")]
    public static void BuildExtra()
    {
        foreach (var cfg in Extra) BuildOne(cfg);
        MarkDirty();
    }

    static void BuildOne(MountainCfg cfg)
    {
        Scene scene = SceneManager.GetActiveScene();

        float groundY = 0f;
        if (Physics.Raycast(new Vector3(cfg.xz.x, 300f, cfg.xz.y), Vector3.down, out RaycastHit gh, 600f, ~0, QueryTriggerInteraction.Ignore))
            groundY = gh.point.y;

        GameObject prev = Find(scene, cfg.name);
        if (prev != null) Undo.DestroyObjectImmediate(prev);

        GameObject root = new GameObject(cfg.name);
        root.transform.position = new Vector3(cfg.xz.x, groundY, cfg.xz.y);
        Undo.RegisterCreatedObjectUndo(root, "Build Mountain");

        Random.InitState(cfg.seed);

        float pileTop = groundY;
        float baseBottom = groundY - 3.5f;
        int placed = 0;

        foreach (Step s in cfg.variant)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + s.prefab + ".prefab");
            if (prefab == null) { Debug.LogWarning($"[MountainBuilder] Missing prefab {s.prefab}"); continue; }

            GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root.transform);
            inst.transform.localScale = Vector3.one * s.scale;
            inst.transform.rotation = Quaternion.Euler(Random.Range(-7f, 7f), s.yaw, Random.Range(-7f, 7f));
            inst.transform.position = new Vector3(cfg.xz.x + s.offset.x, groundY, cfg.xz.y + s.offset.y);

            if (!TryGetWorldBounds(inst, out Bounds b)) { placed++; continue; }
            float desiredBottom = s.stack ? (pileTop - s.embed * b.size.y) : baseBottom;
            float dy = desiredBottom - b.min.y;
            inst.transform.position += new Vector3(0f, dy, 0f);
            float newTop = b.max.y + dy;
            if (newTop > pileTop) pileTop = newTop;
            placed++;
        }

        root.transform.localScale = Vector3.one * cfg.scale;
        root.transform.localEulerAngles = new Vector3(0f, cfg.yaw, 0f);

        if (TryGetWorldBounds(root, out Bounds total))
        {
            Debug.Log($"[MountainBuilder] '{cfg.name}' {placed} rocks @XZ({cfg.xz.x},{cfg.xz.y}) " +
                      $"peakTopY={total.max.y:N1} height={total.size.y:N1} footprint={total.size.x:N1}x{total.size.z:N1}");
            if (cfg.deck) BuildDeck(root, total);
        }
    }

    static void BuildDeck(GameObject root, Bounds mb)
    {
        GameObject tilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "SM_Rock_Tile_01.prefab");
        GameObject fencePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PropDir + "SM_Prop_Fence_02.prefab");
        if (tilePrefab == null || fencePrefab == null)
        { Debug.LogWarning("[MountainBuilder] Deck prefab(s) missing; skipping deck."); return; }

        Vector3 summit = new Vector3(mb.center.x, mb.max.y, mb.center.z);
        float deckY = summit.y - 2.0f;
        const float deckWidth = 9f;
        const float railRadius = 3.8f;

        GameObject holder = new GameObject("Observation_Deck");
        holder.transform.SetParent(root.transform, true);
        Vector3 ls = root.transform.lossyScale;
        holder.transform.localScale = new Vector3(1f / ls.x, 1f / ls.y, 1f / ls.z);
        holder.transform.position = new Vector3(summit.x, deckY, summit.z);

        GameObject tile = (GameObject)PrefabUtility.InstantiatePrefab(tilePrefab, holder.transform);
        tile.transform.localScale = Vector3.one;
        tile.transform.rotation = Quaternion.identity;
        tile.transform.position = new Vector3(summit.x, deckY, summit.z);
        TryGetWorldBounds(tile, out Bounds tb);
        float sx = deckWidth / Mathf.Max(0.01f, tb.size.x);
        float sz = deckWidth / Mathf.Max(0.01f, tb.size.z);
        float sy = Mathf.Min(sx, sz) * 0.35f;
        tile.transform.localScale = new Vector3(sx, sy, sz);
        tile.transform.position = new Vector3(summit.x, deckY, summit.z);
        TryGetWorldBounds(tile, out Bounds tb2);
        float platformTopY = tb2.max.y;

        GameObject probe = (GameObject)PrefabUtility.InstantiatePrefab(fencePrefab, holder.transform);
        probe.transform.localScale = Vector3.one * 1.2f;
        probe.transform.rotation = Quaternion.identity;
        probe.transform.position = new Vector3(summit.x, platformTopY, summit.z);
        TryGetWorldBounds(probe, out Bounds fb);
        float fenceW = Mathf.Max(1f, fb.size.x);
        Object.DestroyImmediate(probe);

        int count = Mathf.Clamp(Mathf.FloorToInt(2f * Mathf.PI * railRadius / fenceW), 6, 14);
        for (int i = 0; i < count; i++)
        {
            if (i == 0) continue;
            float ang = (i / (float)count) * Mathf.PI * 2f;
            float cx = summit.x + railRadius * Mathf.Cos(ang);
            float cz = summit.z + railRadius * Mathf.Sin(ang);
            GameObject fence = (GameObject)PrefabUtility.InstantiatePrefab(fencePrefab, holder.transform);
            fence.transform.localScale = Vector3.one * 1.2f;
            fence.transform.rotation = Quaternion.Euler(0f, ang * Mathf.Rad2Deg + 90f, 0f);
            fence.transform.position = new Vector3(cx, platformTopY, cz);
            if (TryGetWorldBounds(fence, out Bounds bf))
                fence.transform.position += new Vector3(0f, platformTopY - bf.min.y, 0f);
        }

        Debug.Log($"[MountainBuilder] Observation deck on '{root.name}' at ({summit.x:N1},{deckY:N1},{summit.z:N1}), {count - 1} rail segments.");
    }

    [MenuItem("Tools/Vegetation/Clear Mountain")]
    public static void ClearSingle() { ClearByName("Mountain_Stack_01"); MarkDirty(); }

    [MenuItem("Tools/Vegetation/Clear Extra Mountains")]
    public static void ClearExtra()
    {
        foreach (var cfg in Extra) ClearByName(cfg.name);
        MarkDirty();
    }

    static void ClearByName(string name)
    {
        GameObject go = Find(SceneManager.GetActiveScene(), name);
        if (go != null) Undo.DestroyObjectImmediate(go);
    }

    static void MarkDirty()
    {
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
