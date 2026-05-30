using UnityEngine;

[DisallowMultipleComponent]
public class CAMCOLCameraSettingsKeyboardController : MonoBehaviour
{
    private static readonly int[] IsoSteps =
    {
        50, 64, 80, 100, 125, 160, 200, 250, 320, 400, 500, 640, 800,
        1000, 1250, 1600, 2000, 2500, 3200, 4000, 5000, 6400, 8000,
        10000, 12800, 16000, 20000, 25600, 32000, 40000, 51200, 64000,
        80000, 102400
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

    [Header("References")]
    [SerializeField] private CAMCOLCameraSettings settings;

    [Header("ISO")]
    [SerializeField] private KeyCode decreaseIsoKey = KeyCode.None;
    [SerializeField] private KeyCode increaseIsoKey = KeyCode.None;

    [Header("Shutter Speed")]
    [SerializeField] private KeyCode fasterShutterKey = KeyCode.None;
    [SerializeField] private KeyCode slowerShutterKey = KeyCode.None;

    [Header("Aperture")]
    [SerializeField] private KeyCode closeApertureKey = KeyCode.None;
    [SerializeField] private KeyCode openApertureKey = KeyCode.None;
    [SerializeField] private float apertureStepStops = 0.5f;

    [Header("Focal Length")]
    [SerializeField] private KeyCode decreaseFocalLengthKey = KeyCode.None;
    [SerializeField] private KeyCode increaseFocalLengthKey = KeyCode.None;
    [SerializeField] private float focalLengthUnitsPerSecond = 40f;

    [Header("Focus Distance")]
    [SerializeField] private KeyCode decreaseFocusDistanceKey = KeyCode.None;
    [SerializeField] private KeyCode increaseFocusDistanceKey = KeyCode.None;
    [SerializeField] private float nearFocusUnitsPerSecond = 0.5f;
    [SerializeField] private float focusSpeedRatio = 0.3f;
    [SerializeField] private float maxFocusUnitsPerSecond = 1000f;

    [Header("Focus Clear Range")]
    [SerializeField] private KeyCode decreaseFocusClearRangeKey = KeyCode.None;
    [SerializeField] private KeyCode increaseFocusClearRangeKey = KeyCode.None;
    [SerializeField] private float focusClearRangeUnitsPerSecond = 3f;

    [Header("Exposure Compensation")]
    [SerializeField] private KeyCode decreaseExposureCompensationKey = KeyCode.None;
    [SerializeField] private KeyCode increaseExposureCompensationKey = KeyCode.None;
    [SerializeField] private KeyCode alternateDecreaseExposureCompensationKey = KeyCode.Minus;
    [SerializeField] private KeyCode alternateIncreaseExposureCompensationKey = KeyCode.Equals;
    [SerializeField] private float exposureCompensationStopsPerSecond = 1f;

    private void Reset()
    {
        settings = GetComponent<CAMCOLCameraSettings>();
    }

    private void OnValidate()
    {
        apertureStepStops = Mathf.Max(0f, apertureStepStops);
        focalLengthUnitsPerSecond = Mathf.Max(0f, focalLengthUnitsPerSecond);
        nearFocusUnitsPerSecond = Mathf.Max(0.001f, nearFocusUnitsPerSecond);
        focusSpeedRatio = Mathf.Max(0f, focusSpeedRatio);
        maxFocusUnitsPerSecond = Mathf.Max(nearFocusUnitsPerSecond, maxFocusUnitsPerSecond);
        focusClearRangeUnitsPerSecond = Mathf.Max(0f, focusClearRangeUnitsPerSecond);
        exposureCompensationStopsPerSecond = Mathf.Max(0f, exposureCompensationStopsPerSecond);
        EnsureReferences();
    }

    private void Update()
    {
        EnsureReferences();

        if (!settings)
        {
            return;
        }

        float dt = Time.deltaTime;
        ApplyIsoStepInput(decreaseIsoKey, increaseIsoKey);
        ApplyShutterStepInput(fasterShutterKey, slowerShutterKey);
        ApplyStopStepInput(openApertureKey, closeApertureKey, apertureStepStops * 0.5f, settings.Aperture, settings.SetAperture);
        ApplyLinearInput(decreaseFocalLengthKey, increaseFocalLengthKey, focalLengthUnitsPerSecond, settings.FocalLength, settings.SetFocalLength, dt);
        ApplyFocusDistanceContinuousInput(decreaseFocusDistanceKey, increaseFocusDistanceKey, dt);
        ApplyLinearInput(decreaseFocusClearRangeKey, increaseFocusClearRangeKey, focusClearRangeUnitsPerSecond, settings.FocusClearRange, settings.SetFocusClearRange, dt);
        ApplyLinearInput(
            GetInputDirection(decreaseExposureCompensationKey, increaseExposureCompensationKey)
                + GetInputDirection(alternateDecreaseExposureCompensationKey, alternateIncreaseExposureCompensationKey),
            exposureCompensationStopsPerSecond,
            settings.ExposureCompensation,
            settings.SetExposureCompensation,
            dt);
    }

    private void EnsureReferences()
    {
        if (!settings)
        {
            settings = GetComponent<CAMCOLCameraSettings>();
        }
    }

    private static void ApplyStopStepInput(KeyCode decreaseKey, KeyCode increaseKey, float stepStops, float currentValue, System.Action<float> setter)
    {
        int direction = GetKeyDownDirection(decreaseKey, increaseKey);
        if (direction == 0)
        {
            return;
        }

        float multiplier = Mathf.Pow(2f, direction * stepStops);
        setter(Mathf.Max(0.0001f, currentValue * multiplier));
    }

    private void ApplyIsoStepInput(KeyCode decreaseKey, KeyCode increaseKey)
    {
        int direction = GetKeyDownDirection(decreaseKey, increaseKey);
        if (direction == 0)
        {
            return;
        }

        int currentIndex = FindClosestIsoStepIndex(settings.Iso);
        int nextIndex = Mathf.Clamp(currentIndex + direction, 0, IsoSteps.Length - 1);
        settings.SetIso(IsoSteps[nextIndex]);
    }

    private void ApplyShutterStepInput(KeyCode fasterKey, KeyCode slowerKey)
    {
        int direction = GetKeyDownDirection(fasterKey, slowerKey);
        if (direction == 0)
        {
            return;
        }

        int currentIndex = FindClosestShutterStepIndex(settings.ShutterSpeed);
        int nextIndex = Mathf.Clamp(currentIndex + direction, 0, ShutterSpeedSteps.Length - 1);
        settings.SetShutterSpeed(ShutterSpeedSteps[nextIndex]);
    }

    private static int FindClosestIsoStepIndex(float iso)
    {
        int closestIndex = 0;
        float closestDelta = Mathf.Abs(IsoSteps[0] - iso);

        for (int i = 1; i < IsoSteps.Length; i++)
        {
            float delta = Mathf.Abs(IsoSteps[i] - iso);
            if (delta < closestDelta)
            {
                closestDelta = delta;
                closestIndex = i;
            }
        }

        return closestIndex;
    }

    private static int FindClosestShutterStepIndex(float shutterSpeed)
    {
        int closestIndex = 0;
        float closestDelta = Mathf.Abs(ShutterSpeedSteps[0] - shutterSpeed);

        for (int i = 1; i < ShutterSpeedSteps.Length; i++)
        {
            float delta = Mathf.Abs(ShutterSpeedSteps[i] - shutterSpeed);
            if (delta < closestDelta)
            {
                closestDelta = delta;
                closestIndex = i;
            }
        }

        return closestIndex;
    }

    private void ApplyFocusDistanceContinuousInput(KeyCode decreaseKey, KeyCode increaseKey, float deltaTime)
    {
        float direction = GetInputDirection(decreaseKey, increaseKey);
        if (Mathf.Approximately(direction, 0f))
        {
            return;
        }

        float unitsPerSecond = Mathf.Clamp(settings.FocusDistance * focusSpeedRatio, nearFocusUnitsPerSecond, maxFocusUnitsPerSecond);
        settings.SetFocusDistance(settings.FocusDistance + direction * unitsPerSecond * deltaTime);
    }

    private static void ApplyLinearInput(KeyCode decreaseKey, KeyCode increaseKey, float unitsPerSecond, float currentValue, System.Action<float> setter, float deltaTime)
    {
        ApplyLinearInput(GetInputDirection(decreaseKey, increaseKey), unitsPerSecond, currentValue, setter, deltaTime);
    }

    private static void ApplyLinearInput(float direction, float unitsPerSecond, float currentValue, System.Action<float> setter, float deltaTime)
    {
        direction = Mathf.Clamp(direction, -1f, 1f);
        if (Mathf.Approximately(direction, 0f))
        {
            return;
        }

        setter(currentValue + direction * unitsPerSecond * deltaTime);
    }

    private static float GetInputDirection(KeyCode decreaseKey, KeyCode increaseKey)
    {
        float direction = 0f;

        if (decreaseKey != KeyCode.None && Input.GetKey(decreaseKey))
        {
            direction -= 1f;
        }

        if (increaseKey != KeyCode.None && Input.GetKey(increaseKey))
        {
            direction += 1f;
        }

        return direction;
    }

    private static int GetKeyDownDirection(KeyCode decreaseKey, KeyCode increaseKey)
    {
        int direction = 0;

        if (decreaseKey != KeyCode.None && Input.GetKeyDown(decreaseKey))
        {
            direction -= 1;
        }

        if (increaseKey != KeyCode.None && Input.GetKeyDown(increaseKey))
        {
            direction += 1;
        }

        return direction;
    }
}
