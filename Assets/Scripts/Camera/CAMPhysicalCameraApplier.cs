using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public class CAMPhysicalCameraApplier : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Scene-level simulated camera data source. This can live on a separate controller object.")]
    [SerializeField] private CAMCOLCameraSettings settings;
    [SerializeField] private Camera targetCamera;

    private float lastIso = -1f;
    private float lastShutterSpeed = -1f;
    private float lastAperture = -1f;
    private bool warnedMissingSettings;

    private void Reset()
    {
        targetCamera = GetComponent<Camera>();
        settings = GetComponent<CAMCOLCameraSettings>();
    }

    private void OnEnable()
    {
        EnsureReferences();
        ApplyPhysicalCameraSettings();
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
            || !Mathf.Approximately(settings.Aperture, lastAperture))
        {
            ApplyPhysicalCameraSettings();
        }
    }

    private void OnValidate()
    {
        EnsureReferences();
        ApplyPhysicalCameraSettings();
    }

    public void ApplyPhysicalCameraSettings()
    {
        EnsureReferences();

        if (!settings || !targetCamera)
        {
            return;
        }

        targetCamera.usePhysicalProperties = true;
        targetCamera.iso = Mathf.RoundToInt(settings.Iso);
        targetCamera.shutterSpeed = settings.ShutterSpeed;
        targetCamera.aperture = settings.Aperture;

        lastIso = settings.Iso;
        lastShutterSpeed = settings.ShutterSpeed;
        lastAperture = settings.Aperture;
    }

    private void EnsureReferences()
    {
        if (!targetCamera)
        {
            targetCamera = GetComponent<Camera>();
        }

        if (!settings)
        {
            settings = GetComponent<CAMCOLCameraSettings>();
        }

        if (settings)
        {
            warnedMissingSettings = false;
        }
    }

    private void WarnMissingSettings()
    {
        if (warnedMissingSettings)
        {
            return;
        }

        Debug.LogWarning($"{nameof(CAMPhysicalCameraApplier)} on {name} needs a {nameof(CAMCOLCameraSettings)} reference to sync physical camera values.", this);
        warnedMissingSettings = true;
    }
}
