using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class CAMPhotoCapture : MonoBehaviour
{
    private const float BaseIso = 100f;
    private const float BaseShutterSpeed = 1f / 125f;
    private const float BaseAperture = 5.6f;

    [Header("References")]
    [Tooltip("Scene-level simulated camera data source. This can live on a separate controller object.")]
    [SerializeField] private CAMCOLCameraSettings settings;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private CAMMeteringBase metering;
    [SerializeField] private CAMFocusController focusController;
    [SerializeField] private CAMCOLIsoController isoController;
    [SerializeField] private CAMCOLMotionBlurController motionBlurController;
    [Tooltip("Optional RawImage used to display the processed photo after capture.")]
    [SerializeField] private RawImage previewImage;
    [Tooltip("Optional root object for the photo preview UI.")]
    [SerializeField] private GameObject previewRoot;
    [Tooltip("Optional full-screen black Image shown during the shutter interval.")]
    [SerializeField] private Image shutterBlackoutImage;

    [Header("Capture")]
    [SerializeField] private KeyCode halfPressKey = KeyCode.O;
    [SerializeField] private KeyCode captureKey = KeyCode.P;
    [SerializeField] private bool autoMeterOnHalfPress = true;
    [SerializeField] private bool autoFocusOnHalfPress = true;
    [SerializeField] private string photoFolderName = "photo";
    [SerializeField] private string fileNamePrefix = "photo";
    [SerializeField] private bool showPreviewAfterCapture = true;
    [SerializeField] private bool hidePreviewDuringCapture = true;
    [SerializeField] private float captureResolutionScale = 1f;

    [Header("Aurora Capture Response")]
    [Tooltip("How strongly aurora emissive sprites respond to camera exposure during the captured photo render only.")]
    [SerializeField, Range(0f, 2f)] private float auroraPhotoExposureStrength = 0.45f;
    [Tooltip("Extra influence from longer shutter speeds when photographing aurora. Real aurora photography usually benefits more from longer exposures than from simple global brightening.")]
    [SerializeField, Range(0f, 2f)] private float auroraShutterSensitivity = 0.7f;
    [Tooltip("Extra influence from higher ISO values when photographing aurora.")]
    [SerializeField, Range(0f, 2f)] private float auroraIsoSensitivity = 0.35f;
    [Tooltip("Limits how far the photo-only aurora response can be pushed.")]
    [SerializeField, Range(0f, 3f)] private float auroraPhotoExposureClamp = 1.6f;
    [Tooltip("Additional photo-only opacity boost so the aurora reads more clearly without simply becoming brighter.")]
    [SerializeField, Range(1f, 3f)] private float auroraPhotoAlphaBoost = 1.35f;
    [Tooltip("Reduces the apparent strength of edge fade during photo capture, making the shape hold together more clearly.")]
    [SerializeField, Range(0f, 1f)] private float auroraPhotoFadeSoftening = 0.28f;
    [Tooltip("Boosts the internal mask definition during photo capture so the aurora silhouette reads cleaner.")]
    [SerializeField, Range(0f, 1f)] private float auroraPhotoDefinitionBoost = 0.18f;
    [Tooltip("Stabilizes noisy fading edges during photo capture so the aurora outline reads more solid.")]
    [SerializeField, Range(0f, 1f)] private float auroraPhotoEdgeStability = 0.35f;
    [Tooltip("Reveals more internal ribbon detail during photo capture, closer to how long-exposure aurora photos hold structure.")]
    [SerializeField, Range(0f, 1f)] private float auroraPhotoContentBoost = 0.25f;

    private bool isCapturing;
    private Texture2D lastPreviewTexture;
    private bool hasPreparedShot;

    public bool IsHalfPressActive { get; private set; }
    public CAMMeteringBase Metering => metering;

    private void Reset()
    {
        settings = GetComponent<CAMCOLCameraSettings>();
        targetCamera = GetComponent<Camera>();
        metering = GetComponent<CAMMeteringBase>();
        focusController = GetComponent<CAMFocusController>();
        isoController = GetComponent<CAMCOLIsoController>();
        motionBlurController = GetComponent<CAMCOLMotionBlurController>();
        captureResolutionScale = 1f;
    }

    private void OnValidate()
    {
        captureResolutionScale = Mathf.Clamp(captureResolutionScale, 0.25f, 4f);
        auroraPhotoExposureStrength = Mathf.Max(0f, auroraPhotoExposureStrength);
        auroraShutterSensitivity = Mathf.Max(0f, auroraShutterSensitivity);
        auroraIsoSensitivity = Mathf.Max(0f, auroraIsoSensitivity);
        auroraPhotoExposureClamp = Mathf.Max(0f, auroraPhotoExposureClamp);
        auroraPhotoAlphaBoost = Mathf.Max(1f, auroraPhotoAlphaBoost);
        auroraPhotoFadeSoftening = Mathf.Clamp01(auroraPhotoFadeSoftening);
        auroraPhotoDefinitionBoost = Mathf.Clamp01(auroraPhotoDefinitionBoost);
        auroraPhotoEdgeStability = Mathf.Clamp01(auroraPhotoEdgeStability);
        auroraPhotoContentBoost = Mathf.Clamp01(auroraPhotoContentBoost);
    }

    private void Update()
    {
        if (halfPressKey != KeyCode.None && Input.GetKeyDown(halfPressKey))
        {
            StartHalfPress();
        }

        if (halfPressKey != KeyCode.None && Input.GetKeyUp(halfPressKey))
        {
            ReleaseHalfPress();
        }

        if (Input.GetKeyDown(captureKey))
        {
            FullPressShutter();
        }
    }

    public void StartHalfPress()
    {
        IsHalfPressActive = true;
        HalfPressShutter();
    }

    public void ReleaseHalfPress()
    {
        if (isCapturing)
        {
            return;
        }

        IsHalfPressActive = false;
        hasPreparedShot = false;
    }

    public void HalfPressShutter()
    {
        EnsureReferences();

        if (autoMeterOnHalfPress)
        {
            metering?.MeterCenterOnce();
        }

        if (autoFocusOnHalfPress)
        {
            focusController?.FocusCenterOnce();
        }

        hasPreparedShot = true;
    }

    public void FullPressShutter()
    {
        if (!hasPreparedShot)
        {
            StartHalfPress();
        }

        CapturePhoto();
    }

    public void CapturePhoto()
    {
        if (!isCapturing)
        {
            StartCoroutine(CapturePhotoRoutine());
        }
    }

    private IEnumerator CapturePhotoRoutine()
    {
        isCapturing = true;

        EnsureReferences();

        bool restorePreviewRoot = previewRoot && previewRoot.activeSelf;
        if (hidePreviewDuringCapture && previewRoot)
        {
            previewRoot.SetActive(false);
        }

        bool restoreBlackout = shutterBlackoutImage && shutterBlackoutImage.gameObject.activeSelf;
        if (shutterBlackoutImage)
        {
            shutterBlackoutImage.color = new Color(0f, 0f, 0f, 1f);
            shutterBlackoutImage.gameObject.SetActive(true);
        }

        Quaternion startRotation = motionBlurController
            ? motionBlurController.CaptureCameraRotation(targetCamera)
            : CaptureCameraRotation();
        Vector3 startPosition = motionBlurController
            ? motionBlurController.CaptureCameraPosition(targetCamera)
            : CaptureCameraPosition();
        CAMMotionBlurSubject[] motionSubjects = CAMMotionBlurSubject.GetActiveSubjectsSnapshot();
        CAMMotionBlurSubject.SubjectSnapshot[] startSubjectSnapshots = CAMMotionBlurSubject.CaptureSnapshots(motionSubjects);
        int captureWidth = Mathf.Max(1, Mathf.RoundToInt(Screen.width * captureResolutionScale));
        int captureHeight = Mathf.Max(1, Mathf.RoundToInt(Screen.height * captureResolutionScale));
        float exposureDuration = motionBlurController ? motionBlurController.ExposureDuration : (settings ? settings.ShutterSpeed : 0f);
        if (exposureDuration > Time.deltaTime)
        {
            yield return new WaitForSeconds(exposureDuration);
        }

        yield return new WaitForEndOfFrame();

        Quaternion endRotation = motionBlurController
            ? motionBlurController.CaptureCameraRotation(targetCamera)
            : CaptureCameraRotation();
        Vector3 endPosition = motionBlurController
            ? motionBlurController.CaptureCameraPosition(targetCamera)
            : CaptureCameraPosition();
        Vector4 motionSample = motionBlurController
            ? motionBlurController.CalculateMotionSample(targetCamera, startRotation, endRotation, startPosition, endPosition, captureWidth, captureHeight)
            : Vector4.zero;
        CAMMotionBlurSubject.SubjectSnapshot[] endSubjectSnapshots = CAMMotionBlurSubject.CaptureSnapshots(motionSubjects);
        CAMCOLMotionBlurController.LocalMotionBlurSample[] subjectMotionSamples = motionBlurController
            ? motionBlurController.CalculateSubjectMotionSamples(targetCamera, motionSubjects, startSubjectSnapshots, endSubjectSnapshots, captureWidth, captureHeight)
            : Array.Empty<CAMCOLMotionBlurController.LocalMotionBlurSample>();

        if (shutterBlackoutImage)
        {
            shutterBlackoutImage.gameObject.SetActive(restoreBlackout);
        }

        Texture2D photo = CaptureProcessedPhoto(captureWidth, captureHeight, motionSample, subjectMotionSamples);

        string savedPath = SavePhoto(photo);
        Debug.Log($"Saved simulated photo to {savedPath}", this);

        if (showPreviewAfterCapture && previewImage)
        {
            if (lastPreviewTexture)
            {
                Destroy(lastPreviewTexture);
            }

            lastPreviewTexture = photo;
            previewImage.texture = lastPreviewTexture;

            if (previewRoot)
            {
                previewRoot.SetActive(true);
            }
        }
        else
        {
            Destroy(photo);

            if (hidePreviewDuringCapture && previewRoot)
            {
                previewRoot.SetActive(restorePreviewRoot);
            }
        }

        if (shutterBlackoutImage)
        {
            shutterBlackoutImage.gameObject.SetActive(restoreBlackout);
        }

        isCapturing = false;
        hasPreparedShot = false;
        IsHalfPressActive = false;
    }

    private Texture2D CaptureProcessedPhoto(
        int width,
        int height,
        Vector4 motionSample,
        CAMCOLMotionBlurController.LocalMotionBlurSample[] subjectMotionSamples)
    {
        RenderTexture sourceRt = CaptureCameraToRenderTexture(width, height);
        RenderTexture motionRt = motionBlurController ? motionBlurController.ApplyPhotoMotionBlur(sourceRt, motionSample, subjectMotionSamples) : sourceRt;
        RenderTexture isoRt = isoController ? isoController.ApplyPhotoIso(motionRt) : motionRt;

        Texture2D photo = ReadRenderTexture(isoRt, width, height);

        if (!isoController)
        {
            Debug.LogWarning($"{nameof(CAMPhotoCapture)} on {name} has no {nameof(CAMCOLIsoController)} reference, so captured photos will not get ISO post-processing.", this);
        }

        if (motionRt == sourceRt)
        {
            motionBlurController?.ApplyCpuFallback(photo, motionSample, subjectMotionSamples);
        }

        if (isoRt == motionRt)
        {
            isoController?.ApplyCpuFallbackIso(photo);
        }

        if (sourceRt)
        {
            RenderTexture.ReleaseTemporary(sourceRt);
        }

        if (motionRt != sourceRt && motionRt != isoRt)
        {
            RenderTexture.ReleaseTemporary(motionRt);
        }

        if (isoRt != sourceRt && isoRt != motionRt)
        {
            RenderTexture.ReleaseTemporary(isoRt);
        }

        return photo;
    }

    private RenderTexture CaptureCameraToRenderTexture(int width, int height)
    {
        RenderTexture sourceRt = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
        sourceRt.name = "Simulated Photo Source";

        if (!targetCamera)
        {
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = null;
            Texture2D screenSource = new Texture2D(width, height, TextureFormat.RGB24, false);
            screenSource.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            screenSource.Apply(false);
            RenderTexture.active = previous;
            Graphics.Blit(screenSource, sourceRt);
            Destroy(screenSource);
            return sourceRt;
        }

        RenderTexture previousTargetTexture = targetCamera.targetTexture;
        RenderTexture previousActive = RenderTexture.active;
        bool previousCameraEnabled = targetCamera.enabled;
        float previousAuroraExposure = Shader.GetGlobalFloat("_SimulatedCameraExposureEV");
        float previousAuroraMultiplier = Shader.GetGlobalFloat("_SimulatedCameraExposureMultiplier");
        float previousAuroraAlphaBoost = Shader.GetGlobalFloat("_SimulatedAuroraPhotoAlphaBoost");
        float previousAuroraFadeSoftening = Shader.GetGlobalFloat("_SimulatedAuroraPhotoFadeSoftening");
        float previousAuroraDefinitionBoost = Shader.GetGlobalFloat("_SimulatedAuroraPhotoDefinitionBoost");
        float previousAuroraEdgeStability = Shader.GetGlobalFloat("_SimulatedAuroraPhotoEdgeStability");
        float previousAuroraContentBoost = Shader.GetGlobalFloat("_SimulatedAuroraPhotoContentBoost");

        float auroraExposure = ComputeAuroraPhotoExposure();
        float auroraClarity = ComputeAuroraPhotoClarity();
        Shader.SetGlobalFloat("_SimulatedCameraExposureEV", auroraExposure);
        Shader.SetGlobalFloat("_SimulatedCameraExposureMultiplier", Mathf.Pow(2f, auroraExposure));
        Shader.SetGlobalFloat("_SimulatedAuroraPhotoAlphaBoost", Mathf.Lerp(1f, auroraPhotoAlphaBoost, auroraClarity));
        Shader.SetGlobalFloat("_SimulatedAuroraPhotoFadeSoftening", auroraPhotoFadeSoftening * auroraClarity);
        Shader.SetGlobalFloat("_SimulatedAuroraPhotoDefinitionBoost", auroraPhotoDefinitionBoost * auroraClarity);
        Shader.SetGlobalFloat("_SimulatedAuroraPhotoEdgeStability", auroraPhotoEdgeStability * auroraClarity);
        Shader.SetGlobalFloat("_SimulatedAuroraPhotoContentBoost", auroraPhotoContentBoost * auroraClarity);

        targetCamera.targetTexture = sourceRt;
        RenderTexture.active = sourceRt;
        GL.Clear(true, true, targetCamera.backgroundColor);
        targetCamera.Render();

        targetCamera.targetTexture = previousTargetTexture;
        targetCamera.enabled = previousCameraEnabled;
        RenderTexture.active = previousActive;
        Shader.SetGlobalFloat("_SimulatedCameraExposureEV", previousAuroraExposure);
        Shader.SetGlobalFloat("_SimulatedCameraExposureMultiplier", previousAuroraMultiplier);
        Shader.SetGlobalFloat("_SimulatedAuroraPhotoAlphaBoost", previousAuroraAlphaBoost);
        Shader.SetGlobalFloat("_SimulatedAuroraPhotoFadeSoftening", previousAuroraFadeSoftening);
        Shader.SetGlobalFloat("_SimulatedAuroraPhotoDefinitionBoost", previousAuroraDefinitionBoost);
        Shader.SetGlobalFloat("_SimulatedAuroraPhotoEdgeStability", previousAuroraEdgeStability);
        Shader.SetGlobalFloat("_SimulatedAuroraPhotoContentBoost", previousAuroraContentBoost);

        return sourceRt;
    }

    private float ComputeAuroraPhotoExposure()
    {
        if (!settings)
        {
            return 0f;
        }

        float baseEv = ComputeEv100(BaseAperture, BaseShutterSpeed, BaseIso);
        float currentEv = ComputeEv100(settings.Aperture, settings.ShutterSpeed, settings.Iso);
        float manualExposure = baseEv - currentEv + settings.ExposureCompensation;

        float shutterStops = Mathf.Max(0f, Mathf.Log(settings.ShutterSpeed / BaseShutterSpeed, 2f));
        float isoStops = Mathf.Max(0f, Mathf.Log(settings.Iso / BaseIso, 2f));
        float photoWeightedExposure =
            manualExposure * auroraPhotoExposureStrength
            + shutterStops * auroraShutterSensitivity
            + isoStops * auroraIsoSensitivity;

        return Mathf.Clamp(photoWeightedExposure, -auroraPhotoExposureClamp, auroraPhotoExposureClamp);
    }

    private float ComputeAuroraPhotoClarity()
    {
        if (!settings)
        {
            return 0f;
        }

        float shutterStops = Mathf.Max(0f, Mathf.Log(settings.ShutterSpeed / BaseShutterSpeed, 2f));
        float isoStops = Mathf.Max(0f, Mathf.Log(settings.Iso / BaseIso, 2f));
        float apertureStops = Mathf.Max(0f, Mathf.Log(BaseAperture / Mathf.Max(0.01f, settings.Aperture), 2f));
        float clarityStops = shutterStops * 0.55f + isoStops * 0.25f + apertureStops * 0.2f;
        return Mathf.Clamp01(clarityStops / 4f);
    }

    private static float ComputeEv100(float aperture, float shutterSpeed, float iso)
    {
        aperture = Mathf.Max(0.01f, aperture);
        shutterSpeed = Mathf.Max(0.0001f, shutterSpeed);
        iso = Mathf.Max(1f, iso);
        return Mathf.Log((aperture * aperture) / shutterSpeed * 100f / iso, 2f);
    }

    private static Texture2D ReadRenderTexture(RenderTexture renderTexture, int width, int height)
    {
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = renderTexture;
        Texture2D photo = new Texture2D(width, height, TextureFormat.RGB24, false);
        photo.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        photo.Apply(false);
        RenderTexture.active = previous;
        return photo;
    }

    private string SavePhoto(Texture2D photo)
    {
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string folder = Path.Combine(projectRoot, photoFolderName);
        Directory.CreateDirectory(folder);

        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
        string path = Path.Combine(folder, $"{fileNamePrefix}_{timestamp}.png");
        File.WriteAllBytes(path, photo.EncodeToPNG());
        return path;
    }

    private void EnsureReferences()
    {
        if (!settings)
        {
            settings = GetComponent<CAMCOLCameraSettings>();
        }

        if (!targetCamera)
        {
            targetCamera = GetComponent<Camera>();
        }

        if (!targetCamera)
        {
            targetCamera = Camera.main;
        }

        if (!metering)
        {
            metering = GetComponent<CAMMeteringBase>();
        }

        if (!focusController)
        {
            focusController = GetComponent<CAMFocusController>();
        }

        if (!isoController)
        {
            isoController = GetComponent<CAMCOLIsoController>();
        }

        if (!motionBlurController)
        {
            motionBlurController = GetComponent<CAMCOLMotionBlurController>();
        }
    }

    private Quaternion CaptureCameraRotation()
    {
        return targetCamera ? targetCamera.transform.rotation : Quaternion.identity;
    }

    private Vector3 CaptureCameraPosition()
    {
        return targetCamera ? targetCamera.transform.position : Vector3.zero;
    }
}
