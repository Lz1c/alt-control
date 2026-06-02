// GrassMoundVegetation.cs
// Scatters PolygonNature plants (and the occasional small tree) onto every numbered
// SM_Terrain_Ground_Mound_Large_01 (n) grass disc. Each disc gets a varied, randomised
// fill within its circular footprint, raycast-anchored to that disc's own collider.
// Tile "(1)" is deliberately denser than the rest. Parented under "Vegetation_GrassMounds".
//
// Tools > Vegetation > Scatter On Grass Mounds
// Tools > Vegetation > Clear Grass Mounds

#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class GrassMoundVegetation
{
    const string RootName = "Vegetation_GrassMounds";
    const string TilePrefix = "SM_Terrain_Ground_Mound_Large_01 (";  // only the numbered batch
    const string FeatureTile = "SM_Terrain_Ground_Mound_Large_01 (1)"; // extra-dense disc
    const string PlantsDir = "Assets/PolygonNature/Prefabs/Plants/";
    const string TreesDir = "Assets/PolygonNature/Prefabs/Trees/";

    const float RadiusInset = 0.9f;

    // Normal discs (denser than before).
    const int PlantMin = 55, PlantMax = 95;
    const float PlantSpacing = 1.05f;
    // Feature disc (1): noticeably denser.
    const int FeaturePlantMin = 150, FeaturePlantMax = 190;
    const float FeaturePlantSpacing = 0.72f;

    const float TreeSpacing = 4.0f;
    const int TreeMaxPerTile = 2;
    const float MaxSlopeDot = 0.85f;
    const int AttemptMul = 90;

    static readonly string[] Plants =
    {
        "SM_Plant_Grass_01","SM_Plant_Grass_02","SM_Plant_Grass_03","SM_Plant_Grass_04","SM_Plant_Grass_05",
        "SM_Plant_Grass_01","SM_Plant_Grass_03","SM_Plant_Grass_05","SM_Plant_Grass_02","SM_Plant_Grass_04",
        "SM_Plant_Fern_01","SM_Plant_Fern_02","SM_Plant_Fern_03","SM_Plant_Fern_Leaves_01","SM_Plant_Fern_Leaves_02",
        "SM_Plant_Bush_01","SM_Plant_Bush_02","SM_Plant_Bush_03",
        "SM_Plant_Bush_Leaves_01","SM_Plant_Bush_Leaves_02","SM_Plant_Bush_Leaves_03",
        "SM_Plant_Hedge_Bush_01","SM_Plant_Hedge_Bush_02",
        "SM_Plant_01","SM_Plant_02","SM_Plant_03","SM_Plant_04","SM_Plant_05","SM_Plant_06","SM_Plant_07",
        "SM_Plant_Undergrowth_01","SM_Plant_PalmBush_01",
        "SM_Plant_Flowers_01","SM_Plant_PurpleFlower_01",
        "SM_Plant_Mushrooms_01","SM_Plant_Mushrooms_02","SM_Plant_Mushrooms_03",
    };

    static readonly string[] Trees =
    {
        "SM_Tree_Round_01","SM_Tree_Round_02","SM_Tree_Round_03","SM_Tree_Round_04","SM_Tree_Round_05",
        "SM_Tree_Birch_Small_01","SM_Tree_Pine_Small_01","SM_Tree_Pine_Small_02","SM_Tree_01","SM_Tree_02",
    };

    [MenuItem("Tools/Vegetation/Scatter On Grass Mounds")]
    public static void Scatter()
    {
        Scene scene = SceneManager.GetActiveScene();

        var tiles = new List<Collider>();
        foreach (GameObject go in Object.FindObjectsOfType<GameObject>(true))
        {
            if (!go.name.StartsWith(TilePrefix)) continue;
            Collider col = go.GetComponent<Collider>();
            if (col != null) tiles.Add(col);
        }
        if (tiles.Count == 0) { Debug.LogError($"[GrassMounds] No '{TilePrefix}...)' tiles with colliders found."); return; }

        GameObject prev = Find(scene, RootName);
        if (prev != null) Undo.DestroyObjectImmediate(prev);
        GameObject root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Scatter Grass Mounds");

        int totalPlants = 0, totalTrees = 0, idx = 0;
        foreach (Collider tile in tiles)
        {
            idx++;
            Random.InitState(7000 + idx * 131);

            Bounds b = tile.bounds;
            Vector3 center = b.center;
            float radius = Mathf.Min(b.extents.x, b.extents.z) * RadiusInset;

            bool feature = tile.gameObject.name == FeatureTile;
            float plantSpacing = feature ? FeaturePlantSpacing : PlantSpacing;
            int plantTarget = feature ? Random.Range(FeaturePlantMin, FeaturePlantMax + 1)
                                      : Random.Range(PlantMin, PlantMax + 1);
            int treeTarget = feature ? Random.Range(1, 4) : Random.Range(0, TreeMaxPerTile + 1);

            GameObject group = new GameObject(tile.gameObject.name);
            group.transform.SetParent(root.transform);

            var placedPlants = new List<Vector3>();
            var placedTrees = new List<Vector3>();

            totalTrees += PlaceCircle(Trees, TreesDir, treeTarget, TreeSpacing, true, tile, center, radius, placedTrees, group.transform);
            totalPlants += PlaceCircle(Plants, PlantsDir, plantTarget, plantSpacing, false, tile, center, radius, placedPlants, group.transform);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = root;
        Debug.Log($"[GrassMounds] Done. {tiles.Count} discs, {totalPlants} plants + {totalTrees} trees under '{RootName}'.");
    }

    static int PlaceCircle(string[] names, string dir, int target, float spacing, bool isTree,
        Collider tile, Vector3 center, float radius, List<Vector3> placed, Transform parent)
    {
        int placedCount = 0, attempts = 0, maxAttempts = Mathf.Max(60, target * AttemptMul);
        while (placedCount < target && attempts < maxAttempts)
        {
            attempts++;
            float ang = Random.value * Mathf.PI * 2f;
            float r = radius * Mathf.Sqrt(Random.value);
            float x = center.x + r * Mathf.Cos(ang);
            float z = center.z + r * Mathf.Sin(ang);
            Vector3 origin = new Vector3(x, center.y + 50f, z);

            if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 200f, ~0, QueryTriggerInteraction.Ignore))
                continue;
            if (hit.collider != tile) continue;
            if (hit.normal.y < MaxSlopeDot) continue;

            Vector3 p = hit.point;
            if (TooClose(p, placed, spacing)) continue;

            string rel = names[Random.Range(0, names.Length)];
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(dir + rel + ".prefab");
            if (prefab == null) { Debug.LogWarning($"[GrassMounds] Missing prefab: {dir + rel}"); continue; }

            GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            inst.transform.position = p;
            float yaw = Random.Range(0f, 360f);
            if (isTree)
            {
                inst.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
                float s = Random.Range(0.7f, 1.1f);
                inst.transform.localScale = new Vector3(s, s, s);
            }
            else
            {
                inst.transform.rotation = Quaternion.Euler(Random.Range(-6f, 6f), yaw, Random.Range(-6f, 6f));
                float s = Random.Range(0.8f, 1.3f);
                inst.transform.localScale = new Vector3(s, s, s);
            }
            Undo.RegisterCreatedObjectUndo(inst, "Scatter Grass Mounds");
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
            float dx = pts[i].x - p.x, dz = pts[i].z - p.z;
            if (dx * dx + dz * dz < sqr) return true;
        }
        return false;
    }

    [MenuItem("Tools/Vegetation/Clear Grass Mounds")]
    public static void Clear()
    {
        GameObject go = Find(SceneManager.GetActiveScene(), RootName);
        if (go == null) { Debug.Log("[GrassMounds] Nothing to clear."); return; }
        Undo.DestroyObjectImmediate(go);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    [MenuItem("Tools/Vegetation/Fill Tile 10")]
    public static void FillTile10()
    {
        Scene scene = SceneManager.GetActiveScene();
        var go = GameObject.Find("SM_Terrain_Ground_Mound_Large_01 (10)");
        if (go == null) { Debug.Log("[Fill10] tile not found"); return; }
        Collider tile = go.GetComponent<Collider>();
        if (tile == null) { Debug.Log("[Fill10] no collider"); return; }

        GameObject rootGo = Find(scene, RootName);
        if (rootGo == null) { rootGo = new GameObject(RootName); Undo.RegisterCreatedObjectUndo(rootGo, "Fill Tile 10"); }
        Transform existing = rootGo.transform.Find(go.name);
        if (existing != null) Undo.DestroyObjectImmediate(existing.gameObject);
        GameObject group = new GameObject(go.name);
        group.transform.SetParent(rootGo.transform);
        Undo.RegisterCreatedObjectUndo(group, "Fill Tile 10");

        Bounds b = tile.bounds;
        Vector3 center = b.center;
        float radius = Mathf.Max(b.extents.x, b.extents.z); // cover the whole elongated disc
        Random.InitState(99010);
        var placedP = new List<Vector3>();
        var placedT = new List<Vector3>();
        int t = PlaceCircle(Trees, TreesDir, 1, TreeSpacing, true, tile, center, radius, placedT, group.transform);
        int p = PlaceCircle(Plants, PlantsDir, 80, 1.0f, false, tile, center, radius, placedP, group.transform);
        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = group;
        Debug.Log($"[Fill10] placed {p} plants + {t} trees on the open area of (10).");
    }



    static GameObject Find(Scene scene, string name)
    {
        foreach (GameObject go in scene.GetRootGameObjects())
            if (go.name == name) return go;
        return null;
    }
}
#endif
