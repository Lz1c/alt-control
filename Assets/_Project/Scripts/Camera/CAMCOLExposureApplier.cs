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
    private const float DefaultFocusClearRange = 2f;

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

    [Header("Auto Exposure")]
    [Tooltip("Minimum and maximum automatic exposure offset applied from scene metering during half-press style auto exposure correction.")]
    [SerializeField] private Vector2 autoMeteringOffsetLimits = new Vector2(-1.5f, 1.5f);

    [Header("Realtime Exposure Response")]
    [Tooltip("How strongly the computed exposure affects the luminance-aware fullscreen exposure pass.")]
    [SerializeField, Range(0f, 1f)] private float realtimePostExposureStrength = 0.45f;
    [Tooltip("Brightness level where realtime exposure starts to visibly affect pixels. Lower values brighten more of the image, higher values preserve darkness.")]
    [SerializeField, Range(0f, 1f)] private float luminanceExposureThreshold = 0.06f;
    [Tooltip("How gradually the luminance-aware exposure fades in above the threshold.")]
    [SerializeField, Range(0.01f, 1f)] private float luminanceExposureSoftness = 0.24f;

    private ColorAdjustments colorAdjustments;
    private DepthOfField depthOfField;
    private float lastIso = -1f;
    private float lastShutterSpeed = -1f;
    private float lastAperture = -1f;
    private float lastExposureCompensation = float.NaN;
    private float lastFocusDistance = -1f;
    private float lastFocusClearRange = -1f;
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
        ResetAuroraExposureGlobals();
        ApplyExposure(true);
    }

    private void OnDisable()
    {
        ResetAuroraExposureGlobals();
        ResetLuminanceExposureGlobals();
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
            || !Mathf.Approximately(settings.FocusClearRange, lastFocusClearRange)
            || !Mathf.Approximately(GetCurrentFocalLength(), lastFocalLength)
            || HasMeteringChanged())
        {
            ApplyExposure(true);
        }
    }

    private void OnValidate()
    {
        SortLimits(ref autoMeteringOffsetLimits);
        realtimePostExposureStrength = Mathf.Clamp01(realtimePostExposureStrength);
        luminanceExposureThreshold = Mathf.Clamp01(luminanceExposureThreshold);
        luminanceExposureSoftness = Mathf.Max(0.01f, luminanceExposureSoftness);
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
                autoMeteringOffset = ComputeAutoMeteringOffset(metering.MeteredExposureOffset, AutoMeteringFloor, AutoMeteringCeiling);
                lastAppliedMeteringVersion = metering.ReadingVersion;
            }
        }
        else
        {
            autoMeteringOffset = 0f;
            lastAppliedMeteringVersion = -1;
        }

        float totalExposure = manualExposure + autoMeteringOffset;
        float realtimePostExposure = Mathf.Clamp(totalExposure * realtimePostExposureStrength, PostExposureFloor, PostExposureCeiling);

        colorAdjustments.active = true;
        colorAdjustments.postExposure.overrideState = true;
        colorAdjustments.postExposure.value = 0f;

        ResetAuroraExposureGlobals();
        Shader.SetGlobalFloat("_SimulatedLuminanceExposureEV", realtimePostExposure);
        Shader.SetGlobalFloat("_SimulatedLuminanceExposureThreshold", luminanceExposureThreshold);
        Shader.SetGlobalFloat("_SimulatedLuminanceExposureSoftness", luminanceExposureSoftness);
        ApplyFocusIntervalGlobals();

        ApplyDepthOfField();

        lastIso = settings.Iso;
        lastShutterSpeed = settings.ShutterSpeed;
        lastAperture = settings.Aperture;
        lastExposureCompensation = settings.ExposureCompensation;
        lastFocusDistance = GetCurrentFocusDistance();
        lastFocusClearRange = settings.FocusClearRange;
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

        if (targetCamera)
        {
            targetCamera.depthTextureMode |= DepthTextureMode.Depth;
            UniversalAdditionalCameraData cameraData = targetCamera.GetComponent<UniversalAdditionalCameraData>();
            if (cameraData)
            {
                cameraData.requiresDepthTexture = true;
            }
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
        if (colorAdjustments)
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

        profile.TryGet(out depthOfField);

        return colorAdjustments;
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

    private float AutoMeteringFloor => autoMeteringOffsetLimits.x;
    private float AutoMeteringCeiling => autoMeteringOffsetLimits.y;

    private static void ResetAuroraExposureGlobals()
    {
        Shader.SetGlobalFloat("_SimulatedCameraExposureEV", 0f);
        Shader.SetGlobalFloat("_SimulatedCameraExposureMultiplier", 1f);
        Shader.SetGlobalFloat("_SimulatedAuroraPhotoAlphaBoost", 1f);
        Shader.SetGlobalFloat("_SimulatedAuroraPhotoFadeSoftening", 0f);
        Shader.SetGlobalFloat("_SimulatedAuroraPhotoDefinitionBoost", 0f);
        Shader.SetGlobalFloat("_SimulatedAuroraPhotoEdgeStability", 0f);
        Shader.SetGlobalFloat("_SimulatedAuroraPhotoContentBoost", 0f);
    }

    private static void ResetLuminanceExposureGlobals()
    {
        Shader.SetGlobalFloat("_SimulatedLuminanceExposureEV", 0f);
        Shader.SetGlobalFloat("_SimulatedLuminanceExposureThreshold", 0.06f);
        Shader.SetGlobalFloat("_SimulatedLuminanceExposureSoftness", 0.24f);
        Shader.SetGlobalFloat("_SimulatedFocusDistance", 10f);
        Shader.SetGlobalFloat("_SimulatedFocusClearRange", DefaultFocusClearRange);
        Shader.SetGlobalFloat("_SimulatedFocusNearDistance", 9f);
        Shader.SetGlobalFloat("_SimulatedFocusFarDistance", 11f);
        Shader.SetGlobalFloat("_SimulatedFocusBlurStrength", 0f);
        Shader.SetGlobalFloat("_SimulatedFocusBlurRadius", 0f);
        Shader.SetGlobalFloat("_SimulatedFocusBokehHighlightBoost", 0f);
    }

    private static float ComputeAutoMeteringOffset(float meteredExposureOffset, float floor, float ceiling)
    {
        float sign = Mathf.Sign(meteredExposureOffset);
        float magnitude = Mathf.Max(0f, Mathf.Abs(meteredExposureOffset) - AutoMeteringDeadZone);
        float conservativeOffset = magnitude * AutoMeteringStrength * sign;
        return Mathf.Clamp(conservativeOffset, floor, ceiling);
    }

    private static void SortLimits(ref Vector2 limits)
    {
        if (limits.x <= limits.y)
        {
            return;
        }

        limits = new Vector2(limits.y, limits.x);
    }

    private void ApplyDepthOfField()
    {
        if (!depthOfField)
        {
            return;
        }

        depthOfField.active = false;
    }

    private void ApplyFocusIntervalGlobals()
    {
        float focusDistance = GetCurrentFocusDistance();
        float focalLength = GetCurrentFocalLength();
        float aperture = Mathf.Max(0.1f, settings.Aperture);
        float effectiveFocusClearRange = Mathf.Max(0.1f, settings.EffectiveFocusClearRange);
        float effectiveFocusNearDistance = Mathf.Max(0.1f, settings.EffectiveFocusNearDistance);
        float effectiveFocusFarDistance = Mathf.Max(effectiveFocusNearDistance, settings.EffectiveFocusFarDistance);
        float apertureBlur = Mathf.Clamp(BaseAperture / aperture, 0.05f, 6f);
        float focalLengthBlur = Mathf.Clamp(focalLength / DefaultFocalLength, 0.25f, 4f);
        float focusRangeBlur = Mathf.Clamp(DefaultFocusClearRange / effectiveFocusClearRange, 0.2f, 4f);
        float blurStrength = Mathf.Clamp(apertureBlur * focalLengthBlur * focusRangeBlur, 0f, 8f);
        float bokehHighlightBoost = Mathf.Clamp((apertureBlur - 0.75f) * 1.2f, 0.5f, 4f);

        Shader.SetGlobalFloat("_SimulatedFocusDistance", focusDistance);
        Shader.SetGlobalFloat("_SimulatedFocusClearRange", effectiveFocusClearRange);
        Shader.SetGlobalFloat("_SimulatedFocusNearDistance", effectiveFocusNearDistance);
        Shader.SetGlobalFloat("_SimulatedFocusFarDistance", effectiveFocusFarDistance);
        Shader.SetGlobalFloat("_SimulatedFocusBlurStrength", blurStrength);
        Shader.SetGlobalFloat("_SimulatedFocusBlurRadius", 7f);
        Shader.SetGlobalFloat("_SimulatedFocusBokehHighlightBoost", bokehHighlightBoost);
    }

    private float GetCurrentFocusDistance()
    {
        if (settings)
        {
            return Mathf.Max(0.1f, settings.FocusDistance);
        }

        if (focusController && focusController.HasFocusLock)
        {
            return Mathf.Max(0.1f, focusController.FocusDistance);
        }

        return targetCamera ? Mathf.Max(0.1f, targetCamera.farClipPlane * 0.25f) : 10f;
    }

    private float GetCurrentFocalLength()
    {
        if (settings)
        {
            return Mathf.Max(1f, settings.FocalLength);
        }

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
