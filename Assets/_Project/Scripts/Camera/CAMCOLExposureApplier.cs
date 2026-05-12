using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[DisallowMultipleComponent]
public class CAMCOLExposureApplier : MonoBehaviour
{
    private const float BaseIso = 100f;
    private const float BaseShutterSpeed = 1f / 125f;
    private const float BaseAperture = 5.6f;
    private const float DefaultFocalLength = 50f;
    private const float PostExposureFloor = -6f;
    private const float PostExposureCeiling = 6f;
    private const float AutoMeteringStrength = 0.4f;
    private const float AutoMeteringDeadZone = 0.2f;
    private const float AutoMeteringFloor = -1.5f;
    private const float AutoMeteringCeiling = 1.5f;

    [Header("References")]
    [Tooltip("Scene-level simulated camera data source. This can live on a separate controller object.")]
    [SerializeField] private CAMCOLCameraSettings settings;
    [SerializeField] private Volume targetVolume;
    [Tooltip("Optional scene metering source. When assigned, actual rendered brightness also affects exposure.")]
    [SerializeField] private CAMMeteringBase metering;
    [Tooltip("Optional focus source used to drive depth of field focus distance.")]
    [SerializeField] private CAMFocusController focusController;
    [Tooltip("Optional camera used to mirror focal length into the depth of field volume override.")]
    [SerializeField] private Camera targetCamera;

    private ColorAdjustments colorAdjustments;
    private DepthOfField depthOfField;
    private float lastIso = -1f;
    private float lastShutterSpeed = -1f;
    private float lastAperture = -1f;
    private float lastExposureCompensation = float.NaN;
    private float lastFocusDistance = -1f;
    private float lastFocalLength = -1f;
    private float autoMeteringOffset;
    private int lastAppliedMeteringVersion = -1;
    private bool warnedMissingSettings;
    private bool warnedMissingVolume;
    private bool warnedMissingProfile;

    private void Reset()
    {
        settings = GetComponent<CAMCOLCameraSettings>();
        metering = GetComponent<CAMMeteringBase>();
        focusController = GetComponent<CAMFocusController>();
        targetCamera = GetComponent<Camera>();
    }

    private void OnEnable()
    {
        EnsureReferences();
        ApplyExposure(true);
    }

    private void Update()
    {
        if (!settings)
        {
            WarnMissingSettings();
            return;
        }

        if (!Mathf.Approximately(settings.Iso, lastIso)
            || !Mathf.Approximately(settings.ShutterSpeed, lastShutterSpeed)
            || !Mathf.Approximately(settings.Aperture, lastAperture)
            || !Mathf.Approximately(settings.ExposureCompensation, lastExposureCompensation)
            || !Mathf.Approximately(GetCurrentFocusDistance(), lastFocusDistance)
            || !Mathf.Approximately(GetCurrentFocalLength(), lastFocalLength)
            || HasMeteringChanged())
        {
            ApplyExposure(true);
        }
    }

    private void OnValidate()
    {
        EnsureReferences();
        ApplyExposure(false);
    }

    public void ApplyExposure()
    {
        ApplyExposure(true);
    }

    private void ApplyExposure(bool logWarnings)
    {
        EnsureReferences();

        if (!settings || !EnsureVolumeOverrides(logWarnings))
        {
            return;
        }

        float baseEv = ComputeEv100(BaseAperture, BaseShutterSpeed, BaseIso);
        float currentEv = ComputeEv100(settings.Aperture, settings.ShutterSpeed, settings.Iso);
        float manualExposure = baseEv - currentEv + settings.ExposureCompensation;

        if (metering && metering.HasValidReading)
        {
            if (metering.ReadingVersion != lastAppliedMeteringVersion)
            {
                autoMeteringOffset = ComputeAutoMeteringOffset(metering.MeteredExposureOffset);
                lastAppliedMeteringVersion = metering.ReadingVersion;
            }
        }
        else
        {
            autoMeteringOffset = 0f;
            lastAppliedMeteringVersion = -1;
        }

        float postExposure = Mathf.Clamp(manualExposure + autoMeteringOffset, PostExposureFloor, PostExposureCeiling);

        colorAdjustments.active = true;
        colorAdjustments.postExposure.overrideState = true;
        colorAdjustments.postExposure.value = postExposure;

        ApplyDepthOfField();

        lastIso = settings.Iso;
        lastShutterSpeed = settings.ShutterSpeed;
        lastAperture = settings.Aperture;
        lastExposureCompensation = settings.ExposureCompensation;
        lastFocusDistance = GetCurrentFocusDistance();
        lastFocalLength = GetCurrentFocalLength();
    }

    private void EnsureReferences()
    {
        if (!settings)
        {
            settings = GetComponent<CAMCOLCameraSettings>();
        }

        if (!metering)
        {
            metering = GetComponent<CAMMeteringBase>();
        }

        if (!focusController)
        {
            focusController = GetComponent<CAMFocusController>();
        }

        if (!targetCamera)
        {
            targetCamera = GetComponent<Camera>();
        }

        if (!targetCamera && focusController)
        {
            targetCamera = focusController.GetComponent<Camera>();
        }

        if (!targetCamera)
        {
            targetCamera = Camera.main;
        }

        if (!focusController && targetCamera)
        {
            focusController = targetCamera.GetComponent<CAMFocusController>();
        }

        if (settings)
        {
            warnedMissingSettings = false;
        }
    }

    private bool HasMeteringChanged()
    {
        if (!metering || !metering.HasValidReading)
        {
            return !Mathf.Approximately(autoMeteringOffset, 0f);
        }

        return metering.ReadingVersion != lastAppliedMeteringVersion;
    }

    private bool EnsureVolumeOverrides(bool logWarnings)
    {
        if (colorAdjustments && depthOfField)
        {
            return true;
        }

        if (!targetVolume)
        {
            if (logWarnings && !warnedMissingVolume)
            {
                Debug.LogWarning($"{nameof(CAMCOLExposureApplier)} on {name} needs a target Volume to apply exposure.", this);
                warnedMissingVolume = true;
            }

            return false;
        }

        warnedMissingVolume = false;

        VolumeProfile profile = targetVolume.profile;
        if (!profile)
        {
            if (logWarnings && !warnedMissingProfile)
            {
                Debug.LogWarning($"{nameof(CAMCOLExposureApplier)} on {name} could not find a Volume Profile on {targetVolume.name}.", this);
                warnedMissingProfile = true;
            }

            return false;
        }

        warnedMissingProfile = false;

        if (!profile.TryGet(out colorAdjustments))
        {
            colorAdjustments = profile.Add<ColorAdjustments>(true);
        }

        if (!profile.TryGet(out depthOfField))
        {
            depthOfField = profile.Add<DepthOfField>(true);
        }

        return colorAdjustments && depthOfField;
    }

    private void WarnMissingSettings()
    {
        if (warnedMissingSettings)
        {
            return;
        }

        Debug.LogWarning($"{nameof(CAMCOLExposureApplier)} on {name} needs a {nameof(CAMCOLCameraSettings)} reference to read exposure settings.", this);
        warnedMissingSettings = true;
    }

    private static float ComputeAutoMeteringOffset(float meteredExposureOffset)
    {
        float sign = Mathf.Sign(meteredExposureOffset);
        float magnitude = Mathf.Max(0f, Mathf.Abs(meteredExposureOffset) - AutoMeteringDeadZone);
        float conservativeOffset = magnitude * AutoMeteringStrength * sign;
        return Mathf.Clamp(conservativeOffset, AutoMeteringFloor, AutoMeteringCeiling);
    }

    private void ApplyDepthOfField()
    {
        if (!depthOfField || !settings)
        {
            return;
        }

        depthOfField.active = true;
        depthOfField.mode.overrideState = true;
        depthOfField.mode.value = DepthOfFieldMode.Bokeh;

        depthOfField.focusDistance.overrideState = true;
        depthOfField.focusDistance.value = GetCurrentFocusDistance();

        depthOfField.aperture.overrideState = true;
        depthOfField.aperture.value = settings.Aperture;

        depthOfField.focalLength.overrideState = true;
        depthOfField.focalLength.value = GetCurrentFocalLength();
    }

    private float GetCurrentFocusDistance()
    {
        if (focusController)
        {
            return Mathf.Max(0.1f, focusController.FocusDistance);
        }

        return targetCamera ? Mathf.Max(0.1f, targetCamera.farClipPlane * 0.25f) : 10f;
    }

    private float GetCurrentFocalLength()
    {
        if (targetCamera && targetCamera.usePhysicalProperties)
        {
            return Mathf.Max(1f, targetCamera.focalLength);
        }

        return DefaultFocalLength;
    }

    private static float ComputeEv100(float aperture, float shutterSpeed, float iso)
    {
        aperture = Mathf.Max(0.01f, aperture);
        shutterSpeed = Mathf.Max(0.0001f, shutterSpeed);
        iso = Mathf.Max(1f, iso);

        return Mathf.Log((aperture * aperture) / shutterSpeed * 100f / iso, 2f);
    }
}
