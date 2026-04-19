using UnityEngine;
using UnityEngine.UI;

public abstract class CAMMeteringBase : MonoBehaviour
{
    [Header("References")]
    [SerializeField] protected Camera targetCamera;
    [SerializeField] protected Button meteringButton;

    [Header("Metering")]
    [SerializeField] protected float targetMiddleGray = 0.18f;
    [SerializeField] protected KeyCode meteringKey = KeyCode.M;
    [SerializeField] protected int renderSampleSize = 256;

    protected Texture2D sampleTexture;
    protected bool warnedMissingCamera;

    public float CurrentLuminance { get; protected set; } = 0.18f;
    public float MeteredExposureOffset { get; protected set; }
    public bool HasValidReading { get; protected set; }
    public int ReadingVersion { get; protected set; }

    protected virtual void Reset()
    {
        targetCamera = GetComponent<Camera>();
    }

    protected virtual void OnEnable()
    {
        EnsureReferences();

        if (meteringButton)
        {
            meteringButton.onClick.AddListener(MeterCenterOnce);
        }
    }

    protected virtual void OnDisable()
    {
        if (meteringButton)
        {
            meteringButton.onClick.RemoveListener(MeterCenterOnce);
        }
    }

    protected virtual void Update()
    {
        if (meteringKey != KeyCode.None && Input.GetKeyDown(meteringKey))
        {
            MeterCenterOnce();
        }
    }

    protected virtual void OnValidate()
    {
        targetMiddleGray = Mathf.Max(0.001f, targetMiddleGray);
        renderSampleSize = Mathf.Clamp(renderSampleSize, 32, 1024);
        EnsureReferences();
    }

    public void MeterCenterOnce()
    {
        EnsureReferences();

        if (!targetCamera)
        {
            WarnMissingCamera();
            return;
        }

        warnedMissingCamera = false;

        int width = renderSampleSize;
        int height = Mathf.Max(32, Mathf.RoundToInt(renderSampleSize / Mathf.Max(0.1f, targetCamera.aspect)));
        EnsureSampleTexture(width, height);

        RenderTexture previousTargetTexture = targetCamera.targetTexture;
        RenderTexture previousActive = RenderTexture.active;
        RenderTexture sampleRt = RenderTexture.GetTemporary(width, height, 16, RenderTextureFormat.ARGB32);

        targetCamera.targetTexture = sampleRt;
        RenderTexture.active = sampleRt;
        GL.Clear(true, true, targetCamera.backgroundColor);
        targetCamera.Render();

        sampleTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        sampleTexture.Apply(false);

        targetCamera.targetTexture = previousTargetTexture;
        RenderTexture.active = previousActive;
        RenderTexture.ReleaseTemporary(sampleRt);

        CurrentLuminance = Mathf.Max(0.0001f, ComputeMeteredLuminance(sampleTexture.GetPixels32(), width, height));
        MeteredExposureOffset = Mathf.Log(Mathf.Max(0.001f, targetMiddleGray) / CurrentLuminance, 2f);
        HasValidReading = true;
        ReadingVersion++;
    }

    protected abstract float ComputeMeteredLuminance(Color32[] pixels, int width, int height);

    protected void EnsureReferences()
    {
        if (!targetCamera)
        {
            targetCamera = GetComponent<Camera>();
        }

        if (!targetCamera)
        {
            targetCamera = Camera.main;
        }
    }

    protected void EnsureSampleTexture(int width, int height)
    {
        if (sampleTexture && sampleTexture.width == width && sampleTexture.height == height)
        {
            return;
        }

        if (sampleTexture)
        {
            Destroy(sampleTexture);
        }

        sampleTexture = new Texture2D(width, height, TextureFormat.RGB24, false);
    }

    protected void WarnMissingCamera()
    {
        if (warnedMissingCamera)
        {
            return;
        }

        Debug.LogWarning($"{GetType().Name} on {name} needs a target Camera to meter scene brightness.", this);
        warnedMissingCamera = true;
    }

    protected static float ComputeLogAverageLuminance(Color32[] pixels)
    {
        double logSum = 0d;
        const float epsilon = 0.0001f;

        for (int i = 0; i < pixels.Length; i++)
        {
            float luminance = ComputeLuminance(pixels[i], epsilon);
            logSum += Mathf.Log(luminance);
        }

        return Mathf.Exp((float)(logSum / Mathf.Max(1, pixels.Length)));
    }

    protected static float ComputeLuminance(Color32 pixel, float epsilon = 0.0001f)
    {
        float r = Mathf.GammaToLinearSpace(pixel.r / 255f);
        float g = Mathf.GammaToLinearSpace(pixel.g / 255f);
        float b = Mathf.GammaToLinearSpace(pixel.b / 255f);
        return Mathf.Max(epsilon, r * 0.2126f + g * 0.7152f + b * 0.0722f);
    }

    protected virtual void OnDestroy()
    {
        if (sampleTexture)
        {
            Destroy(sampleTexture);
        }
    }
}
