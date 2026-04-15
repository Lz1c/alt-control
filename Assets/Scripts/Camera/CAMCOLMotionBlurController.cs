using UnityEngine;

[DisallowMultipleComponent]
public class CAMCOLMotionBlurController : MonoBehaviour
{
    private const float ForwardBlurMetersForFullStrength = 1.25f;
    private const float SidewaysBlurMetersForFullStrength = 0.5f;
    private const float VerticalBlurMetersForFullStrength = 0.5f;
    private const float RollDegreesForFullStrength = 12f;

    [Header("References")]
    [Tooltip("Scene-level simulated camera data source. This can live on a separate controller object.")]
    [SerializeField] private CAMCOLCameraSettings settings;

    private Material photoProcessingMaterial;

    public float ExposureDuration => settings ? Mathf.Max(0f, settings.ShutterSpeed) : 0f;

    private void Reset()
    {
        settings = GetComponent<CAMCOLCameraSettings>();
    }

    private void OnValidate()
    {
        EnsureReferences();
    }

    public Quaternion CaptureCameraRotation(Camera targetCamera)
    {
        return targetCamera ? targetCamera.transform.rotation : Quaternion.identity;
    }

    public Vector3 CaptureCameraPosition(Camera targetCamera)
    {
        return targetCamera ? targetCamera.transform.position : Vector3.zero;
    }

    public Vector4 CalculateMotionSample(
        Camera targetCamera,
        Quaternion startRotation,
        Quaternion endRotation,
        Vector3 startPosition,
        Vector3 endPosition,
        int width,
        int height)
    {
        if (!targetCamera)
        {
            return Vector4.zero;
        }

        Quaternion delta = endRotation * Quaternion.Inverse(startRotation);
        Vector3 euler = delta.eulerAngles;
        float yaw = Mathf.DeltaAngle(0f, euler.y);
        float pitch = Mathf.DeltaAngle(0f, euler.x);
        float roll = Mathf.DeltaAngle(0f, euler.z);

        float pixelsPerDegreeX = width / Mathf.Max(1f, targetCamera.fieldOfView * targetCamera.aspect);
        float pixelsPerDegreeY = height / Mathf.Max(1f, targetCamera.fieldOfView);
        Vector3 localDelta = Quaternion.Inverse(startRotation) * (endPosition - startPosition);
        float sidewaysPixels = Mathf.Clamp(localDelta.x / SidewaysBlurMetersForFullStrength, -1f, 1f) * width * 0.12f;
        float verticalPixels = Mathf.Clamp(localDelta.y / VerticalBlurMetersForFullStrength, -1f, 1f) * height * 0.12f;
        float forwardDelta = localDelta.z;
        float radialStrength = Mathf.Clamp(forwardDelta / ForwardBlurMetersForFullStrength, -1f, 1f);
        float rollStrength = Mathf.Clamp(roll / RollDegreesForFullStrength, -1f, 1f);

        float finalMotionX = -yaw * pixelsPerDegreeX + sidewaysPixels;
        float finalMotionY = pitch * pixelsPerDegreeY + verticalPixels;
        return new Vector4(finalMotionX, finalMotionY, radialStrength, rollStrength);
    }

    public RenderTexture ApplyPhotoMotionBlur(RenderTexture source, Vector4 motionSample)
    {
        if (!source || !EnsurePhotoProcessingMaterial())
        {
            return source;
        }

        Vector2 motionUv = new Vector2(motionSample.x / source.width, motionSample.y / source.height);
        RenderTexture processed = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32);

        photoProcessingMaterial.SetVector("_NoiseParams", Vector4.zero);
        photoProcessingMaterial.SetVector("_MotionParams", new Vector4(
            motionUv.x,
            motionUv.y,
            Mathf.Clamp01(motionUv.magnitude * 80f),
            motionSample.z));
        photoProcessingMaterial.SetVector("_RotationParams", new Vector4(motionSample.w, 0f, 0f, 0f));
        photoProcessingMaterial.SetFloat("_NoiseSeed", 0f);
        Graphics.Blit(source, processed, photoProcessingMaterial);

        return processed;
    }

    public void ApplyCpuFallback(Texture2D photo, Vector4 motionSample)
    {
        ApplyDirectionalCpuBlur(photo, new Vector2(motionSample.x, motionSample.y));
        ApplyRadialCpuBlur(photo, motionSample.z);
        ApplyRotationalCpuBlur(photo, motionSample.w);
    }

    private static void ApplyDirectionalCpuBlur(Texture2D photo, Vector2 motionPixels)
    {
        int radius = Mathf.Clamp(Mathf.RoundToInt(motionPixels.magnitude * 0.15f), 0, 8);
        if (radius <= 0)
        {
            return;
        }

        Color32[] source = photo.GetPixels32();
        Color32[] output = new Color32[source.Length];
        int width = photo.width;
        int height = photo.height;
        Vector2 direction = motionPixels.normalized;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float r = 0f;
                float g = 0f;
                float b = 0f;
                float count = 0f;

                for (int i = -radius; i <= radius; i++)
                {
                    int sx = Mathf.Clamp(Mathf.RoundToInt(x + direction.x * i), 0, width - 1);
                    int sy = Mathf.Clamp(Mathf.RoundToInt(y + direction.y * i), 0, height - 1);
                    Color32 sample = source[sy * width + sx];
                    r += sample.r;
                    g += sample.g;
                    b += sample.b;
                    count += 1f;
                }

                output[y * width + x] = new Color32(
                    (byte)Mathf.RoundToInt(r / count),
                    (byte)Mathf.RoundToInt(g / count),
                    (byte)Mathf.RoundToInt(b / count),
                    source[y * width + x].a);
            }
        }

        photo.SetPixels32(output);
        photo.Apply(false);
    }

    private static void ApplyRadialCpuBlur(Texture2D photo, float radialStrength)
    {
        int radius = Mathf.Clamp(Mathf.RoundToInt(Mathf.Abs(radialStrength) * 8f), 0, 8);
        if (radius <= 0)
        {
            return;
        }

        Color32[] source = photo.GetPixels32();
        Color32[] output = new Color32[source.Length];
        int width = photo.width;
        int height = photo.height;
        Vector2 center = new Vector2((width - 1) * 0.5f, (height - 1) * 0.5f);
        float directionSign = Mathf.Sign(radialStrength);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2 toPixel = new Vector2(x, y) - center;
                Vector2 radialDirection = toPixel.sqrMagnitude > 0.0001f ? toPixel.normalized * directionSign : Vector2.zero;
                float r = 0f;
                float g = 0f;
                float b = 0f;
                float count = 0f;

                for (int i = -radius; i <= radius; i++)
                {
                    int sx = Mathf.Clamp(Mathf.RoundToInt(x + radialDirection.x * i), 0, width - 1);
                    int sy = Mathf.Clamp(Mathf.RoundToInt(y + radialDirection.y * i), 0, height - 1);
                    Color32 sample = source[sy * width + sx];
                    r += sample.r;
                    g += sample.g;
                    b += sample.b;
                    count += 1f;
                }

                output[y * width + x] = new Color32(
                    (byte)Mathf.RoundToInt(r / count),
                    (byte)Mathf.RoundToInt(g / count),
                    (byte)Mathf.RoundToInt(b / count),
                    source[y * width + x].a);
            }
        }

        photo.SetPixels32(output);
        photo.Apply(false);
    }

    private static void ApplyRotationalCpuBlur(Texture2D photo, float rollStrength)
    {
        int radius = Mathf.Clamp(Mathf.RoundToInt(Mathf.Abs(rollStrength) * 8f), 0, 8);
        if (radius <= 0)
        {
            return;
        }

        Color32[] source = photo.GetPixels32();
        Color32[] output = new Color32[source.Length];
        int width = photo.width;
        int height = photo.height;
        Vector2 center = new Vector2((width - 1) * 0.5f, (height - 1) * 0.5f);
        float directionSign = Mathf.Sign(rollStrength);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2 toPixel = new Vector2(x, y) - center;
                Vector2 tangent = toPixel.sqrMagnitude > 0.0001f
                    ? new Vector2(-toPixel.y, toPixel.x).normalized * directionSign
                    : Vector2.zero;
                float r = 0f;
                float g = 0f;
                float b = 0f;
                float count = 0f;

                for (int i = -radius; i <= radius; i++)
                {
                    int sx = Mathf.Clamp(Mathf.RoundToInt(x + tangent.x * i), 0, width - 1);
                    int sy = Mathf.Clamp(Mathf.RoundToInt(y + tangent.y * i), 0, height - 1);
                    Color32 sample = source[sy * width + sx];
                    r += sample.r;
                    g += sample.g;
                    b += sample.b;
                    count += 1f;
                }

                output[y * width + x] = new Color32(
                    (byte)Mathf.RoundToInt(r / count),
                    (byte)Mathf.RoundToInt(g / count),
                    (byte)Mathf.RoundToInt(b / count),
                    source[y * width + x].a);
            }
        }

        photo.SetPixels32(output);
        photo.Apply(false);
    }

    private void EnsureReferences()
    {
        if (!settings)
        {
            settings = GetComponent<CAMCOLCameraSettings>();
        }
    }

    private bool EnsurePhotoProcessingMaterial()
    {
        if (photoProcessingMaterial)
        {
            return true;
        }

        Shader shader = Shader.Find("Hidden/Simulated Camera/Photo Processing");
        if (!shader)
        {
            Debug.LogWarning($"{nameof(CAMCOLMotionBlurController)} could not find photo processing shader. Falling back to CPU motion blur.", this);
            return false;
        }

        photoProcessingMaterial = new Material(shader)
        {
            hideFlags = HideFlags.HideAndDontSave
        };

        return true;
    }

    private void OnDestroy()
    {
        if (photoProcessingMaterial)
        {
            Destroy(photoProcessingMaterial);
        }
    }
}
