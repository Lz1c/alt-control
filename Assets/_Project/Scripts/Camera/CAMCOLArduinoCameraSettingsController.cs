using System;
using UnityEngine;

[DisallowMultipleComponent]
public class CAMCOLArduinoCameraSettingsController : MonoBehaviour
{
    private static readonly float[] IsoSteps =
    {
        50f, 64f, 80f, 100f, 125f, 160f, 200f, 250f, 320f, 400f, 500f, 640f,
        800f, 1000f, 1250f, 1600f, 2000f, 2500f, 3200f, 4000f, 5000f, 6400f,
        8000f, 10000f, 12800f, 16000f, 20000f, 25600f, 32000f, 40000f,
        51200f, 64000f, 80000f, 102400f
    };

    private static readonly float[] ShutterSpeedSteps =
    {
        1f / 1000f, 1f / 800f, 1f / 640f, 1f / 500f, 1f / 400f,
        1f / 320f, 1f / 250f, 1f / 200f, 1f / 160f, 1f / 125f,
        1f / 100f, 1f / 80f, 1f / 60f, 1f / 50f, 1f / 40f,
        1f / 30f, 1f / 25f, 1f / 20f, 1f / 15f, 1f / 13f,
        1f / 10f, 1f / 8f, 1f / 6f, 1f / 5f, 1f / 4f,
        0.3f, 0.4f, 0.5f, 0.6f, 0.8f,
        1f, 2f, 3f, 4f, 5f, 6f
    };

    private static readonly float[] ApertureSteps =
    {
        1f, 1.2f, 1.4f, 1.8f, 2f, 2.2f, 2.8f, 3.5f, 4f, 4.5f,
        5.6f, 6.3f, 7.1f, 8f, 9f, 10f, 11f, 13f, 16f, 18f, 20f,
        22f, 25f, 29f, 32f
    };

    [Header("References")]
    [SerializeField] private CAMCOLArduinoSerialReader serialReader;
    [SerializeField] private CAMCOLCameraSettings settings;

    [Header("Input")]
    [SerializeField] private string prefix = "KNOBS:";
    [SerializeField] private int analogMin = 0;
    [SerializeField] private int analogMax = 1023;

    [Header("A0 ISO")]
    [SerializeField] private float isoMin = 100f;
    [SerializeField] private float isoMax = 6400f;
    [SerializeField] private bool invertIso;

    [Header("A1 Shutter Speed")]
    [SerializeField] private float shutterSpeedMin = 1f / 1000f;
    [SerializeField] private float shutterSpeedMax = 6f;
    [SerializeField] private bool invertShutterSpeed;

    [Header("A2 Aperture")]
    [SerializeField] private float apertureMin = 1f;
    [SerializeField] private float apertureMax = 32f;
    [SerializeField] private bool invertAperture;

    [Header("A3 Exposure Compensation")]
    [SerializeField] private float exposureCompensationMin = -3f;
    [SerializeField] private float exposureCompensationMax = 3f;
    [SerializeField] private bool invertExposureCompensation;

    [Header("A4 Focal Length")]
    [SerializeField] private float focalLengthMin = 24f;
    [SerializeField] private float focalLengthMax = 70f;
    [SerializeField] private bool invertFocalLength;
    [SerializeField] private bool focalLengthLogScale = true;

    [Header("A5 Focus Distance")]
    [SerializeField] private float focusDistanceMin = 0.2f;
    [SerializeField] private float focusDistanceMax = 500f;
    [SerializeField] private bool invertFocusDistance;
    [SerializeField] private bool focusDistanceLogScale = true;

    [Header("Debug")]
    [SerializeField] private bool logAppliedValues = true;
    [SerializeField] private string lastLine;
    [SerializeField] private int lastA0 = -1;
    [SerializeField] private int lastA1 = -1;
    [SerializeField] private int lastA2 = -1;
    [SerializeField] private int lastA3 = -1;
    [SerializeField] private int lastA4 = -1;
    [SerializeField] private int lastA5 = -1;

    private string lastAppliedLine;

    private void Reset()
    {
        serialReader = GetComponent<CAMCOLArduinoSerialReader>();
        settings = GetComponent<CAMCOLCameraSettings>();
    }

    private void OnEnable()
    {
        EnsureReferences();
        if (serialReader)
        {
            serialReader.OnLineReceived.AddListener(ApplySerialLine);
        }
    }

    private void OnDisable()
    {
        if (serialReader)
        {
            serialReader.OnLineReceived.RemoveListener(ApplySerialLine);
        }
    }

    private void Update()
    {
        EnsureReferences();
        if (!serialReader || string.IsNullOrEmpty(serialReader.LastReceivedLine) || string.Equals(serialReader.LastReceivedLine, lastAppliedLine, StringComparison.Ordinal))
        {
            return;
        }

        ApplySerialLine(serialReader.LastReceivedLine);
    }

    private void OnValidate()
    {
        analogMax = Mathf.Max(analogMin + 1, analogMax);
        isoMin = Mathf.Max(1f, isoMin);
        isoMax = Mathf.Max(isoMin, isoMax);
        shutterSpeedMin = Mathf.Max(0.0001f, shutterSpeedMin);
        shutterSpeedMax = Mathf.Max(shutterSpeedMin, shutterSpeedMax);
        apertureMin = Mathf.Max(0.1f, apertureMin);
        apertureMax = Mathf.Max(apertureMin, apertureMax);
        exposureCompensationMax = Mathf.Max(exposureCompensationMin, exposureCompensationMax);
        focalLengthMin = Mathf.Max(1f, focalLengthMin);
        focalLengthMax = Mathf.Max(focalLengthMin, focalLengthMax);
        focusDistanceMin = Mathf.Max(0.1f, focusDistanceMin);
        focusDistanceMax = Mathf.Max(focusDistanceMin, focusDistanceMax);
        EnsureReferences();
    }

    private void EnsureReferences()
    {
        if (!serialReader)
        {
            serialReader = GetComponent<CAMCOLArduinoSerialReader>();
        }

        if (!settings)
        {
            settings = GetComponent<CAMCOLCameraSettings>();
        }
    }

    private void ApplySerialLine(string line)
    {
        lastLine = line;
        lastAppliedLine = line;

        if (!settings || string.IsNullOrWhiteSpace(line) || !line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        string[] parts = line.Substring(prefix.Length).Split(',');
        if (parts.Length < 6)
        {
            Debug.LogWarning($"Arduino camera controller expected 6 knob values, got {parts.Length}: {line}", this);
            return;
        }

        if (!ReadAnalog(parts[0], out lastA0)
            || !ReadAnalog(parts[1], out lastA1)
            || !ReadAnalog(parts[2], out lastA2)
            || !ReadAnalog(parts[3], out lastA3)
            || !ReadAnalog(parts[4], out lastA4)
            || !ReadAnalog(parts[5], out lastA5))
        {
            Debug.LogWarning($"Arduino camera controller could not parse knob values: {line}", this);
            return;
        }

        ApplyValues();
    }

    private bool ReadAnalog(string text, out int value)
    {
        value = 0;
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        string trimmed = text.Trim();
        if (!int.TryParse(trimmed, out value))
        {
            char[] digits = new char[trimmed.Length];
            int digitCount = 0;
            for (int i = 0; i < trimmed.Length; i++)
            {
                if (char.IsDigit(trimmed[i]))
                {
                    digits[digitCount] = trimmed[i];
                    digitCount++;
                }
            }

            if (digitCount == 0 || !int.TryParse(new string(digits, 0, digitCount), out value))
            {
                return false;
            }
        }

        value = Mathf.Clamp(value, analogMin, analogMax);
        return true;
    }

    private void ApplyValues()
    {
        float isoT = Normalize(lastA0, invertIso);
        float shutterT = Normalize(lastA1, invertShutterSpeed);
        float apertureT = Normalize(lastA2, invertAperture);
        float exposureT = Normalize(lastA3, invertExposureCompensation);
        float focalT = Normalize(lastA4, invertFocalLength);
        float focusT = Normalize(lastA5, invertFocusDistance);

        settings.SetIso(PickStep(IsoSteps, isoMin, isoMax, isoT));
        settings.SetShutterSpeed(PickStep(ShutterSpeedSteps, shutterSpeedMin, shutterSpeedMax, shutterT));
        settings.SetAperture(PickStep(ApertureSteps, apertureMin, apertureMax, apertureT));
        settings.SetExposureCompensation(Mathf.Lerp(exposureCompensationMin, exposureCompensationMax, exposureT));
        settings.SetFocalLength(LerpRange(focalLengthMin, focalLengthMax, focalT, focalLengthLogScale));
        settings.SetFocusDistance(LerpRange(focusDistanceMin, focusDistanceMax, focusT, focusDistanceLogScale));

        if (logAppliedValues)
        {
            Debug.Log($"Applied Arduino knobs -> ISO {settings.Iso:0}, Shutter {settings.ShutterSpeed:0.####}, F{settings.Aperture:0.#}, EV {settings.ExposureCompensation:0.#}, {settings.FocalLength:0.#}mm, Focus {settings.FocusDistance:0.##}m", this);
        }
    }

    private float Normalize(int value, bool invert)
    {
        float t = Mathf.InverseLerp(analogMin, analogMax, value);
        return invert ? 1f - t : t;
    }

    private static float LerpRange(float min, float max, float t, bool logScale)
    {
        return logScale
            ? Mathf.Exp(Mathf.Lerp(Mathf.Log(min), Mathf.Log(max), t))
            : Mathf.Lerp(min, max, t);
    }

    private static float PickStep(float[] steps, float minValue, float maxValue, float t)
    {
        int first = 0;
        int last = steps.Length - 1;

        for (int i = 0; i < steps.Length; i++)
        {
            if (steps[i] >= minValue)
            {
                first = i;
                break;
            }
        }

        for (int i = steps.Length - 1; i >= 0; i--)
        {
            if (steps[i] <= maxValue)
            {
                last = i;
                break;
            }
        }

        if (last < first)
        {
            return Mathf.Lerp(minValue, maxValue, t);
        }

        int index = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(first, last, t)), first, last);
        return steps[index];
    }
}
