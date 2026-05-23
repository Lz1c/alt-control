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
    [Header("References")]
    [SerializeField] private CAMCOLCameraSettings settings;
    [Tooltip("Camera used to resolve the live aspect ratio. Falls back to Camera.main if unset.")]
    [SerializeField] private Camera referenceCamera;

    [Header("Exposure Text Outputs (bottom HUD)")]
    [SerializeField] private TMP_Text isoText;
    [SerializeField] private TMP_Text shutterSpeedText;
    [SerializeField] private TMP_Text apertureText;
    [SerializeField] private TMP_Text exposureCompensationText;

    [Header("EV Scale Pointer")]
    [Tooltip("Triangle child of EVScale; X is driven by current EV. Scale spans 120 px → 20 px per EV step.")]
    [SerializeField] private RectTransform evScalePointer;
    [SerializeField] private float evScalePixelsPerStop = 20f;
    [SerializeField] private float evScaleRangeStops = 3f;

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
    private float lastExposureCompensation = float.NaN;

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
        EnsureReferences();
        Refresh(true);
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
            SetText(exposureCompensationText, string.Empty);

            lastIso = float.NaN;
            lastShutterSpeed = float.NaN;
            lastAperture = float.NaN;
            lastExposureCompensation = float.NaN;
            return;
        }

        if (!force
            && Mathf.Approximately(settings.Iso, lastIso)
            && Mathf.Approximately(settings.ShutterSpeed, lastShutterSpeed)
            && Mathf.Approximately(settings.Aperture, lastAperture)
            && Mathf.Approximately(settings.ExposureCompensation, lastExposureCompensation))
        {
            return;
        }

        lastIso = settings.Iso;
        lastShutterSpeed = settings.ShutterSpeed;
        lastAperture = settings.Aperture;
        lastExposureCompensation = settings.ExposureCompensation;

        SetText(isoText, FormatIso(lastIso));
        SetText(shutterSpeedText, FormatShutterSpeed(lastShutterSpeed));
        SetText(apertureText, FormatAperture(lastAperture));
        SetText(exposureCompensationText, FormatExposureCompensation(lastExposureCompensation));
        UpdateEvScalePointer(lastExposureCompensation);
    }

    private void UpdateEvScalePointer(float ev)
    {
        if (!evScalePointer) return;
        float clamped = Mathf.Clamp(ev, -evScaleRangeStops, evScaleRangeStops);
        Vector2 pos = evScalePointer.anchoredPosition;
        pos.x = clamped * evScalePixelsPerStop;
        evScalePointer.anchoredPosition = pos;
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
