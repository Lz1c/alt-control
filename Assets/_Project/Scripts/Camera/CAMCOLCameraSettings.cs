using UnityEngine;

[DisallowMultipleComponent]
public class CAMCOLCameraSettings : MonoBehaviour
{
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
    public float FocusDistance => focusDistance;
    public float FocusClearRange => focusClearRange;
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
    private float FocalLengthMin => Mathf.Max(1f, focalLengthLimits.x);
    private float FocalLengthMax => Mathf.Max(FocalLengthMin, focalLengthLimits.y);
    private float FocusDistanceMin => Mathf.Max(0.1f, focusDistanceLimits.x);
    private float FocusDistanceMax => Mathf.Max(FocusDistanceMin, focusDistanceLimits.y);
    private float FocusClearRangeMin => Mathf.Max(0.1f, focusClearRangeLimits.x);
    private float FocusClearRangeMax => Mathf.Max(FocusClearRangeMin, focusClearRangeLimits.y);
    private float ExposureCompensationMin => exposureCompensationLimits.x;
    private float ExposureCompensationMax => Mathf.Max(ExposureCompensationMin, exposureCompensationLimits.y);

    private static void SortLimits(ref Vector2 limits)
    {
        if (limits.x <= limits.y)
        {
            return;
        }

        limits = new Vector2(limits.y, limits.x);
    }
}
