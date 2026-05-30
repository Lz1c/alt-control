using TMPro;
using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public class CAMCOLCameraSettingsTMPDisplay : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CAMCOLCameraSettings settings;

    [Header("Text Outputs")]
    [SerializeField] private TMP_Text isoText;
    [SerializeField] private TMP_Text shutterSpeedText;
    [SerializeField] private TMP_Text apertureText;
    [SerializeField] private TMP_Text focalLengthText;
    [SerializeField] private TMP_Text focusDistanceText;
    [SerializeField] private TMP_Text focusClearRangeText;
    [SerializeField] private TMP_Text exposureCompensationText;

    private float lastIso = float.NaN;
    private float lastShutterSpeed = float.NaN;
    private float lastAperture = float.NaN;
    private float lastFocalLength = float.NaN;
    private float lastFocusDistance = float.NaN;
    private float lastFocusClearRange = float.NaN;
    private float lastExposureCompensation = float.NaN;

    private void Reset()
    {
        settings = GetComponent<CAMCOLCameraSettings>();
    }

    private void OnEnable()
    {
        EnsureReferences();
        Refresh(true);
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
        if (!settings)
        {
            ClearText();
            return;
        }

        if (!force
            && Mathf.Approximately(settings.Iso, lastIso)
            && Mathf.Approximately(settings.ShutterSpeed, lastShutterSpeed)
            && Mathf.Approximately(settings.Aperture, lastAperture)
            && Mathf.Approximately(settings.FocalLength, lastFocalLength)
            && Mathf.Approximately(settings.FocusDistance, lastFocusDistance)
            && Mathf.Approximately(settings.FocusClearRange, lastFocusClearRange)
            && Mathf.Approximately(settings.ExposureCompensation, lastExposureCompensation))
        {
            return;
        }

        lastIso = settings.Iso;
        lastShutterSpeed = settings.ShutterSpeed;
        lastAperture = settings.Aperture;
        lastFocalLength = settings.FocalLength;
        lastFocusDistance = settings.FocusDistance;
        lastFocusClearRange = settings.FocusClearRange;
        lastExposureCompensation = settings.ExposureCompensation;

        SetText(isoText, FormatIso(lastIso));
        SetText(shutterSpeedText, FormatShutterSpeed(lastShutterSpeed));
        SetText(apertureText, FormatAperture(lastAperture));
        SetText(focalLengthText, FormatFocalLength(lastFocalLength));
        SetText(focusDistanceText, FormatFocusDistance(lastFocusDistance));
        SetText(focusClearRangeText, FormatFocusClearRange(lastFocusClearRange));
        SetText(exposureCompensationText, FormatExposureCompensation(lastExposureCompensation));
    }

    private void EnsureReferences()
    {
        if (!settings)
        {
            settings = GetComponent<CAMCOLCameraSettings>();
        }
    }

    private void ClearText()
    {
        SetText(isoText, string.Empty);
        SetText(shutterSpeedText, string.Empty);
        SetText(apertureText, string.Empty);
        SetText(focalLengthText, string.Empty);
        SetText(focusDistanceText, string.Empty);
        SetText(focusClearRangeText, string.Empty);
        SetText(exposureCompensationText, string.Empty);

        lastIso = float.NaN;
        lastShutterSpeed = float.NaN;
        lastAperture = float.NaN;
        lastFocalLength = float.NaN;
        lastFocusDistance = float.NaN;
        lastFocusClearRange = float.NaN;
        lastExposureCompensation = float.NaN;
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
        return Mathf.RoundToInt(value).ToString();
    }

    private static string FormatShutterSpeed(float value)
    {
        if (value <= 0f)
        {
            return "0s";
        }

        if (value < 1f)
        {
            return $"1/{Mathf.RoundToInt(1f / value)}s";
        }

        return $"{value:0.##}s";
    }

    private static string FormatAperture(float value)
    {
        return $"f/{value:0.#}";
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

    private static string FormatExposureCompensation(float value)
    {
        return $"{value:+0.#;-0.#;0} EV";
    }
}
