using System;
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class CAMExposureAccumulator : MonoBehaviour
{
    private const int DefaultSampleRate = 60;
    private const int DefaultMinSamples = 1;
    private const int DefaultMaxSamples = 6;

    [Header("References")]
    [Tooltip("Scene-level simulated camera data source. This can live on a separate controller object.")]
    [SerializeField] private CAMCOLCameraSettings settings;

    [Header("Sampling")]
    [SerializeField] private int captureSampleRate = DefaultSampleRate;
    [SerializeField] private int minAccumulationSamples = DefaultMinSamples;
    [SerializeField] private int maxAccumulationSamples = DefaultMaxSamples;

    private Material accumulationMaterial;

    public float ExposureDuration => settings ? Mathf.Max(0f, settings.ShutterSpeed) : 0f;
    public int CaptureSampleRate => Mathf.Max(1, captureSampleRate);
    public int MinAccumulationSamples => Mathf.Max(1, minAccumulationSamples);
    public int MaxAccumulationSamples => Mathf.Max(MinAccumulationSamples, maxAccumulationSamples);

    private void Reset()
    {
        settings = GetComponent<CAMCOLCameraSettings>();
    }

    private void OnValidate()
    {
        EnsureReferences();
        captureSampleRate = Mathf.Max(1, captureSampleRate);
        minAccumulationSamples = Mathf.Max(1, minAccumulationSamples);
        maxAccumulationSamples = Mathf.Max(minAccumulationSamples, maxAccumulationSamples);
    }

    public int GetSampleCount()
    {
        int sampleCount = Mathf.CeilToInt(ExposureDuration * CaptureSampleRate);
        return Mathf.Clamp(sampleCount, MinAccumulationSamples, MaxAccumulationSamples);
    }

    public IEnumerator CaptureAccumulatedExposure(Camera targetCamera, int width, int height, Action<RenderTexture> onCompleted)
    {
        EnsureReferences();

        int sampleCount = GetSampleCount();
        RenderTexture current = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGBHalf);
        RenderTexture next = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGBHalf);
        current.name = "Simulated Exposure Accumulator A";
        next.name = "Simulated Exposure Accumulator B";
        ClearRenderTexture(current);
        ClearRenderTexture(next);

        float sampleWeight = 1f / Mathf.Max(1, sampleCount);
        float sampleInterval = sampleCount > 1 ? ExposureDuration / (sampleCount - 1) : 0f;

        for (int i = 0; i < sampleCount; i++)
        {
            yield return new WaitForEndOfFrame();

            RenderTexture sample = CaptureCameraToRenderTexture(targetCamera, width, height);
            AccumulateSample(current, next, sample, sampleWeight);
            RenderTexture.ReleaseTemporary(sample);

            RenderTexture swap = current;
            current = next;
            next = swap;

            if (i < sampleCount - 1 && sampleInterval > 0f)
            {
                yield return new WaitForSeconds(sampleInterval);
            }
        }

        RenderTexture.ReleaseTemporary(next);
        onCompleted?.Invoke(current);
    }

    public RenderTexture ResolveToOutput(RenderTexture accumulator, int width, int height)
    {
        if (!accumulator)
        {
            return null;
        }

        RenderTexture resolved = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
        resolved.name = "Simulated Exposure Resolved";
        Graphics.Blit(accumulator, resolved);
        return resolved;
    }

    private void EnsureReferences()
    {
        if (!settings)
        {
            settings = GetComponent<CAMCOLCameraSettings>();
        }
    }

    private void AccumulateSample(RenderTexture accumulated, RenderTexture destination, RenderTexture sample, float weight)
    {
        if (!EnsureAccumulationMaterial())
        {
            Graphics.Blit(sample, destination);
            return;
        }

        accumulationMaterial.SetTexture("_AccumTex", accumulated);
        accumulationMaterial.SetTexture("_SampleTex", sample);
        accumulationMaterial.SetFloat("_SampleWeight", weight);
        Graphics.Blit(null, destination, accumulationMaterial);
    }

    private bool EnsureAccumulationMaterial()
    {
        if (accumulationMaterial)
        {
            return true;
        }

        Shader shader = Shader.Find("Hidden/Simulated Camera/Exposure Accumulation");
        if (!shader)
        {
            Debug.LogWarning($"{nameof(CAMExposureAccumulator)} could not find accumulation shader. Falling back to last-sample capture.", this);
            return false;
        }

        accumulationMaterial = new Material(shader)
        {
            hideFlags = HideFlags.HideAndDontSave
        };

        return true;
    }

    private static void ClearRenderTexture(RenderTexture target)
    {
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = target;
        GL.Clear(true, true, Color.clear);
        RenderTexture.active = previous;
    }

    private static RenderTexture CaptureCameraToRenderTexture(Camera targetCamera, int width, int height)
    {
        RenderTexture sourceRt = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
        sourceRt.name = "Simulated Exposure Sample";

        if (!targetCamera)
        {
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = null;
            Texture2D screenSource = new Texture2D(width, height, TextureFormat.RGB24, false);
            screenSource.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            screenSource.Apply(false);
            RenderTexture.active = previous;
            Graphics.Blit(screenSource, sourceRt);
            UnityEngine.Object.Destroy(screenSource);
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

    private void OnDestroy()
    {
        if (accumulationMaterial)
        {
            Destroy(accumulationMaterial);
        }
    }
}
