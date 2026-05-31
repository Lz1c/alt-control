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
        SaveTexture(BuildEvScale(), "ev_scale.png");
        SaveTexture(BuildEvPointer(), "ev_pointer.png");
        SaveTexture(BuildAFBracket(), "af_bracket.png");
        SaveTexture(BuildVignette(), "vignette.png");
        SaveTexture(BuildFocalScale(), "focal_scale.png");
        SaveTexture(BuildFocalPointer(), "focal_pointer.png");
        SaveTexture(BuildFocusScale(), "focus_scale.png");
        SaveTexture(BuildFocusPointer(), "focus_pointer.png");
        SaveTexture(BuildFocusBracket(), "focus_bracket.png");

        AssetDatabase.Refresh();
        ApplyIconImportSettings();

        // The in-focus bracket must stretch VERTICALLY (vertical focus scale), so re-import it
        // 9-sliced with a top/bottom border. ApplyIconImportSettings above forces Single mode
        // with no border, so this targeted fix-up must run AFTER it.
        if (AssetImporter.GetAtPath(IconDir + "/focus_bracket.png") is TextureImporter bracketImporter)
        {
            bracketImporter.spriteBorder = new Vector4(0f, 6f, 0f, 6f); // L, B, R, T
            bracketImporter.SaveAndReimport();
        }

        Debug.Log("[CameraHudTools] Generated 11 HUD icons -> " + IconDir);
    }

    // ---------------------------------------------------------------- Lens scale sprites

    // Vertical focal-length zoom bar (W bottom → T top), 12×160. Right baseline column +
    // major ticks (7px) at log positions of common focal lengths, minor ticks (4px) between.
    // Tick positions use the SAME log map as the runtime pointer (focal range 16–300 mm).
    private static Texture2D BuildFocalScale()
    {
        const int W = 12, H = 160;
        const float Min = 16f, Max = 300f;
        Texture2D tex = NewTransparent(W, H);
        FillRect(tex, W - 1, 0, 1, H, Color.white); // right baseline

        float[] majors = { 16f, 24f, 35f, 50f, 70f, 105f, 150f, 200f, 300f };
        foreach (float mm in majors)
        {
            FillRect(tex, W - 8, TickPos(mm, Min, Max, H), 7, 1, Color.white);
        }

        float[] minors = { 20f, 28f, 40f, 60f, 85f, 135f, 250f };
        foreach (float mm in minors)
        {
            FillRect(tex, W - 5, TickPos(mm, Min, Max, H), 4, 1, Color.white);
        }

        tex.Apply();
        return tex;
    }

    // Vertical focus-distance ruler (near 0.5 bottom → ∞ top), 12×160. LEFT baseline + horizontal
    // ticks (mirror of the focal bar). Log map 0.5–1000 m, same as the runtime pointer.
    private static Texture2D BuildFocusScale()
    {
        const int W = 12, H = 160;
        const float Min = 0.5f, Max = 1000f;
        Texture2D tex = NewTransparent(W, H);
        FillRect(tex, 0, 0, 1, H, Color.white); // left baseline

        float[] majors = { 0.5f, 1f, 2f, 5f, 10f, 50f, 1000f };
        foreach (float m in majors)
        {
            FillRect(tex, 1, TickPos(m, Min, Max, H), 7, 1, Color.white);
        }

        float[] minors = { 0.7f, 1.5f, 3f, 7f, 20f, 100f, 300f };
        foreach (float m in minors)
        {
            FillRect(tex, 1, TickPos(m, Min, Max, H), 4, 1, Color.white);
        }

        tex.Apply();
        return tex;
    }

    private static int TickPos(float value, float min, float max, int span)
    {
        return Mathf.Clamp(Mathf.RoundToInt(CAMCOLCameraSettingsTMPDisplay.NormalizeLog(value, min, max) * (span - 1)), 0, span - 1);
    }

    // 6×10 right-pointing triangle ▶ that sits to the LEFT of the focal bar and slides in Y.
    private static Texture2D BuildFocalPointer()
    {
        const int W = 6, H = 10;
        Texture2D tex = NewTransparent(W, H);
        FillRect(tex, 0, 0, 1, 10, Color.white);
        FillRect(tex, 1, 1, 1, 8, Color.white);
        FillRect(tex, 2, 2, 1, 6, Color.white);
        FillRect(tex, 3, 3, 1, 4, Color.white);
        FillRect(tex, 4, 4, 1, 2, Color.white); // apex points right, into the bar
        tex.Apply();
        return tex;
    }

    // 6×10 left-pointing triangle ◀ that sits to the RIGHT of the focus bar and slides in Y.
    private static Texture2D BuildFocusPointer()
    {
        const int W = 6, H = 10;
        Texture2D tex = NewTransparent(W, H);
        FillRect(tex, 5, 0, 1, 10, Color.white);
        FillRect(tex, 4, 1, 1, 8, Color.white);
        FillRect(tex, 3, 2, 1, 6, Color.white);
        FillRect(tex, 2, 3, 1, 4, Color.white);
        FillRect(tex, 1, 4, 1, 2, Color.white); // apex points left, into the bar
        tex.Apply();
        return tex;
    }

    // Stretchable VERTICAL in-focus bracket, 12×16. Top + bottom caps with small inward feet +
    // a thin vertical connector. Imported with a 6px top/bottom border (see GenerateIcons) so the
    // middle stretches in height and the caps stay crisp.
    private static Texture2D BuildFocusBracket()
    {
        const int W = 12, H = 16;
        Texture2D tex = NewTransparent(W, H);

        // bottom cap + feet
        FillRect(tex, 0, 0, W, 2, Color.white);
        FillRect(tex, 0, 2, 3, 1, Color.white);
        FillRect(tex, W - 3, 2, 3, 1, Color.white);
        // top cap + feet
        FillRect(tex, 0, H - 2, W, 2, Color.white);
        FillRect(tex, 0, H - 3, 3, 1, Color.white);
        FillRect(tex, W - 3, H - 3, 3, 1, Color.white);
        // mid connector (stretches with the sliced middle)
        FillRect(tex, W / 2, 5, 1, H - 10, Color.white);

        tex.Apply();
        return tex;
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

    // Sony-style EV scale: 7 long integer ticks (EV -3..+3) + 6 short half-stop
    // ticks + a top baseline. EV=0 is double-wide for emphasis. Width 120 → each
    // integer step = 20 px (matches the pointer math in CAMCOLCameraSettingsTMPDisplay).
    private static Texture2D BuildEvScale()
    {
        const int W = 120, H = 12;
        Texture2D tex = NewTransparent(W, H);

        FillRect(tex, 0, 8, W, 1, Color.white);

        for (int i = 0; i <= 6; i++)
        {
            int x = Mathf.RoundToInt(i * (W - 1) / 6f);
            if (i == 3)
            {
                FillRect(tex, x - 1, 0, 2, 9, Color.white);
            }
            else
            {
                FillRect(tex, x, 0, 1, 8, Color.white);
            }
        }

        for (int i = 0; i < 6; i++)
        {
            int x = Mathf.RoundToInt((i + 0.5f) * (W - 1) / 6f);
            FillRect(tex, x, 0, 1, 4, Color.white);
        }

        tex.Apply();
        return tex;
    }

    // 7×5 downward triangle pointer that sits above the EV scale and slides on EV change.
    private static Texture2D BuildEvPointer()
    {
        const int W = 7, H = 5;
        Texture2D tex = NewTransparent(W, H);
        FillRect(tex, 0, 4, 7, 1, Color.white);
        FillRect(tex, 1, 3, 5, 1, Color.white);
        FillRect(tex, 2, 2, 3, 1, Color.white);
        FillRect(tex, 3, 1, 1, 1, Color.white);
        tex.Apply();
        return tex;
    }

    // Sony-style AF bracket: 80×60 transparent, 4 corner L-shapes (12 px arm, 2 px thick).
    private static Texture2D BuildAFBracket()
    {
        const int W = 80, H = 60, Arm = 12, Thick = 2;
        Texture2D tex = NewTransparent(W, H);

        // Top-left
        FillRect(tex, 0, H - Thick, Arm, Thick, Color.white);
        FillRect(tex, 0, H - Arm, Thick, Arm, Color.white);
        // Top-right
        FillRect(tex, W - Arm, H - Thick, Arm, Thick, Color.white);
        FillRect(tex, W - Thick, H - Arm, Thick, Arm, Color.white);
        // Bottom-left
        FillRect(tex, 0, 0, Arm, Thick, Color.white);
        FillRect(tex, 0, 0, Thick, Arm, Color.white);
        // Bottom-right
        FillRect(tex, W - Arm, 0, Arm, Thick, Color.white);
        FillRect(tex, W - Thick, 0, Thick, Arm, Color.white);

        tex.Apply();
        return tex;
    }

    // Radial vignette 256×256: transparent center, edges fade to alpha ~160 black.
    // SmoothStep at d∈[0.4, 1.0] keeps the inner ~40% completely clear.
    private static Texture2D BuildVignette()
    {
        const int W = 256, H = 256;
        Texture2D tex = NewTransparent(W, H);
        Vector2 center = new Vector2((W - 1) * 0.5f, (H - 1) * 0.5f);
        float maxDist = center.x;

        Color32[] pixels = new Color32[W * H];
        for (int y = 0; y < H; y++)
        {
            for (int x = 0; x < W; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), center) / maxDist;
                float t = Mathf.SmoothStep(0.4f, 1.0f, d);
                byte a = (byte)Mathf.Clamp(Mathf.RoundToInt(t * 160f), 0, 255);
                pixels[y * W + x] = new Color32(0, 0, 0, a);
            }
        }
        tex.SetPixels32(pixels);
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

        // CreateFontAsset spawns a sub-material and atlas texture in memory but
        // CreateAsset only persists fontAsset itself. Without explicitly adding the
        // material and atlas as sub-assets, the references dangle after a reload,
        // breaking TMP_BaseEditorPanel.GetMaterialPresets / DrawFont.
        if (fontAsset.material)
        {
            fontAsset.material.name = "Material";
            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
        }
        if (fontAsset.atlasTexture)
        {
            fontAsset.atlasTexture.name = "Font Atlas";
            AssetDatabase.AddObjectToAsset(fontAsset.atlasTexture, fontAsset);
        }

        EditorUtility.SetDirty(fontAsset);
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(FontAssetPath, ImportAssetOptions.ForceUpdate);

        // Reload from disk so subsequent rebinds reference the persisted sub-assets,
        // not the transient in-memory ones.
        fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);

        int rebound = RebindHudTextMeshesAndOutline(fontAsset);
        Debug.Log($"[CameraHudTools] Pixel-LCD SDF font asset at {FontAssetPath}, rebound {rebound} TMPs");
    }

    // ---------------------------------------------------------------- Viewfinder

    private const string ViewfinderRootName = "ViewfinderOverlay";
    private const string VignettePath = IconDir + "/vignette.png";
    private const string AFBracketPath = IconDir + "/af_bracket.png";

    // Grid line color = white with alpha 0.30 (rule-of-thirds, no center horizon line).
    private static readonly Color GridLineColor = new Color(1f, 1f, 1f, 0.30f);

    // Reference resolution: Canvas scaleFactor 2.4 → UI-space 800×450.
    // 9-grid third lines at x=±133, y=±75; AF bracket 80×60 centered.
    private const float UiWidth = 800f;
    private const float UiHeight = 450f;
    private const float ThirdX = UiWidth / 6f;   // ≈133
    private const float ThirdY = UiHeight / 6f;  // =75
    private const float AFBracketWidth = 80f;
    private const float AFBracketHeight = 60f;

    [MenuItem("Tools/Camera HUD/Setup Viewfinder Overlay")]
    public static void SetupViewfinderOverlay()
    {
        GameObject canvas = GameObject.Find("Canvas");
        if (!canvas)
        {
            Debug.LogError("[CameraHudTools] Canvas not found in active scene");
            return;
        }

        // Idempotent: nuke and re-create.
        Transform existing = canvas.transform.Find(ViewfinderRootName);
        if (existing)
        {
            Object.DestroyImmediate(existing.gameObject);
        }

        Sprite vignetteSprite = AssetDatabase.LoadAssetAtPath<Sprite>(VignettePath);
        Sprite afSprite = AssetDatabase.LoadAssetAtPath<Sprite>(AFBracketPath);
        if (!vignetteSprite || !afSprite)
        {
            Debug.LogError($"[CameraHudTools] Missing sprites — run Generate Icons first ({VignettePath}, {AFBracketPath})");
            return;
        }

        GameObject root = new GameObject(ViewfinderRootName, typeof(RectTransform));
        root.layer = LayerMask.NameToLayer("UI");
        root.transform.SetParent(canvas.transform, false);
        StretchFull((RectTransform)root.transform);
        root.transform.SetAsFirstSibling();

        // Vignette (full-screen radial)
        GameObject vignette = NewUIChild(root.transform, "Vignette");
        StretchFull((RectTransform)vignette.transform);
        Image vimg = vignette.AddComponent<Image>();
        vimg.sprite = vignetteSprite;
        vimg.raycastTarget = false;

        // GridOverlay (container + 5 line children)
        GameObject grid = NewUIChild(root.transform, "GridOverlay");
        StretchFull((RectTransform)grid.transform);
        int lines = 0;
        lines += AddLine(grid.transform, "VLine_L", -ThirdX, 0f, 1f, UiHeight, GridLineColor) ? 1 : 0;
        lines += AddLine(grid.transform, "VLine_R", +ThirdX, 0f, 1f, UiHeight, GridLineColor) ? 1 : 0;
        lines += AddLine(grid.transform, "HLine_T", 0f, +ThirdY, UiWidth, 1f, GridLineColor) ? 1 : 0;
        lines += AddLine(grid.transform, "HLine_B", 0f, -ThirdY, UiWidth, 1f, GridLineColor) ? 1 : 0;

        // AFBracket (centered)
        GameObject af = NewUIChild(root.transform, "AFBracket");
        RectTransform afRT = (RectTransform)af.transform;
        afRT.anchorMin = afRT.anchorMax = new Vector2(0.5f, 0.5f);
        afRT.pivot = new Vector2(0.5f, 0.5f);
        afRT.anchoredPosition = Vector2.zero;
        afRT.sizeDelta = new Vector2(AFBracketWidth, AFBracketHeight);
        Image afImg = af.AddComponent<Image>();
        afImg.sprite = afSprite;
        afImg.raycastTarget = false;

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"[CameraHudTools] Viewfinder overlay built: vignette + {lines} grid lines + AF bracket");
    }

    private static GameObject NewUIChild(Transform parent, string name)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);
        return go;
    }

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;
    }

    private static bool AddLine(Transform parent, string name, float x, float y, float w, float h, Color color)
    {
        GameObject go = NewUIChild(parent, name);
        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);
        Image img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return true;
    }

    // ---------------------------------------------------------------- Lens Scales

    private const string LensRootName = "LensScales";
    private const string FocalScalePath = IconDir + "/focal_scale.png";
    private const string FocalPointerPath = IconDir + "/focal_pointer.png";
    private const string FocusScalePath = IconDir + "/focus_scale.png";
    private const string FocusPointerPath = IconDir + "/focus_pointer.png";
    private const string FocusBracketPath = IconDir + "/focus_bracket.png";

    // Focal log range (mm) and Focus log range (m) — pushed into the driver by SetupLensScales so
    // tick labels and sprite ticks line up with the driven pointer regardless of serialized state.
    private const float FocalMin = 16f, FocalMax = 300f;
    private const float FocusMin = 0.5f, FocusMax = 1000f;
    private const float BarWidth = 12f;     // both vertical bars
    private const float BarHeight = 160f;   // matches focal/focusScaleTravel
    private const float LensTravel = 160f;  // matches focal/focusScaleTravel

    // Builds the Sony-style lens indicators: two vertical bars on the right edge, side by side —
    // focal-length zoom (W↔T) on the outer column and focus distance (near↔∞, with an in-focus
    // bracket) on the inner column. Idempotent: destroys+recreates the LensScales root, then
    // re-wires the driver each run.
    [MenuItem("Tools/Camera HUD/Setup Lens Scales")]
    public static void SetupLensScales()
    {
        GameObject canvas = GameObject.Find("Canvas");
        if (!canvas)
        {
            Debug.LogError("[CameraHudTools] Canvas not found in active scene");
            return;
        }

        Sprite focalBar = AssetDatabase.LoadAssetAtPath<Sprite>(FocalScalePath);
        Sprite focalPointer = AssetDatabase.LoadAssetAtPath<Sprite>(FocalPointerPath);
        Sprite focusBar = AssetDatabase.LoadAssetAtPath<Sprite>(FocusScalePath);
        Sprite focusBracket = AssetDatabase.LoadAssetAtPath<Sprite>(FocusBracketPath);
        Sprite focusPointer = AssetDatabase.LoadAssetAtPath<Sprite>(FocusPointerPath);
        if (!focalBar || !focalPointer || !focusBar || !focusBracket || !focusPointer)
        {
            Debug.LogError("[CameraHudTools] Missing lens-scale sprites — run Tools/Camera HUD/Generate Icons first");
            return;
        }

        CAMCOLCameraSettingsTMPDisplay display = canvas.GetComponentInChildren<CAMCOLCameraSettingsTMPDisplay>(true);
        // Parent the widgets under the Canvas (full 800×450 space), NOT under the driver's
        // GameObject — the driver lives on the bottom HUD bar, which is only an 800×60 strip
        // and would squash/clip the vertical bars.
        Transform host = canvas.transform;

        Transform existing = host.Find(LensRootName);
        if (!existing)
        {
            GameObject stray = GameObject.Find(LensRootName); // catch a root left elsewhere by an older run
            if (stray)
            {
                existing = stray.transform;
            }
        }
        if (existing)
        {
            Object.DestroyImmediate(existing.gameObject);
        }

        GameObject root = NewUIChild(host, LensRootName);
        StretchFull((RectTransform)root.transform);

        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);

        Vector2 barSize = new Vector2(BarWidth, BarHeight);
        Vector2 pointerSize = new Vector2(6f, 10f);
        float valueY = BarHeight * 0.5f + 18f;

        // ---- Focal zoom bar (outer / rightmost column) ----
        // Bar centered on the group origin; pointer ▶ on its LEFT; T/W + value labels on its RIGHT.
        GameObject focal = NewUIChild(root.transform, "FocalScale");
        SetGroupRect((RectTransform)focal.transform, new Vector2(1f, 0.5f), new Vector2(-46f, 0f), new Vector2(60f, 200f));

        AddImage(focal.transform, "Bar", focalBar, Vector2.zero, barSize, false);
        GameObject focalPtr = AddImage(focal.transform, "Pointer", focalPointer, new Vector2(-11f, 0f), pointerSize, false);
        GameObject focalVal = AddLabel(focal.transform, "Value", "50mm", font, new Vector2(0f, valueY), 15f);
        AddLabel(focal.transform, "LabelT", "T", font, new Vector2(14f, BarHeight * 0.5f - 10f), 12f);
        AddLabel(focal.transform, "LabelW", "W", font, new Vector2(14f, -BarHeight * 0.5f + 10f), 12f);

        // ---- Focus distance bar (inner column, left of focal) ----
        // Bar centered; pointer ◀ on its RIGHT; distance tick labels on its LEFT; bracket over the bar.
        GameObject focus = NewUIChild(root.transform, "FocusScale");
        SetGroupRect((RectTransform)focus.transform, new Vector2(1f, 0.5f), new Vector2(-120f, 0f), new Vector2(60f, 200f));

        AddImage(focus.transform, "Track", focusBar, Vector2.zero, barSize, false);
        GameObject bracket = AddImage(focus.transform, "Bracket", focusBracket, Vector2.zero, new Vector2(BarWidth + 4f, 16f), true);
        GameObject focusPtr = AddImage(focus.transform, "Pointer", focusPointer, new Vector2(11f, 0f), pointerSize, false);
        GameObject focusVal = AddLabel(focus.transform, "Value", "10m", font, new Vector2(0f, valueY), 15f);

        string[] tickLabels = { "0.5", "1", "2", "5", "10", "∞" };
        float[] tickValues = { 0.5f, 1f, 2f, 5f, 10f, 1000f };
        for (int i = 0; i < tickLabels.Length; i++)
        {
            float t = CAMCOLCameraSettingsTMPDisplay.NormalizeLog(tickValues[i], FocusMin, FocusMax);
            float y = (t - 0.5f) * LensTravel;
            AddLabel(focus.transform, "Tick_" + i, tickLabels[i], font, new Vector2(-15f, y), 11f);
        }

        // ---- Wire the created widgets into the driver ----
        // Also push the geometry (travel + log ranges) so the driver's pointer math stays locked
        // to the sprite the menu just built — never trust a stale serialized travel/range from an
        // earlier run (changing a field's default does NOT update already-serialized values).
        if (display)
        {
            SerializedObject so = new SerializedObject(display);
            SetRef(so, "focalScalePointer", focalPtr.GetComponent<RectTransform>());
            SetRef(so, "focusScalePointer", focusPtr.GetComponent<RectTransform>());
            SetRef(so, "focusClearBracket", bracket.GetComponent<RectTransform>());
            SetRef(so, "focalScaleValueText", focalVal.GetComponent<TMP_Text>());
            SetRef(so, "focusScaleValueText", focusVal.GetComponent<TMP_Text>());
            SetFloat(so, "focalScaleTravel", LensTravel);
            SetFloat(so, "focusScaleTravel", LensTravel);
            SetVec2(so, "focalDisplayRange", new Vector2(FocalMin, FocalMax));
            SetVec2(so, "focusDisplayRange", new Vector2(FocusMin, FocusMax));
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(display);
        }
        else
        {
            Debug.LogWarning("[CameraHudTools] CAMCOLCameraSettingsTMPDisplay not found — lens-scale refs were NOT wired");
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"[CameraHudTools] Lens scales built under {host.name}/{LensRootName}{(display ? " and wired" : " (NOT wired)")}");
    }

    private static void SetGroupRect(RectTransform rt, Vector2 anchor, Vector2 anchoredPos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = anchoredPos;
        rt.localScale = Vector3.one;
    }

    // Child element centered on its parent's origin; the centered (t-0.5)*travel pointer math
    // depends on bar/pointer sharing this origin, so everything uses anchor=pivot=(0.5,0.5).
    private static GameObject AddImage(Transform parent, string name, Sprite sprite, Vector2 pos, Vector2 size, bool sliced)
    {
        GameObject go = NewUIChild(parent, name);
        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
        Image img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.raycastTarget = false;
        if (sliced)
        {
            img.type = Image.Type.Sliced;
            img.fillCenter = true;
        }
        return go;
    }

    private static GameObject AddLabel(Transform parent, string name, string text, TMP_FontAsset font, Vector2 pos, float fontSize)
    {
        GameObject go = NewUIChild(parent, name);
        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(56f, 20f);
        rt.anchoredPosition = pos;

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = fontSize;
        tmp.enableAutoSizing = false;
        tmp.raycastTarget = false;
        tmp.color = Color.white;
        if (font)
        {
            tmp.font = font;
            if (font.material)
            {
                tmp.fontSharedMaterial = font.material;
            }
        }
        return go;
    }

    private static void SetRef(SerializedObject so, string propertyName, Object value)
    {
        SerializedProperty p = so.FindProperty(propertyName);
        if (p != null)
        {
            p.objectReferenceValue = value;
        }
        else
        {
            Debug.LogWarning("[CameraHudTools] Driver field not found, not wired: " + propertyName);
        }
    }

    private static void SetFloat(SerializedObject so, string propertyName, float value)
    {
        SerializedProperty p = so.FindProperty(propertyName);
        if (p != null)
        {
            p.floatValue = value;
        }
    }

    private static void SetVec2(SerializedObject so, string propertyName, Vector2 value)
    {
        SerializedProperty p = so.FindProperty(propertyName);
        if (p != null)
        {
            p.vector2Value = value;
        }
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
        foreach (string rootName in new[] { "CameraHUD", "CameraHUDTop", "LensScales" })
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
