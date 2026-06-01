// VegetationScatterTool.cs
// Editor utility that procedurally scatters PolygonNature plant/tree prefabs onto the
// scene's "ground" terrain colliders, filling the bare green tiles with natural-looking
// vegetation while leaving the existing hand-placed garden ("another" group) untouched.
//
// All spawned objects are parented under a single "Vegetation_Scatter" root so the whole
// pass can be cleared or re-run from the Tools/Vegetation menu. Placement raycasts straight
// down onto the ground colliders, so plants conform to the surface height; spots that already
// have a prop/tree/existing plant on top (first raycast hit is not a ground collider) are
// skipped, as are points too close to the existing garden or to each other.
//
// Tools > Vegetation > Scatter On Ground   -> generate
// Tools > Vegetation > Clear Scatter        -> remove the Vegetation_Scatter root

#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class VegetationScatterTool
{
    const string ScatterRootName = "Vegetation_Scatter";
    const string GroundRootName = "ground";

    // Only scatter onto these specific tiles: the four SM_Terrain_Ground_Mound_Small_01
    // tiles (base, (1), (2), (3)) under the "ground" group. A placement is accepted only
    // when the downward ray's first hit is one of these tile colliders, so plants never
    // land on neighbouring tiles, the base tile, rocks or props.
    const string TargetParentName = "ground";
    const string TargetTilePrefix = "SM_Terrain_Ground_Mound_Small_01";

    const int PlantTarget = 340;      // desired number of ground plants
    const int TreeTarget = 10;        // desired number of sprinkled trees
    const float PlantSpacing = 0.7f;  // min distance between scattered plants
    const float TreeSpacing = 3.5f;   // min distance between scattered trees
    const float PlantEdgeInset = 0.4f;// keep plants this far inside a tile's footprint
    const float TreeEdgeInset = 1.2f; // keep trees further from the rim (wider canopy)
    const float MaxSlopeDot = 0.80f;  // require ground normal.y >= this (skip steep faces)
    const int MaxAttemptsMultiplier = 200;

    // Ground-cover plants (weighted by repetition: grass/ferns/bushes dominate).
    static readonly string[] PlantPrefabs =
    {
        "Plants/SM_Plant_Grass_01", "Plants/SM_Plant_Grass_02", "Plants/SM_Plant_Grass_03",
        "Plants/SM_Plant_Grass_04", "Plants/SM_Plant_Grass_05",
        "Plants/SM_Plant_Grass_01", "Plants/SM_Plant_Grass_03", "Plants/SM_Plant_Grass_05",
        "Plants/SM_Plant_Fern_01", "Plants/SM_Plant_Fern_02", "Plants/SM_Plant_Fern_03",
        "Plants/SM_Plant_Fern_Leaves_01", "Plants/SM_Plant_Fern_Leaves_02",
        "Plants/SM_Plant_Bush_01", "Plants/SM_Plant_Bush_02", "Plants/SM_Plant_Bush_03",
        "Plants/SM_Plant_Bush_Leaves_01", "Plants/SM_Plant_Bush_Leaves_02", "Plants/SM_Plant_Bush_Leaves_03",
        "Plants/SM_Plant_Hedge_Bush_01", "Plants/SM_Plant_Hedge_Bush_02",
        "Plants/SM_Plant_01", "Plants/SM_Plant_02", "Plants/SM_Plant_03", "Plants/SM_Plant_04",
        "Plants/SM_Plant_05", "Plants/SM_Plant_06", "Plants/SM_Plant_07",
        "Plants/SM_Plant_Undergrowth_01", "Plants/SM_Plant_PalmBush_01",
        "Plants/SM_Plant_Flowers_01", "Plants/SM_Plant_PurpleFlower_01",
        "Plants/SM_Plant_Mushrooms_01", "Plants/SM_Plant_Mushrooms_02", "Plants/SM_Plant_Mushrooms_03",
    };

    // Healthy trees only (healing tone) - sprinkled sparsely for verticality.
    static readonly string[] TreePrefabs =
    {
        "Trees/SM_Tree_01", "Trees/SM_Tree_02", "Trees/SM_Tree_03", "Trees/SM_Tree_04",
        "Trees/SM_Tree_Round_01", "Trees/SM_Tree_Round_02", "Trees/SM_Tree_Round_03",
        "Trees/SM_Tree_Round_04", "Trees/SM_Tree_Round_05",
        "Trees/SM_Tree_Birch_01", "Trees/SM_Tree_Birch_02", "Trees/SM_Tree_Birch_03",
        "Trees/SM_Tree_Birch_Small_01",
        "Trees/SM_Tree_Pine_Small_01", "Trees/SM_Tree_Pine_Small_02",
        "Trees/SM_Tree_TallRound_01",
    };

    const string PrefabRoot = "Assets/PolygonNature/Prefabs/";

    [MenuItem("Tools/Vegetation/Scatter On Ground")]
    public static void Scatter()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid())
        {
            Debug.LogError("[VegetationScatter] No active scene.");
            return;
        }

        // Resolve the target tiles and collect their colliders + combined XZ sample region.
        GameObject groundRoot = FindRoot(scene, TargetParentName);
        if (groundRoot == null)
        {
            Debug.LogError($"[VegetationScatter] Could not find '{TargetParentName}' root.");
            return;
        }
        var targetSet = new HashSet<Collider>();
        var sampleMin = new Vector2(float.MaxValue, float.MaxValue);
        var sampleMax = new Vector2(float.MinValue, float.MinValue);
        var tileNames = new List<string>();
        foreach (Transform t in groundRoot.GetComponentsInChildren<Transform>(true))
        {
            if (!t.name.StartsWith(TargetTilePrefix)) continue;
            Collider col = t.GetComponent<Collider>();
            if (col == null) continue;
            targetSet.Add(col);
            tileNames.Add(t.name);
            Bounds b = col.bounds;
            sampleMin.x = Mathf.Min(sampleMin.x, b.min.x);
            sampleMin.y = Mathf.Min(sampleMin.y, b.min.z);
            sampleMax.x = Mathf.Max(sampleMax.x, b.max.x);
            sampleMax.y = Mathf.Max(sampleMax.y, b.max.z);
        }
        if (targetSet.Count == 0)
        {
            Debug.LogError($"[VegetationScatter] No tiles named '{TargetTilePrefix}*' with colliders under '{TargetParentName}'.");
            return;
        }
        Debug.Log($"[VegetationScatter] Target tiles ({targetSet.Count}): {string.Join(", ", tileNames)}  " +
                  $"sampleXZ=({sampleMin.x:N1},{sampleMin.y:N1})..({sampleMax.x:N1},{sampleMax.y:N1})");

        // Fresh scatter root (clear any previous pass first).
        GameObject prev = FindRoot(scene, ScatterRootName);
        if (prev != null)
            Undo.DestroyObjectImmediate(prev);

        GameObject scatterRoot = new GameObject(ScatterRootName);
        Undo.RegisterCreatedObjectUndo(scatterRoot, "Scatter Vegetation");

        GameObject plantGroup = new GameObject("Plants");
        plantGroup.transform.SetParent(scatterRoot.transform);
        GameObject treeGroup = new GameObject("Trees");
        treeGroup.transform.SetParent(scatterRoot.transform);

        // Separate spacing lists: trees space out only against other trees, plants against
        // other plants. Trees go first so plant undergrowth can fill in around their bases.
        var placedTrees = new List<Vector3>();
        var placedPlants = new List<Vector3>();

        int treesPlaced = PlacePass(TreePrefabs, TreeTarget, TreeSpacing, TreeEdgeInset, true,
            targetSet, sampleMin, sampleMax, placedTrees, treeGroup.transform);
        int plantsPlaced = PlacePass(PlantPrefabs, PlantTarget, PlantSpacing, PlantEdgeInset, false,
            targetSet, sampleMin, sampleMax, placedPlants, plantGroup.transform);

        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = scatterRoot;
        Debug.Log($"[VegetationScatter] Placed {plantsPlaced} plants and {treesPlaced} trees under '{ScatterRootName}'.");
    }

    static int PlacePass(string[] prefabs, int target, float spacing, float edgeInset, bool isTree,
        HashSet<Collider> targetSet, Vector2 sampleMin, Vector2 sampleMax,
        List<Vector3> placed, Transform parent)
    {
        int placedCount = 0;
        int attempts = 0;
        int maxAttempts = target * MaxAttemptsMultiplier;

        while (placedCount < target && attempts < maxAttempts)
        {
            attempts++;

            float x = Random.Range(sampleMin.x, sampleMax.x);
            float z = Random.Range(sampleMin.y, sampleMax.y);
            Vector3 origin = new Vector3(x, 60f, z);

            if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 200f, ~0, QueryTriggerInteraction.Ignore))
                continue;

            // First hit must be one of the four target mound tiles - nothing else.
            if (!targetSet.Contains(hit.collider))
                continue;

            // Stay inside the tile's footprint by an inset so foliage doesn't overhang the rim.
            Bounds b = hit.collider.bounds;
            if (hit.point.x < b.min.x + edgeInset || hit.point.x > b.max.x - edgeInset ||
                hit.point.z < b.min.z + edgeInset || hit.point.z > b.max.z - edgeInset)
                continue;

            if (hit.normal.y < MaxSlopeDot)
                continue;

            Vector3 p = hit.point;

            if (TooClose(p, placed, spacing)) continue;

            string rel = prefabs[Random.Range(0, prefabs.Length)];
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabRoot + rel + ".prefab");
            if (prefab == null)
            {
                Debug.LogWarning($"[VegetationScatter] Missing prefab: {PrefabRoot + rel}.prefab");
                continue;
            }

            GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            inst.transform.position = p;

            float yaw = Random.Range(0f, 360f);
            if (isTree)
            {
                inst.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
                float s = Random.Range(0.85f, 1.35f);
                inst.transform.localScale = new Vector3(s, s, s);
            }
            else
            {
                // Slight tilt for organic feel; plants stay essentially upright.
                float tiltX = Random.Range(-6f, 6f);
                float tiltZ = Random.Range(-6f, 6f);
                inst.transform.rotation = Quaternion.Euler(tiltX, yaw, tiltZ);
                float s = Random.Range(0.8f, 1.3f);
                inst.transform.localScale = new Vector3(s, s, s);
            }

            Undo.RegisterCreatedObjectUndo(inst, "Scatter Vegetation");
            placed.Add(p);
            placedCount++;
        }

        return placedCount;
    }

    static bool TooClose(Vector3 p, List<Vector3> pts, float minDist)
    {
        float sqr = minDist * minDist;
        for (int i = 0; i < pts.Count; i++)
        {
            float dx = pts[i].x - p.x;
            float dz = pts[i].z - p.z;
            if (dx * dx + dz * dz < sqr)
                return true;
        }
        return false;
    }

    // Diagnostic: raycast straight down at a coarse grid and log the full collider stack
    // at each point, so we can see which terrain tile is the green walkable top surface.
    [MenuItem("Tools/Vegetation/Probe Ground")]
    public static void ProbeGround()
    {
        // First clear any scatter so plant colliders don't pollute the probe.
        Clear();

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("[VegetationScatter] Ground probe (top->bottom collider stack per point):");
        for (float x = -90f; x <= -20f; x += 14f)
        {
            for (float z = -45f; z <= 30f; z += 15f)
            {
                Vector3 origin = new Vector3(x, 80f, z);
                RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, 200f, ~0, QueryTriggerInteraction.Ignore);
                System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                if (hits.Length == 0)
                {
                    sb.AppendLine($"  ({x:N0},{z:N0}) : <no hit>");
                    continue;
                }
                var parts = new List<string>();
                foreach (var h in hits)
                    parts.Add($"{h.collider.gameObject.name}@{h.point.y:N1}");
                sb.AppendLine($"  ({x:N0},{z:N0}) : {string.Join("  |  ", parts)}");
            }
        }
        string outPath = "Assets/Screenshots/ground_probe.txt";
        System.IO.File.WriteAllText(outPath, sb.ToString());
        Debug.Log("[VegetationScatter] Ground probe written to " + outPath);
    }

    [MenuItem("Tools/Vegetation/Clear Scatter")]
    public static void Clear()
    {
        Scene scene = SceneManager.GetActiveScene();
        GameObject prev = FindRoot(scene, ScatterRootName);
        if (prev == null)
        {
            Debug.Log("[VegetationScatter] Nothing to clear.");
            return;
        }
        Undo.DestroyObjectImmediate(prev);
        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("[VegetationScatter] Cleared scatter root.");
    }

    static GameObject FindRoot(Scene scene, string name)
    {
        foreach (GameObject go in scene.GetRootGameObjects())
            if (go.name == name)
                return go;
        return null;
    }
}
#endif
