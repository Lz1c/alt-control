using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

/// <summary>
/// Editor tooling for the camera HUD (top bar + bottom bar) in the working scene.
/// All menu items live under <c>Tools/Camera HUD/</c>. Each is idempotent.
///
/// Typical workflow when you change something:
/// 1. Edit pixel shapes / icon sizes → run <c>Generate Icons</c>
/// 2. Switch font / re-tune pixel-LCD look → run <c>Generate Font Asset</c>
///    (it also re-applies TMP outline to all HUD TMPs automatically)
/// 3. Change outline width / color without regenerating font → run <c>Apply TMP Outline</c>
/// 4. After messy iteration, tidy hierarchy → run <c>Cleanup Hierarchy</c>
/// </summary>
public static class CameraHudTools
{
    private const string IconDir = "Assets/_Project/UI/Icons";
    private const string FontDir = "Assets/_Project/UI/Fonts";
    private const string FontTtfPath = FontDir + "/BarlowCondensed-Medium.ttf";
    private const string FontAssetPath = FontDir + "/BarlowCondensed-Medium SDF.asset";

    private const float TmpOutlineWidth = 0.2f;
    private static readonly Color TmpOutlineColor = Color.black;

    // ---------------------------------------------------------------- Icons

    [MenuItem("Tools/Camera HUD/Generate Icons")]
    public static void GenerateIcons()
    {
        Directory.CreateDirectory(IconDir);

        SaveTexture(BuildShotsCard(), "shots_card.png");
        SaveTexture(BuildBatteryCharge(), "battery_charge.png");
        SaveTexture(BuildEvChecker(), "ev_checker.png");

        AssetDatabase.Refresh();
        ApplyIconImportSettings();
        Debug.Log("[CameraHudTools] Generated 3 HUD icons -> " + IconDir);
    }

    private static Texture2D BuildShotsCard()
    {
        const int W = 28, H = 16;
        Texture2D tex = NewTransparent(W, H);
        FillRect(tex, 26, 0, 2, H, Color.white);
        FillRect(tex, 0, 0, W, 2, Color.white);
        FillRect(tex, 5, H - 2, W - 7, 2, Color.white);
        FillRect(tex, 0, 0, 2, 11, Color.white);
        DrawLineThick(tex, 0, 10, 5, 15, Color.white);
        FillRect(tex, 5, 8, W - 11, 1, Color.white);
        tex.Apply();
        return tex;
    }

    private static Texture2D BuildBatteryCharge()
    {
        const int W = 32, H = 16, MainW = 30;
        Texture2D tex = NewTransparent(W, H);
        FillRect(tex, 0, H - 2, MainW, 2, Color.white);
        FillRect(tex, 0, 0, MainW, 2, Color.white);
        FillRect(tex, 0, 0, 2, H, Color.white);
        FillRect(tex, MainW - 2, 0, 2, H, Color.white);
        FillRect(tex, MainW, 5, 2, 6, Color.white);
        DrawLineThick(tex, 7, 3, 11, 12, Color.white);
        DrawLineThick(tex, 16, 3, 20, 12, Color.white);
        tex.Apply();
        return tex;
    }

    private static Texture2D BuildEvChecker()
    {
        const int W = 14, H = 14;
        Texture2D tex = NewTransparent(W, H);
        FillRect(tex, 0, H - 1, W, 1, Color.white);
        FillRect(tex, 0, 0, W, 1, Color.white);
        FillRect(tex, 0, 0, 1, H, Color.white);
        FillRect(tex, W - 1, 0, 1, H, Color.white);
        FillRect(tex, 1, 7, 6, 6, Color.white);
        FillRect(tex, 7, 1, 6, 6, Color.white);
        tex.Apply();
        return tex;
    }

    // ---------------------------------------------------------------- Font

    [MenuItem("Tools/Camera HUD/Generate Font Asset")]
    public static void GenerateFontAsset()
    {
        Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(FontTtfPath);
        if (!sourceFont)
        {
            Debug.LogError($"[CameraHudTools] TTF not found at {FontTtfPath}");
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath) != null)
        {
            AssetDatabase.DeleteAsset(FontAssetPath);
        }

        // Low sampling + small atlas + Point filter = chunky Sony-LCD pixel feel.
        TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
            sourceFont,
            32,
            2,
            GlyphRenderMode.SDFAA,
            512,
            512,
            AtlasPopulationMode.Dynamic
        );

        if (!fontAsset)
        {
            Debug.LogError("[CameraHudTools] CreateFontAsset returned null");
            return;
        }

        if (fontAsset.atlasTexture)
        {
            fontAsset.atlasTexture.filterMode = FilterMode.Point;
        }

        AssetDatabase.CreateAsset(fontAsset, FontAssetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        int rebound = RebindHudTextMeshesAndOutline(fontAsset);
        Debug.Log($"[CameraHudTools] Pixel-LCD SDF font asset at {FontAssetPath}, rebound {rebound} TMPs");
    }

    // ---------------------------------------------------------------- Outline

    [MenuItem("Tools/Camera HUD/Apply TMP Outline")]
    public static void ApplyTmpOutline()
    {
        int count = 0;
        foreach (TMP_Text tmp in EnumerateHudTextMeshes())
        {
            ApplyOutlineToTmp(tmp);
            count++;
        }
        Debug.Log($"[CameraHudTools] Outline applied to {count} TMPs");
    }

    private static void ApplyOutlineToTmp(TMP_Text tmp)
    {
        Material mat = tmp.fontMaterial;
        if (mat)
        {
            mat.SetFloat(ShaderUtilities.ID_OutlineWidth, TmpOutlineWidth);
            mat.SetColor(ShaderUtilities.ID_OutlineColor, TmpOutlineColor);
        }
        tmp.UpdateMeshPadding();
        tmp.SetVerticesDirty();
        tmp.SetMaterialDirty();
        EditorUtility.SetDirty(tmp);
    }

    private static int RebindHudTextMeshesAndOutline(TMP_FontAsset newFontAsset)
    {
        int count = 0;
        foreach (TMP_Text tmp in EnumerateHudTextMeshes())
        {
            tmp.font = newFontAsset;
            if (newFontAsset.material)
            {
                tmp.fontSharedMaterial = newFontAsset.material;
            }
            ApplyOutlineToTmp(tmp);
            count++;
        }
        return count;
    }

    // ---------------------------------------------------------------- Cleanup

    [MenuItem("Tools/Camera HUD/Cleanup Hierarchy")]
    public static void CleanupHierarchy()
    {
        int renames = 0, removedOutlines = 0, reordered = 0, rebound = 0;

        // 1. Idempotent rename of legacy "Image" / "Image (1)" photo overlays.
        GameObject canvas = GameObject.Find("Canvas");
        if (canvas)
        {
            foreach (Transform child in canvas.transform)
            {
                if (child.name == "Image")
                {
                    child.name = "PhotoPreview";
                    renames++;
                }
                else if (child.name == "Image (1)")
                {
                    child.name = "ShutterBlackout";
                    renames++;
                }
            }
        }

        // 2. Strip dead UnityEngine.UI.Outline from HUD TMPs (TMP doesn't honor it;
        //    real outline lives on the font material via Apply TMP Outline).
        foreach (TMP_Text tmp in EnumerateHudTextMeshes())
        {
            Outline outline = tmp.GetComponent<Outline>();
            if (outline)
            {
                Object.DestroyImmediate(outline);
                removedOutlines++;
            }
        }

        // 3. Reorder CameraHUDTop children to logical (Icon, Text) pairs.
        GameObject hudTop = GameObject.Find("CameraHUDTop");
        if (hudTop)
        {
            string[] order =
            {
                "ShotsIcon", "ShotsText",
                "AspectFrame", "AspectText",
                "FormatFrame", "FormatText",
                "BatteryIcon", "BatteryText"
            };
            for (int i = 0; i < order.Length; i++)
            {
                Transform child = hudTop.transform.Find(order[i]);
                if (child && child.GetSiblingIndex() != i)
                {
                    child.SetSiblingIndex(i);
                    reordered++;
                }
            }
        }

        // 4. Repair any TMP whose fontSharedMaterial dangles (happens after
        //    Generate Font Asset deletes the prior asset's sub-material while
        //    serialized scene references still pointed at it).
        TMP_FontAsset currentFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        if (currentFont)
        {
            foreach (TMP_Text tmp in EnumerateHudTextMeshes())
            {
                bool fontWrong = tmp.font != currentFont;
                bool matMissing = tmp.fontSharedMaterial == null
                                  || tmp.fontSharedMaterial != currentFont.material;
                if (fontWrong || matMissing)
                {
                    tmp.font = currentFont;
                    if (currentFont.material)
                    {
                        tmp.fontSharedMaterial = currentFont.material;
                    }
                    ApplyOutlineToTmp(tmp);
                    rebound++;
                }
            }
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"[CameraHudTools] Cleanup: {renames} renames, {removedOutlines} dead Outlines removed, {reordered} siblings reordered, {rebound} TMPs rebound");
    }

    // ---------------------------------------------------------------- Helpers

    private static System.Collections.Generic.IEnumerable<TMP_Text> EnumerateHudTextMeshes()
    {
        foreach (string rootName in new[] { "CameraHUD", "CameraHUDTop" })
        {
            GameObject root = GameObject.Find(rootName);
            if (!root) continue;

            foreach (TMP_Text tmp in root.GetComponentsInChildren<TMP_Text>(true))
            {
                yield return tmp;
            }
        }
    }

    private static Texture2D NewTransparent(int w, int h)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        var pixels = new Color32[w * h];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = new Color32(255, 255, 255, 0);
        }
        tex.SetPixels32(pixels);
        return tex;
    }

    private static void FillRect(Texture2D tex, int x, int y, int w, int h, Color color)
    {
        for (int dy = 0; dy < h; dy++)
        {
            for (int dx = 0; dx < w; dx++)
            {
                int px = x + dx;
                int py = y + dy;
                if (px >= 0 && px < tex.width && py >= 0 && py < tex.height)
                {
                    tex.SetPixel(px, py, color);
                }
            }
        }
    }

    private static void DrawLineThick(Texture2D tex, int x0, int y0, int x1, int y1, Color color)
    {
        DrawLine(tex, x0, y0, x1, y1, color);
        DrawLine(tex, x0 + 1, y0, x1 + 1, y1, color);
    }

    private static void DrawLine(Texture2D tex, int x0, int y0, int x1, int y1, Color color)
    {
        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        int safety = (dx + dy) * 2 + 4;
        while (safety-- > 0)
        {
            if (x0 >= 0 && x0 < tex.width && y0 >= 0 && y0 < tex.height)
            {
                tex.SetPixel(x0, y0, color);
            }
            if (x0 == x1 && y0 == y1) break;
            int e2 = err * 2;
            if (e2 > -dy) { err -= dy; x0 += sx; }
            if (e2 < dx) { err += dx; y0 += sy; }
        }
    }

    private static void SaveTexture(Texture2D tex, string filename)
    {
        string path = Path.Combine(IconDir, filename);
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    private static void ApplyIconImportSettings()
    {
        string[] guids = AssetDatabase.FindAssets("t:texture2D", new[] { IconDir });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (AssetImporter.GetAtPath(path) is TextureImporter importer)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.filterMode = FilterMode.Point;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
        }
    }
}
