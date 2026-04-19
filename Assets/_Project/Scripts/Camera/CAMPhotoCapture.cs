using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class CAMPhotoCapture : MonoBehaviour
{
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

    [Header("Capture")]
    [SerializeField] private KeyCode halfPressKey = KeyCode.O;
    [SerializeField] private KeyCode captureKey = KeyCode.P;
    [SerializeField] private bool autoMeterOnHalfPress = true;
    [SerializeField] private bool autoFocusOnHalfPress = true;
    [SerializeField] private string photoFolderName = "photo";
    [SerializeField] private string fileNamePrefix = "photo";
    [SerializeField] private bool showPreviewAfterCapture = true;
    [SerializeField] private bool hidePreviewDuringCapture = true;

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

        Quaternion startRotation = motionBlurController
            ? motionBlurController.CaptureCameraRotation(targetCamera)
            : CaptureCameraRotation();
        Vector3 startPosition = motionBlurController
            ? motionBlurController.CaptureCameraPosition(targetCamera)
            : CaptureCameraPosition();

        float exposureDuration = motionBlurController ? motionBlurController.ExposureDuration : (settings ? settings.ShutterSpeed : 0f);
        if (exposureDuration > Time.deltaTime)
        {
            yield return new WaitForSeconds(exposureDuration);
        }

        yield return new WaitForEndOfFrame();

        int captureWidth = Screen.width;
        int captureHeight = Screen.height;
        Quaternion endRotation = motionBlurController
            ? motionBlurController.CaptureCameraRotation(targetCamera)
            : CaptureCameraRotation();
        Vector3 endPosition = motionBlurController
            ? motionBlurController.CaptureCameraPosition(targetCamera)
            : CaptureCameraPosition();
        Vector4 motionSample = motionBlurController
            ? motionBlurController.CalculateMotionSample(targetCamera, startRotation, endRotation, startPosition, endPosition, captureWidth, captureHeight)
            : Vector4.zero;
        Texture2D photo = CaptureProcessedPhoto(captureWidth, captureHeight, motionSample);

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

        isCapturing = false;
        hasPreparedShot = false;
        IsHalfPressActive = false;
    }

    private Texture2D CaptureProcessedPhoto(int width, int height, Vector4 motionSample)
    {
        RenderTexture sourceRt = CaptureCameraToRenderTexture(width, height);
        RenderTexture isoRt = isoController ? isoController.ApplyPhotoIso(sourceRt) : sourceRt;
        RenderTexture processedRt = motionBlurController ? motionBlurController.ApplyPhotoMotionBlur(isoRt, motionSample) : isoRt;

        Texture2D photo = ReadRenderTexture(processedRt, width, height);

        if (!isoController)
        {
            Debug.LogWarning($"{nameof(CAMPhotoCapture)} on {name} has no {nameof(CAMCOLIsoController)} reference, so captured photos will not get ISO post-processing.", this);
        }

        if (isoRt == sourceRt)
        {
            isoController?.ApplyCpuFallbackIso(photo);
        }

        if (processedRt == isoRt)
        {
            motionBlurController?.ApplyCpuFallback(photo, motionSample);
        }

        RenderTexture.ReleaseTemporary(sourceRt);
        if (isoRt != sourceRt && isoRt != processedRt)
        {
            RenderTexture.ReleaseTemporary(isoRt);
        }

        if (processedRt != sourceRt)
        {
            RenderTexture.ReleaseTemporary(processedRt);
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

        targetCamera.targetTexture = sourceRt;
        RenderTexture.active = sourceRt;
        GL.Clear(true, true, targetCamera.backgroundColor);
        targetCamera.Render();

        targetCamera.targetTexture = previousTargetTexture;
        targetCamera.enabled = previousCameraEnabled;
        RenderTexture.active = previousActive;

        return sourceRt;
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
