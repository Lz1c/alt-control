using UnityEngine;

[DisallowMultipleComponent]
public class CAMCOLCameraSettings : MonoBehaviour
{
    private const float ReferenceFocusClearRange = 2f;
    private const float CircleOfConfusionMm = 0.03f;

    [SerializeField] private float iso = 100f;
    [SerializeField] private Vector2 isoLimits = new Vector2(50f, 6400f);

    [SerializeField] private float shutterSpeed = 1f / 125f;
    [SerializeField] private Vector2 shutterSpeedLimits = new Vector2(1f / 1000f, 6f);

    [SerializeField] private float aperture = 5.6f;
    [SerializeField] private Vector2 apertureLimits = new Vector2(1f, 32f);

    [SerializeField] private float focalLength = 50f;
    [SerializeField] private Vector2 focalLengthLimits = new Vector2(1f, 300f);

    [SerializeField] private float focusDistance = 10f;
    [SerializeField] private Vector2 focusDistanceLimits = new Vector2(0.1f, 10000f);

    [SerializeField] private float focusClearRange = 2f;
    [SerializeField] private Vector2 focusClearRangeLimits = new Vector2(0.1f, 100f);

    [SerializeField] private float exposureCompensation;
    [SerializeField] private Vector2 exposureCompensationLimits = new Vector2(-3f, 3f);

    public float Iso => iso;
    public float ShutterSpeed => shutterSpeed;
    public float Aperture => aperture;
    public float FocalLength => focalLength;
    public float FocalLengthMin => Mathf.Max(1f, focalLengthLimits.x);
    public float FocalLengthMax => Mathf.Max(FocalLengthMin, focalLengthLimits.y);
    public float FocusDistance => focusDistance;
    public float FocusDistanceMin => Mathf.Max(0.1f, focusDistanceLimits.x);
    public float FocusDistanceMax => Mathf.Max(FocusDistanceMin, focusDistanceLimits.y);
    public float FocusClearRange => focusClearRange;
    public float EffectiveFocusNearDistance
    {
        get
        {
            CalculateEffectiveFocusRange(out float near, out _, out _);
            return near;
        }
    }
    public float EffectiveFocusFarDistance
    {
        get
        {
            CalculateEffectiveFocusRange(out _, out float far, out _);
            return far;
        }
    }
    public float EffectiveFocusClearRange
    {
        get
        {
            CalculateEffectiveFocusRange(out _, out _, out float clearRange);
            return clearRange;
        }
    }
    public float ExposureCompensation => exposureCompensation;

    public void SetIso(float value)
    {
        iso = Mathf.Clamp(value, IsoMin, IsoMax);
    }

    public void SetShutterSpeed(float value)
    {
        shutterSpeed = Mathf.Clamp(value, ShutterSpeedMin, ShutterSpeedMax);
    }

    public void SetAperture(float value)
    {
        aperture = Mathf.Clamp(value, ApertureMin, ApertureMax);
    }

    public void SetFocalLength(float value)
    {
        focalLength = Mathf.Clamp(value, FocalLengthMin, FocalLengthMax);
    }

    public void SetFocusDistance(float value)
    {
        focusDistance = Mathf.Clamp(value, FocusDistanceMin, FocusDistanceMax);
    }

    public void SetFocusClearRange(float value)
    {
        focusClearRange = Mathf.Clamp(value, FocusClearRangeMin, FocusClearRangeMax);
    }

    public void SetExposureCompensation(float value)
    {
        exposureCompensation = Mathf.Clamp(value, ExposureCompensationMin, ExposureCompensationMax);
    }

    private void OnValidate()
    {
        SortLimits(ref isoLimits);
        SortLimits(ref shutterSpeedLimits);
        SortLimits(ref apertureLimits);
        SortLimits(ref focalLengthLimits);
        SortLimits(ref focusDistanceLimits);
        SortLimits(ref focusClearRangeLimits);
        SortLimits(ref exposureCompensationLimits);

        SetIso(iso);
        SetShutterSpeed(shutterSpeed);
        SetAperture(aperture);
        SetFocalLength(focalLength);
        SetFocusDistance(focusDistance);
        SetFocusClearRange(focusClearRange);
        SetExposureCompensation(exposureCompensation);
    }

    private float IsoMin => Mathf.Max(50f, isoLimits.x);
    private float IsoMax => Mathf.Max(IsoMin, isoLimits.y);
    private float ShutterSpeedMin => Mathf.Max(0.0001f, shutterSpeedLimits.x);
    private float ShutterSpeedMax => Mathf.Max(ShutterSpeedMin, shutterSpeedLimits.y);
    private float ApertureMin => Mathf.Max(0.1f, apertureLimits.x);
    private float ApertureMax => Mathf.Max(ApertureMin, apertureLimits.y);
    private float FocusClearRangeMin => Mathf.Max(0.1f, focusClearRangeLimits.x);
    private float FocusClearRangeMax => Mathf.Max(FocusClearRangeMin, focusClearRangeLimits.y);
    private float ExposureCompensationMin => exposureCompensationLimits.x;
    private float ExposureCompensationMax => Mathf.Max(ExposureCompensationMin, exposureCompensationLimits.y);

    private void CalculateEffectiveFocusRange(out float nearDistance, out float farDistance, out float clearRange)
    {
        float physicalNear = FocusDistance;
        float physicalFar = FocusDistance;
        CalculatePhysicalDepthOfField(out physicalNear, out physicalFar);

        float frontRange = Mathf.Max(0f, FocusDistance - physicalNear);
        float backRange = Mathf.Max(0f, physicalFar - FocusDistance);
        float physicalRange = Mathf.Max(0.001f, frontRange + backRange);
        float tuningMultiplier = Mathf.Max(0.01f, focusClearRange / ReferenceFocusClearRange);
        float tunedRange = Mathf.Clamp(physicalRange * tuningMultiplier, FocusClearRangeMin, FocusClearRangeMax);
        float frontRatio = Mathf.Clamp01(frontRange / physicalRange);
        float backRatio = 1f - frontRatio;

        nearDistance = Mathf.Max(FocusDistanceMin, FocusDistance - tunedRange * frontRatio);
        farDistance = Mathf.Min(FocusDistanceMax, FocusDistance + tunedRange * backRatio);
        clearRange = Mathf.Max(0.001f, farDistance - nearDistance);
    }

    private void CalculatePhysicalDepthOfField(out float nearDistance, out float farDistance)
    {
        float focalLengthMm = Mathf.Max(1f, FocalLength);
        float apertureValue = Mathf.Max(0.1f, Aperture);
        float focusDistanceMm = Mathf.Max(focalLengthMm + 1f, FocusDistance * 1000f);
        float hyperfocalMm = focalLengthMm * focalLengthMm / (apertureValue * CircleOfConfusionMm) + focalLengthMm;

        float nearMm = hyperfocalMm * focusDistanceMm / (hyperfocalMm + focusDistanceMm - focalLengthMm);
        float farDenominator = hyperfocalMm - focusDistanceMm + focalLengthMm;
        float farMm = farDenominator <= 0.001f
            ? FocusDistanceMax * 1000f
            : hyperfocalMm * focusDistanceMm / farDenominator;

        nearDistance = Mathf.Clamp(nearMm * 0.001f, FocusDistanceMin, FocusDistanceMax);
        farDistance = Mathf.Clamp(farMm * 0.001f, nearDistance, FocusDistanceMax);
    }

    private static void SortLimits(ref Vector2 limits)
    {
        if (limits.x <= limits.y)
        {
            return;
        }

        limits = new Vector2(limits.y, limits.x);
    }
}
