using TMPro;
using UnityEngine;

/// <summary>
/// Drives the Sony-style camera HUD (top + bottom rows on the working scene's Canvas).
///
/// Wiring (set in Inspector):
///   • <see cref="settings"/>      → <see cref="CAMCOLCameraSettings"/> source of truth.
///   • <see cref="referenceCamera"/> → resolves live aspect ratio (falls back to <see cref="Camera.main"/>).
///   • Eight TMP refs map 1:1 to HUD GameObjects under Canvas/CameraHUD and Canvas/CameraHUDTop.
///
/// Output:
///   Bottom row (live from CAMCOLCameraSettings):   ShutterText, ApertureText, EVText, ISOText
///   Top row    (status — derived or designer-set): ShotsText (decrements on PhotoSaved event),
///                                                  AspectText (auto from camera aspect),
///                                                  FormatText ("RAW" by default),
///                                                  BatteryText (manual %)
///
/// Aspect snap table:
///   1:1, 4:3, 3:2, 16:9, 21:9 (tolerance 0.03). Outside → GCD-reduced "W:H".
///
/// Visual assets — regenerate via Unity menu when changed:
///   Tools/Camera HUD/Generate Icons        → 3 PNGs in Assets/_Project/UI/Icons/
///   Tools/Camera HUD/Generate Font Asset   → BarlowCondensed-Medium SDF.asset (pixel-LCD style)
///   Tools/Camera HUD/Apply TMP Outline     → re-applies 0.2 black outline to all 8 TMPs
///   Tools/Camera HUD/Cleanup Hierarchy     → idempotent: renames, strips dead UI.Outline, reorders siblings
///
/// To add a new HUD field: add a TMP_Text serialized ref, add a Format helper, add change-detection in
/// the appropriate RefreshXxx method. Designer wires the TMP in the Inspector — no code changes elsewhere.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public class CAMCOLCameraSettingsTMPDisplay : MonoBehaviour
{
    private const int LensTickCount = 6;

    [Header("References")]
    [SerializeField] private CAMCOLCameraSettings settings;
    [Tooltip("Camera used to resolve the live aspect ratio. Falls back to Camera.main if unset.")]
    [SerializeField] private Camera referenceCamera;

    [Header("Exposure Text Outputs (bottom HUD)")]
    [SerializeField] private TMP_Text isoText;
    [SerializeField] private TMP_Text shutterSpeedText;
    [SerializeField] private TMP_Text apertureText;
    [SerializeField] private TMP_Text focalLengthText;
    [SerializeField] private TMP_Text focusDistanceText;
    [SerializeField] private TMP_Text focusClearRangeText;
    [SerializeField] private TMP_Text exposureCompensationText;

    [Header("EV Scale Pointer")]
    [Tooltip("Triangle child of EVScale; X is driven by current EV. Scale spans 120 px → 20 px per EV step.")]
    [SerializeField] private RectTransform evScalePointer;
    [SerializeField] private float evScalePixelsPerStop = 20f;
    [SerializeField] private float evScaleRangeStops = 3f;

    [Header("Lens Scales (Focal / Focus)")]
    [Tooltip("Vertical zoom-bar pointer. Y is driven by focal length (mm), W bottom → T top. Built by Tools/Camera HUD/Setup Lens Scales.")]
    [SerializeField] private RectTransform focalScalePointer;
    [Tooltip("Optional TMP that shows the current focal length (e.g. \"50mm\").")]
    [SerializeField] private TMP_Text focalScaleValueText;
    [Tooltip("Focal length display range in mm (log-mapped). Independent of the CAMCOLCameraSettings clamp limits; keep in sync with the baked sprite ticks.")]
    [SerializeField] private Vector2 focalDisplayRange = new Vector2(16f, 300f);
    [Tooltip("Total vertical pixel travel of the focal pointer along the bar.")]
    [SerializeField] private float focalScaleTravel = 160f;

    [Tooltip("Vertical distance-scale pointer. Y is driven by focus distance (m), near bottom → ∞ top.")]
    [SerializeField] private RectTransform focusScalePointer;
    [Tooltip("Stretchable in-focus bracket. Y + height are driven by focus distance and focus clear range.")]
    [SerializeField] private RectTransform focusClearBracket;
    [Tooltip("Optional TMP that shows the current focus distance (e.g. \"2.4m\").")]
    [SerializeField] private TMP_Text focusScaleValueText;
    [Tooltip("Focus distance display range in metres (log-mapped). Values above max park at the ∞ tick.")]
    [SerializeField] private Vector2 focusDisplayRange = new Vector2(0.5f, 1000f);
    [Tooltip("Total vertical pixel travel of the focus pointer along the scale.")]
    [SerializeField] private float focusScaleTravel = 160f;
    [Tooltip("Minimum on-screen width of the in-focus bracket, in pixels.")]
    [SerializeField] private float minBracketPixels = 6f;

    [Header("Status Text Outputs (top HUD)")]
    [SerializeField] private TMP_Text shotsRemainingText;
    [SerializeField] private TMP_Text aspectRatioText;
    [SerializeField] private TMP_Text fileFormatText;
    [SerializeField] private TMP_Text batteryText;

    [Header("Status Values")]
    [SerializeField, Min(0)] private int shotsRemaining = 1999;
    [SerializeField] private string fileFormat = "RAW";
    [SerializeField, Range(0f, 100f)] private float batteryPercent = 36f;

    private float lastIso = float.NaN;
    private float lastShutterSpeed = float.NaN;
    private float lastAperture = float.NaN;
    private float lastFocalLength = float.NaN;
    private float lastFocalLengthMin = float.NaN;
    private float lastFocalLengthMax = float.NaN;
    private float lastFocusDistance = float.NaN;
    private float lastFocusDistanceMin = float.NaN;
    private float lastFocusDistanceMax = float.NaN;
    private float lastFocusClearRange = float.NaN;
    private float lastEffectiveFocusNearDistance = float.NaN;
    private float lastEffectiveFocusFarDistance = float.NaN;
    private float lastExposureCompensation = float.NaN;

    private TMP_Text[] focalTickLabels;
    private TMP_Text[] focusTickLabels;

    private int lastShotsRemaining = int.MinValue;
    private string lastAspectRatio;
    private string lastFileFormat;
    private float lastBatteryPercent = float.NaN;

    public int ShotsRemaining
    {
        get => shotsRemaining;
        set
        {
            shotsRemaining = Mathf.Max(0, value);
            Refresh(true);
        }
    }

    public float BatteryPercent
    {
        get => batteryPercent;
        set
        {
            batteryPercent = Mathf.Clamp(value, 0f, 100f);
            Refresh(true);
        }
    }

    private void Reset()
    {
        settings = GetComponent<CAMCOLCameraSettings>();
    }

    private void OnEnable()
    {
        EnsureReferences();
        Refresh(true);
        CAMPhotoCapture.PhotoSaved += HandlePhotoSaved;
    }

    private void OnDisable()
    {
        CAMPhotoCapture.PhotoSaved -= HandlePhotoSaved;
    }

    private void HandlePhotoSaved()
    {
        if (shotsRemaining > 0)
        {
            shotsRemaining--;
            Refresh(true);
        }
    }

    private void Update()
    {
        Refresh(false);
    }

    private void OnValidate()
    {
        focalDisplayRange = SortRange(focalDisplayRange, 1f);
        focusDisplayRange = SortRange(focusDisplayRange, 0.1f);
        focalScaleTravel = Mathf.Max(1f, focalScaleTravel);
        focusScaleTravel = Mathf.Max(1f, focusScaleTravel);
        minBracketPixels = Mathf.Max(0f, minBracketPixels);
        EnsureReferences();
#if UNITY_EDITOR
        // Driving a RectTransform sizeDelta (the focus bracket) directly inside OnValidate trips
        // "SendMessage cannot be called during OnValidate". Defer one editor tick.
        if (!Application.isPlaying)
        {
            UnityEditor.EditorApplication.delayCall -= DeferredEditorRefresh;
            UnityEditor.EditorApplication.delayCall += DeferredEditorRefresh;
            return;
        }
#endif
        Refresh(true);
    }

#if UNITY_EDITOR
    private void DeferredEditorRefresh()
    {
        UnityEditor.EditorApplication.delayCall -= DeferredEditorRefresh;
        if (this == null)
        {
            return;
        }
        Refresh(true);
    }
#endif

    private static Vector2 SortRange(Vector2 range, float floor)
    {
        float min = Mathf.Max(floor, Mathf.Min(range.x, range.y));
        float max = Mathf.Max(min + 1e-3f, Mathf.Max(range.x, range.y));
        return new Vector2(min, max);
    }

    public void Refresh()
    {
        Refresh(true);
    }

    private void Refresh(bool force)
    {
        RefreshExposure(force);
        RefreshStatus(force);
    }

    private void RefreshExposure(bool force)
    {
        if (!settings)
        {
            SetText(isoText, string.Empty);
            SetText(shutterSpeedText, string.Empty);
            SetText(apertureText, string.Empty);
            SetText(focalLengthText, string.Empty);
            SetText(focusDistanceText, string.Empty);
            SetText(focusClearRangeText, string.Empty);
            SetText(exposureCompensationText, string.Empty);
            SetText(focalScaleValueText, string.Empty);
            SetText(focusScaleValueText, string.Empty);

            lastIso = float.NaN;
            lastShutterSpeed = float.NaN;
            lastAperture = float.NaN;
            lastFocalLength = float.NaN;
            lastFocalLengthMin = float.NaN;
            lastFocalLengthMax = float.NaN;
            lastFocusDistance = float.NaN;
            lastFocusDistanceMin = float.NaN;
            lastFocusDistanceMax = float.NaN;
            lastFocusClearRange = float.NaN;
            lastEffectiveFocusNearDistance = float.NaN;
            lastEffectiveFocusFarDistance = float.NaN;
            lastExposureCompensation = float.NaN;
            return;
        }

        float effectiveFocusNearDistance = settings.EffectiveFocusNearDistance;
        float effectiveFocusFarDistance = settings.EffectiveFocusFarDistance;
        if (!force
            && Mathf.Approximately(settings.Iso, lastIso)
            && Mathf.Approximately(settings.ShutterSpeed, lastShutterSpeed)
            && Mathf.Approximately(settings.Aperture, lastAperture)
            && Mathf.Approximately(settings.FocalLength, lastFocalLength)
            && Mathf.Approximately(settings.FocalLengthMin, lastFocalLengthMin)
            && Mathf.Approximately(settings.FocalLengthMax, lastFocalLengthMax)
            && Mathf.Approximately(settings.FocusDistance, lastFocusDistance)
            && Mathf.Approximately(settings.FocusDistanceMin, lastFocusDistanceMin)
            && Mathf.Approximately(settings.FocusDistanceMax, lastFocusDistanceMax)
            && Mathf.Approximately(settings.FocusClearRange, lastFocusClearRange)
            && Mathf.Approximately(effectiveFocusNearDistance, lastEffectiveFocusNearDistance)
            && Mathf.Approximately(effectiveFocusFarDistance, lastEffectiveFocusFarDistance)
            && Mathf.Approximately(settings.ExposureCompensation, lastExposureCompensation))
        {
            return;
        }

        lastIso = settings.Iso;
        lastShutterSpeed = settings.ShutterSpeed;
        lastAperture = settings.Aperture;
        lastFocalLength = settings.FocalLength;
        lastFocalLengthMin = settings.FocalLengthMin;
        lastFocalLengthMax = settings.FocalLengthMax;
        lastFocusDistance = settings.FocusDistance;
        lastFocusDistanceMin = settings.FocusDistanceMin;
        lastFocusDistanceMax = settings.FocusDistanceMax;
        lastFocusClearRange = settings.FocusClearRange;
        lastEffectiveFocusNearDistance = effectiveFocusNearDistance;
        lastEffectiveFocusFarDistance = effectiveFocusFarDistance;
        lastExposureCompensation = settings.ExposureCompensation;
        float effectiveFocusClearRange = settings.EffectiveFocusClearRange;

        SetText(isoText, FormatIso(lastIso));
        SetText(shutterSpeedText, FormatShutterSpeed(lastShutterSpeed));
        SetText(apertureText, FormatAperture(lastAperture));
        SetText(focalLengthText, FormatFocalLength(lastFocalLength));
        SetText(focusDistanceText, FormatFocusDistance(lastFocusDistance));
        SetText(focusClearRangeText, FormatFocusClearRange(effectiveFocusClearRange));
        SetText(exposureCompensationText, FormatExposureCompensation(lastExposureCompensation));
        UpdateEvScalePointer(lastExposureCompensation);
        UpdateFocalScalePointer(lastFocalLength, lastFocalLengthMin, lastFocalLengthMax);
        UpdateFocusScale(lastFocusDistance, effectiveFocusNearDistance, effectiveFocusFarDistance, lastFocusDistanceMin, lastFocusDistanceMax);
    }

    private void UpdateEvScalePointer(float ev)
    {
        if (!evScalePointer) return;
        float clamped = Mathf.Clamp(ev, -evScaleRangeStops, evScaleRangeStops);
        Vector2 pos = evScalePointer.anchoredPosition;
        pos.x = clamped * evScalePixelsPerStop;
        evScalePointer.anchoredPosition = pos;
    }

    /// <summary>
    /// Maps a value to 0..1 on a logarithmic scale between min and max.
    /// Shared by the runtime pointer placement (below) and the editor tick/label placement
    /// in CameraHudTools.SetupLensScales, so the bar art and the pointer never drift.
    /// </summary>
    public static float NormalizeLog(float value, float min, float max)
    {
        value = Mathf.Max(value, 1e-4f);
        min = Mathf.Max(min, 1e-4f);
        max = Mathf.Max(max, min + 1e-4f);
        float t = (Mathf.Log(value) - Mathf.Log(min)) / (Mathf.Log(max) - Mathf.Log(min));
        return Mathf.Clamp01(t);
    }

    // Vertical zoom bar: focal length (mm) → pointer Y (W bottom, T top). Centered convention
    // (t-0.5)*travel keeps the pointer aligned with a bar centered on the pointer's parent origin.
    private void UpdateFocalScalePointer(float focalLengthMm, float minFocalLengthMm, float maxFocalLengthMm)
    {
        Vector2 range = SortRange(new Vector2(minFocalLengthMm, maxFocalLengthMm), 1f);
        if (focalScalePointer)
        {
            float t = NormalizeLog(focalLengthMm, range.x, range.y);
            Vector2 pos = focalScalePointer.anchoredPosition;
            pos.y = (t - 0.5f) * focalScaleTravel;
            focalScalePointer.anchoredPosition = pos;
        }

        SetText(focalScaleValueText, FormatFocalLength(focalLengthMm));
        UpdateLensTickLabels(focalScalePointer, focalScaleValueText, ref focalTickLabels, range, focalScaleTravel, 24f, FormatFocalTickLabel);
    }

    // Vertical distance scale: focus distance (m) → pointer Y (near bottom → ∞ top), plus an
    // in-focus bracket sized from the clear range (slightly asymmetric: ~1/3 in front, ~2/3
    // behind, like real DoF).
    private void UpdateFocusScale(float focusDistanceM, float nearFocusDistanceM, float farFocusDistanceM, float minFocusDistanceM, float maxFocusDistanceM)
    {
        Vector2 range = SortRange(new Vector2(minFocusDistanceM, maxFocusDistanceM), 0.1f);
        if (focusScalePointer)
        {
            float t = NormalizeLog(focusDistanceM, range.x, range.y);
            Vector2 pos = focusScalePointer.anchoredPosition;
            pos.y = (t - 0.5f) * focusScaleTravel;
            focusScalePointer.anchoredPosition = pos;
        }

        if (focusClearBracket)
        {
            float near = Mathf.Clamp(nearFocusDistanceM, range.x, range.y);
            float far = Mathf.Clamp(farFocusDistanceM, near, range.y);
            float tNear = NormalizeLog(near, range.x, range.y);
            float tFar = NormalizeLog(far, range.x, range.y);
            float centerT = (tNear + tFar) * 0.5f;

            Vector2 pos = focusClearBracket.anchoredPosition;
            pos.y = (centerT - 0.5f) * focusScaleTravel;
            focusClearBracket.anchoredPosition = pos;

            Vector2 size = focusClearBracket.sizeDelta;
            size.y = Mathf.Max(minBracketPixels, (tFar - tNear) * focusScaleTravel);
            focusClearBracket.sizeDelta = size;
        }

        SetText(focusScaleValueText, FormatFocusDistance(focusDistanceM));
        UpdateLensTickLabels(focusScalePointer, focusScaleValueText, ref focusTickLabels, range, focusScaleTravel, -15f, FormatFocusTickLabel);
    }

    private static void UpdateLensTickLabels(RectTransform pointer, TMP_Text template, ref TMP_Text[] labels, Vector2 range, float travel, float xPosition, System.Func<float, string> formatter)
    {
        if (!pointer)
        {
            return;
        }

        EnsureLensTickLabels(pointer.parent, template, ref labels, xPosition);
        if (labels == null || labels.Length == 0)
        {
            return;
        }

        for (int i = 0; i < labels.Length; i++)
        {
            TMP_Text label = labels[i];
            if (!label)
            {
                continue;
            }

            float t = labels.Length <= 1 ? 0f : i / (float)(labels.Length - 1);
            float value = Mathf.Exp(Mathf.Lerp(Mathf.Log(range.x), Mathf.Log(range.y), t));
            if (i == 0)
            {
                value = range.x;
            }
            else if (i == labels.Length - 1)
            {
                value = range.y;
            }

            label.text = formatter(value);

            RectTransform rt = label.rectTransform;
            Vector2 pos = rt.anchoredPosition;
            pos.y = (NormalizeLog(value, range.x, range.y) - 0.5f) * travel;
            rt.anchoredPosition = pos;
        }
    }

    private static void EnsureLensTickLabels(Transform parent, TMP_Text template, ref TMP_Text[] labels, float xPosition)
    {
        if (!parent)
        {
            return;
        }

        if (labels == null || labels.Length != LensTickCount)
        {
            labels = new TMP_Text[LensTickCount];
        }

        for (int i = 0; i < labels.Length; i++)
        {
            if (labels[i])
            {
                continue;
            }

            Transform tick = parent.Find("Tick_" + i);
            labels[i] = tick ? tick.GetComponent<TMP_Text>() : CreateLensTickLabel(parent, template, i, xPosition);
        }
    }

    private static TMP_Text CreateLensTickLabel(Transform parent, TMP_Text template, int index, float xPosition)
    {
        GameObject tick = new GameObject("Tick_" + index, typeof(RectTransform));
        tick.layer = parent.gameObject.layer;
        tick.transform.SetParent(parent, false);

        RectTransform rt = tick.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(56f, 20f);
        rt.anchoredPosition = new Vector2(xPosition, 0f);

        TextMeshProUGUI label = tick.AddComponent<TextMeshProUGUI>();
        label.text = string.Empty;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;

        if (template)
        {
            label.font = template.font;
            label.fontSharedMaterial = template.fontSharedMaterial;
            label.fontSize = Mathf.Max(1f, template.fontSize * 0.75f);
            label.color = template.color;
            label.enableWordWrapping = false;
        }
        else
        {
            label.fontSize = 11f;
            label.color = Color.white;
            label.enableWordWrapping = false;
        }

        return label;
    }

    private void RefreshStatus(bool force)
    {
        string resolvedAspect = ResolveAspectRatio();

        if (!force
            && shotsRemaining == lastShotsRemaining
            && string.Equals(resolvedAspect, lastAspectRatio)
            && string.Equals(fileFormat, lastFileFormat)
            && Mathf.Approximately(batteryPercent, lastBatteryPercent))
        {
            return;
        }

        lastShotsRemaining = shotsRemaining;
        lastAspectRatio = resolvedAspect;
        lastFileFormat = fileFormat;
        lastBatteryPercent = batteryPercent;

        SetText(shotsRemainingText, FormatShotsRemaining(lastShotsRemaining));
        SetText(aspectRatioText, lastAspectRatio);
        SetText(fileFormatText, FormatFileFormat(lastFileFormat));
        SetText(batteryText, FormatBattery(lastBatteryPercent));
    }

    private static readonly (float ratio, string label)[] StandardAspectRatios =
    {
        (1f / 1f, "1:1"),
        (4f / 3f, "4:3"),
        (3f / 2f, "3:2"),
        (16f / 9f, "16:9"),
        (21f / 9f, "21:9"),
    };

    private string ResolveAspectRatio()
    {
        Camera cam = referenceCamera ? referenceCamera : Camera.main;
        int width;
        int height;
        float aspect;

        if (cam && cam.targetTexture)
        {
            width = cam.targetTexture.width;
            height = cam.targetTexture.height;
            aspect = height > 0 ? (float)width / height : 1f;
        }
        else
        {
            width = Mathf.Max(1, Screen.width);
            height = Mathf.Max(1, Screen.height);
            aspect = cam ? cam.aspect : (float)width / height;
        }

        const float tolerance = 0.03f;
        foreach (var (ratio, label) in StandardAspectRatios)
        {
            if (Mathf.Abs(aspect - ratio) < tolerance)
            {
                return label;
            }
        }

        int divisor = Gcd(width, height);
        return $"{width / divisor}:{height / divisor}";
    }

    private static int Gcd(int a, int b)
    {
        a = Mathf.Abs(a);
        b = Mathf.Abs(b);
        while (b != 0)
        {
            int t = b;
            b = a % b;
            a = t;
        }
        return a == 0 ? 1 : a;
    }

    private void EnsureReferences()
    {
        if (!settings)
        {
            settings = GetComponent<CAMCOLCameraSettings>();
        }
    }

    private static void SetText(TMP_Text target, string value)
    {
        if (target)
        {
            target.text = value;
        }
    }

    private static string FormatIso(float value)
    {
        return $"ISO {Mathf.RoundToInt(value)}";
    }

    private static string FormatShutterSpeed(float value)
    {
        if (value <= 0f)
        {
            return "0\"";
        }

        if (value < 1f)
        {
            return $"1/{Mathf.RoundToInt(1f / value)}";
        }

        return $"{value:0.##}\"";
    }

    private static string FormatAperture(float value)
    {
        return $"F{value:0.#}";
    }

    private static string FormatFocalLength(float value)
    {
        return $"{value:0.#}mm";
    }

    private static string FormatFocusDistance(float value)
    {
        return $"{value:0.##}m";
    }

    private static string FormatFocusClearRange(float value)
    {
        return $"{value:0.##}m";
    }

    private static string FormatFocalTickLabel(float value)
    {
        return value < 10f ? $"{value:0.#}" : $"{Mathf.RoundToInt(value)}";
    }

    private static string FormatFocusTickLabel(float value)
    {
        if (value < 1f)
        {
            return $"{value:0.##}";
        }

        if (value < 10f)
        {
            return $"{value:0.#}";
        }

        return $"{Mathf.RoundToInt(value)}";
    }

    private static string FormatExposureCompensation(float value)
    {
        if (Mathf.Approximately(value, 0f))
        {
            return "±0.0";
        }

        return $"{value:+0.0;-0.0}";
    }

    private static string FormatShotsRemaining(int value)
    {
        return value.ToString();
    }

    private static string FormatFileFormat(string value)
    {
        return string.IsNullOrEmpty(value) ? string.Empty : value;
    }

    private static string FormatBattery(float value)
    {
        return $"{Mathf.RoundToInt(value)}%";
    }
}
