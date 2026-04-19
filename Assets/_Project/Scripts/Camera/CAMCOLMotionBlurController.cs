using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class CAMCOLMotionBlurController : MonoBehaviour
{
    private const float ForwardBlurMetersForFullStrength = 1.25f;
    private const float SidewaysBlurMetersForFullStrength = 0.5f;
    private const float VerticalBlurMetersForFullStrength = 0.5f;
    private const float RollDegreesForFullStrength = 12f;
    private const float CompensationStrength = 0.45f;
    private const int MaxSubjectSamples = 8;
    private const float SubjectFeather = 0.3f;
    private const float SubjectBoundsScale = 1f;
    private const float SubjectMotionExpansion = 0.7f;

    public struct LocalMotionBlurSample
    {
        public Vector2 CenterUv;
        public Vector2 HalfSizeUv;
        public Vector2 MotionUv;
        public float BlendStrength;
        public float Feather;
    }

    [Header("References")]
    [Tooltip("Scene-level simulated camera data source. This can live on a separate controller object.")]
    [SerializeField] private CAMCOLCameraSettings settings;

    [Header("Subject Blur Tuning")]
    [SerializeField] private float subjectPixelsForFullBlur = 20f;
    [SerializeField] private float subjectMotionSensitivity = 2.5f;

    private Material photoProcessingMaterial;
    private readonly List<LocalMotionBlurSample> subjectSampleBuffer = new List<LocalMotionBlurSample>(MaxSubjectSamples);

    public float ExposureDuration => settings ? Mathf.Max(0f, settings.ShutterSpeed) : 0f;

    private void Reset()
    {
        settings = GetComponent<CAMCOLCameraSettings>();
    }

    private void OnValidate()
    {
        EnsureReferences();
        subjectPixelsForFullBlur = Mathf.Max(1f, subjectPixelsForFullBlur);
        subjectMotionSensitivity = Mathf.Max(0.1f, subjectMotionSensitivity);
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
        float radialStrength = Mathf.Clamp(forwardDelta / ForwardBlurMetersForFullStrength, -1f, 1f) * CompensationStrength;
        float rollStrength = Mathf.Clamp(roll / RollDegreesForFullStrength, -1f, 1f) * CompensationStrength;

        float finalMotionX = (-yaw * pixelsPerDegreeX + sidewaysPixels) * CompensationStrength;
        float finalMotionY = (pitch * pixelsPerDegreeY + verticalPixels) * CompensationStrength;
        return new Vector4(finalMotionX, finalMotionY, radialStrength, rollStrength);
    }

    public LocalMotionBlurSample[] CalculateSubjectMotionSamples(
        Camera targetCamera,
        CAMMotionBlurSubject[] subjects,
        CAMMotionBlurSubject.SubjectSnapshot[] startSnapshots,
        CAMMotionBlurSubject.SubjectSnapshot[] endSnapshots,
        int width,
        int height)
    {
        subjectSampleBuffer.Clear();

        if (!targetCamera || subjects == null || startSnapshots == null || endSnapshots == null)
        {
            return System.Array.Empty<LocalMotionBlurSample>();
        }

        int count = Mathf.Min(subjects.Length, startSnapshots.Length, endSnapshots.Length);
        float shutterDuration = Mathf.Max(ExposureDuration, 0f);
        for (int i = 0; i < count; i++)
        {
            CAMMotionBlurSubject subject = subjects[i];
            if (!subject)
            {
                continue;
            }

            if (!startSnapshots[i].IsValid || !endSnapshots[i].IsValid)
            {
                continue;
            }

            Bounds endBounds = endSnapshots[i].WorldBounds;
            Vector3 velocityWorld = endSnapshots[i].VelocityWorld * (subject.BlurMultiplier * subjectMotionSensitivity);
            Vector3 worldOffset = velocityWorld * shutterDuration;

            if (worldOffset.sqrMagnitude <= 0.0000001f && startSnapshots[i].IsValid)
            {
                worldOffset = endSnapshots[i].WorldBounds.center - startSnapshots[i].WorldBounds.center;
            }

            Bounds estimatedStartBounds = OffsetBounds(endBounds, -worldOffset);

            if (!TryProjectBounds(targetCamera, estimatedStartBounds, out Vector2 startCenterUv, out Vector2 startHalfSizeUv))
            {
                continue;
            }

            if (!TryProjectBounds(targetCamera, endBounds, out Vector2 endCenterUv, out Vector2 endHalfSizeUv))
            {
                continue;
            }

            Vector2 motionPixels = new Vector2(
                (endCenterUv.x - startCenterUv.x) * width,
                (endCenterUv.y - startCenterUv.y) * height);

            float magnitude = motionPixels.magnitude;
            if (magnitude < subject.MinimumScreenMotionPixels)
            {
                continue;
            }

            Vector2 motionUv = new Vector2(motionPixels.x / width, motionPixels.y / height);
            Vector2 halfSizeUv = Vector2.Max(startHalfSizeUv, endHalfSizeUv) * SubjectBoundsScale;
            halfSizeUv += new Vector2(Mathf.Abs(motionUv.x), Mathf.Abs(motionUv.y)) * SubjectMotionExpansion;
            halfSizeUv.x = Mathf.Clamp(halfSizeUv.x, 0.01f, 0.45f);
            halfSizeUv.y = Mathf.Clamp(halfSizeUv.y, 0.01f, 0.45f);

            subjectSampleBuffer.Add(new LocalMotionBlurSample
            {
                CenterUv = (startCenterUv + endCenterUv) * 0.5f,
                HalfSizeUv = halfSizeUv,
                MotionUv = motionUv,
                BlendStrength = Mathf.Clamp01(magnitude / subjectPixelsForFullBlur),
                Feather = SubjectFeather
            });
        }

        if (subjectSampleBuffer.Count <= 1)
        {
            return subjectSampleBuffer.ToArray();
        }

        subjectSampleBuffer.Sort((a, b) => b.MotionUv.sqrMagnitude.CompareTo(a.MotionUv.sqrMagnitude));
        if (subjectSampleBuffer.Count > MaxSubjectSamples)
        {
            subjectSampleBuffer.RemoveRange(MaxSubjectSamples, subjectSampleBuffer.Count - MaxSubjectSamples);
        }

        return subjectSampleBuffer.ToArray();
    }

    public RenderTexture ApplyPhotoMotionBlur(RenderTexture source, Vector4 motionSample, LocalMotionBlurSample[] subjectSamples)
    {
        if (!source || !EnsurePhotoProcessingMaterial())
        {
            return source;
        }

        RenderTexture current = source;
        bool ownsCurrent = false;

        if (motionSample != Vector4.zero)
        {
            Vector2 motionUv = new Vector2(motionSample.x / source.width, motionSample.y / source.height);
            RenderTexture cameraProcessed = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32);
            ConfigureGlobalPass(motionUv, motionSample.z, motionSample.w);
            Graphics.Blit(source, cameraProcessed, photoProcessingMaterial);
            current = cameraProcessed;
            ownsCurrent = true;
        }

        if (subjectSamples == null || subjectSamples.Length == 0)
        {
            return current;
        }

        for (int i = 0; i < subjectSamples.Length; i++)
        {
            LocalMotionBlurSample sample = subjectSamples[i];
            if (sample.BlendStrength <= 0f || sample.MotionUv.sqrMagnitude <= 0f)
            {
                continue;
            }

            RenderTexture next = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32);
            ConfigureLocalPass(sample);
            Graphics.Blit(current, next, photoProcessingMaterial);

            if (ownsCurrent)
            {
                RenderTexture.ReleaseTemporary(current);
            }

            current = next;
            ownsCurrent = true;
        }

        return current;
    }

    public void ApplyCpuFallback(Texture2D photo, Vector4 motionSample, LocalMotionBlurSample[] subjectSamples)
    {
        ApplyDirectionalCpuBlur(photo, new Vector2(motionSample.x, motionSample.y));
        ApplyRadialCpuBlur(photo, motionSample.z);
        ApplyRotationalCpuBlur(photo, motionSample.w);
        ApplyLocalizedCpuBlur(photo, subjectSamples);
    }

    private static void ApplyDirectionalCpuBlur(Texture2D photo, Vector2 motionPixels)
    {
        int radius = Mathf.Clamp(Mathf.RoundToInt(motionPixels.magnitude * 0.22f), 0, 12);
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
        int radius = Mathf.Clamp(Mathf.RoundToInt(Mathf.Abs(radialStrength) * 10f), 0, 10);
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
        int radius = Mathf.Clamp(Mathf.RoundToInt(Mathf.Abs(rollStrength) * 10f), 0, 10);
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

    private void ConfigureGlobalPass(Vector2 motionUv, float radialStrength, float rollStrength)
    {
        photoProcessingMaterial.SetVector("_NoiseParams", Vector4.zero);
        photoProcessingMaterial.SetVector("_MotionParams", new Vector4(
            motionUv.x,
            motionUv.y,
            Mathf.Clamp01(motionUv.magnitude * 80f),
            radialStrength));
        photoProcessingMaterial.SetVector("_RotationParams", new Vector4(rollStrength, 0f, 0f, 0f));
        photoProcessingMaterial.SetVector("_LocalBlurCenter", Vector4.zero);
        photoProcessingMaterial.SetVector("_LocalBlurHalfSize", Vector4.zero);
        photoProcessingMaterial.SetVector("_LocalBlurMotion", Vector4.zero);
        photoProcessingMaterial.SetVector("_LocalBlurSettings", Vector4.zero);
        photoProcessingMaterial.SetFloat("_NoiseSeed", 0f);
    }

    private void ConfigureLocalPass(LocalMotionBlurSample sample)
    {
        photoProcessingMaterial.SetVector("_NoiseParams", Vector4.zero);
        photoProcessingMaterial.SetVector("_MotionParams", Vector4.zero);
        photoProcessingMaterial.SetVector("_RotationParams", Vector4.zero);
        photoProcessingMaterial.SetVector("_LocalBlurCenter", new Vector4(sample.CenterUv.x, sample.CenterUv.y, 0f, 0f));
        photoProcessingMaterial.SetVector("_LocalBlurHalfSize", new Vector4(sample.HalfSizeUv.x, sample.HalfSizeUv.y, 0f, 0f));
        photoProcessingMaterial.SetVector("_LocalBlurMotion", new Vector4(sample.MotionUv.x, sample.MotionUv.y, 0f, 0f));
        photoProcessingMaterial.SetVector("_LocalBlurSettings", new Vector4(sample.BlendStrength, sample.Feather, 1f, 0f));
        photoProcessingMaterial.SetFloat("_NoiseSeed", 0f);
    }

    private static bool TryProjectBounds(Camera targetCamera, Bounds bounds, out Vector2 centerUv, out Vector2 halfSizeUv)
    {
        Vector3[] corners = new Vector3[8];
        Vector3 min = bounds.min;
        Vector3 max = bounds.max;
        corners[0] = new Vector3(min.x, min.y, min.z);
        corners[1] = new Vector3(max.x, min.y, min.z);
        corners[2] = new Vector3(min.x, max.y, min.z);
        corners[3] = new Vector3(max.x, max.y, min.z);
        corners[4] = new Vector3(min.x, min.y, max.z);
        corners[5] = new Vector3(max.x, min.y, max.z);
        corners[6] = new Vector3(min.x, max.y, max.z);
        corners[7] = new Vector3(max.x, max.y, max.z);

        bool hasPoint = false;
        Vector2 minUv = Vector2.one;
        Vector2 maxUv = Vector2.zero;
        for (int i = 0; i < corners.Length; i++)
        {
            Vector3 viewport = targetCamera.WorldToViewportPoint(corners[i]);
            if (viewport.z <= 0f)
            {
                continue;
            }

            Vector2 point = new Vector2(viewport.x, viewport.y);
            if (!hasPoint)
            {
                minUv = point;
                maxUv = point;
                hasPoint = true;
                continue;
            }

            minUv = Vector2.Min(minUv, point);
            maxUv = Vector2.Max(maxUv, point);
        }

        if (!hasPoint)
        {
            centerUv = default;
            halfSizeUv = default;
            return false;
        }

        minUv = Vector2.Max(minUv, Vector2.zero);
        maxUv = Vector2.Min(maxUv, Vector2.one);
        centerUv = (minUv + maxUv) * 0.5f;
        halfSizeUv = Vector2.Max((maxUv - minUv) * 0.5f, new Vector2(0.01f, 0.01f));
        return true;
    }

    private static Bounds OffsetBounds(Bounds bounds, Vector3 offset)
    {
        bounds.center += offset;
        return bounds;
    }

    private static void ApplyLocalizedCpuBlur(Texture2D photo, LocalMotionBlurSample[] subjectSamples)
    {
        if (subjectSamples == null || subjectSamples.Length == 0)
        {
            return;
        }

        int width = photo.width;
        int height = photo.height;
        Color32[] source = photo.GetPixels32();
        Color32[] working = new Color32[source.Length];
        System.Array.Copy(source, working, source.Length);

        for (int s = 0; s < subjectSamples.Length; s++)
        {
            LocalMotionBlurSample sample = subjectSamples[s];
            Vector2 motionPixels = new Vector2(sample.MotionUv.x * width, sample.MotionUv.y * height);
            int radius = Mathf.Clamp(Mathf.RoundToInt(motionPixels.magnitude * 0.8f), 0, 18);
            if (radius <= 0)
            {
                continue;
            }

            Vector2 direction = motionPixels.normalized;
            int minX = Mathf.Clamp(Mathf.FloorToInt((sample.CenterUv.x - sample.HalfSizeUv.x) * width), 0, width - 1);
            int maxX = Mathf.Clamp(Mathf.CeilToInt((sample.CenterUv.x + sample.HalfSizeUv.x) * width), 0, width - 1);
            int minY = Mathf.Clamp(Mathf.FloorToInt((sample.CenterUv.y - sample.HalfSizeUv.y) * height), 0, height - 1);
            int maxY = Mathf.Clamp(Mathf.CeilToInt((sample.CenterUv.y + sample.HalfSizeUv.y) * height), 0, height - 1);

            Color32[] output = new Color32[working.Length];
            System.Array.Copy(working, output, working.Length);

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float nx = Mathf.Abs(((x + 0.5f) / width - sample.CenterUv.x) / Mathf.Max(sample.HalfSizeUv.x, 0.0001f));
                    float ny = Mathf.Abs(((y + 0.5f) / height - sample.CenterUv.y) / Mathf.Max(sample.HalfSizeUv.y, 0.0001f));
                    float box = Mathf.Max(nx, ny);
                    float mask = 1f - Mathf.SmoothStep(1f - sample.Feather, 1f, box);
                    if (mask <= 0f)
                    {
                        continue;
                    }

                    float r = 0f;
                    float g = 0f;
                    float b = 0f;
                    float count = 0f;
                    for (int i = -radius; i <= radius; i++)
                    {
                        int sx = Mathf.Clamp(Mathf.RoundToInt(x + direction.x * i), 0, width - 1);
                        int sy = Mathf.Clamp(Mathf.RoundToInt(y + direction.y * i), 0, height - 1);
                        Color32 color = working[sy * width + sx];
                        r += color.r;
                        g += color.g;
                        b += color.b;
                        count += 1f;
                    }

                    int index = y * width + x;
                    Color32 original = working[index];
                    Color32 blurred = new Color32(
                        (byte)Mathf.RoundToInt(r / count),
                        (byte)Mathf.RoundToInt(g / count),
                        (byte)Mathf.RoundToInt(b / count),
                        original.a);
                    output[index] = Color32.Lerp(original, blurred, mask * sample.BlendStrength);
                }
            }

            working = output;
        }

        photo.SetPixels32(working);
        photo.Apply(false);
    }
}
