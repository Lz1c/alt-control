using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[DisallowMultipleComponent]
public class CAMCOLIsoController : MonoBehaviour
{
    private const float BaseIso = 100f;
    private const float BaseShutterSpeed = 1f / 125f;
    private const float GrainIntensityAtIso6400 = 0.8f;
    private const float GrainCurvePower = 1.15f;
    private const float GrainResponse = 0.8f;
    private const float LuminanceNoiseAtIso6400 = 0.1f;
    private const float ChromaNoiseAtIso6400 = 0.075f;
    private const float DarkAreaResponse = 0.85f;
    private const float ChromaBlockSize = 4f;

    [Header("References")]
    [Tooltip("Scene-level simulated camera data source. This can live on a separate controller object.")]
    [SerializeField] private CAMCOLCameraSettings settings;
    [SerializeField] private Volume targetVolume;

    [Header("Noise Thresholds")]
    [SerializeField] private float isoNoiseThreshold = 6400f;
    [SerializeField] private float shutterNoiseThreshold = 1f;
    [SerializeField] private float maxSupportedIsoNoise = 25600f;
    [SerializeField] private float maxSupportedShutterNoise = 8f;

    private FilmGrain filmGrain;
    private Material photoProcessingMaterial;
    private bool warnedMissingSettings;
    private bool warnedMissingVolume;
    private bool warnedMissingProfile;

    private void Reset()
    {
        settings = GetComponent<CAMCOLCameraSettings>();
    }

    private void OnEnable()
    {
        ApplyRealtimeGrain(true);
    }

    private void Update()
    {
        ApplyRealtimeGrain(true);
    }

    private void OnValidate()
    {
        isoNoiseThreshold = Mathf.Max(BaseIso, isoNoiseThreshold);
        shutterNoiseThreshold = Mathf.Max(0.0001f, shutterNoiseThreshold);
        maxSupportedIsoNoise = Mathf.Max(isoNoiseThreshold, maxSupportedIsoNoise);
        maxSupportedShutterNoise = Mathf.Max(shutterNoiseThreshold, maxSupportedShutterNoise);
        ApplyRealtimeGrain(false);
    }

    public RenderTexture ApplyPhotoIso(RenderTexture source)
    {
        if (!source || !EnsurePhotoProcessingMaterial())
        {
            return source;
        }

        float iso = settings ? settings.Iso : BaseIso;
        float shutterSpeed = settings ? settings.ShutterSpeed : BaseShutterSpeed;
        float noiseAmount = ComputeNoiseAmount(iso, shutterSpeed, isoNoiseThreshold, shutterNoiseThreshold, maxSupportedIsoNoise, maxSupportedShutterNoise);
        RenderTexture processed = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32);

        photoProcessingMaterial.SetVector("_NoiseParams", new Vector4(
            LuminanceNoiseAtIso6400 * noiseAmount,
            ChromaNoiseAtIso6400 * noiseAmount,
            DarkAreaResponse,
            ChromaBlockSize));
        photoProcessingMaterial.SetVector("_MotionParams", Vector4.zero);
        photoProcessingMaterial.SetFloat("_NoiseSeed", UnityEngine.Random.value * 1000f);
        Graphics.Blit(source, processed, photoProcessingMaterial);

        return processed;
    }

    public void ApplyCpuFallbackIso(Texture2D photo)
    {
        float iso = settings ? settings.Iso : BaseIso;
        float shutterSpeed = settings ? settings.ShutterSpeed : BaseShutterSpeed;
        float noiseAmount = ComputeNoiseAmount(iso, shutterSpeed, isoNoiseThreshold, shutterNoiseThreshold, maxSupportedIsoNoise, maxSupportedShutterNoise);
        float luminanceNoise = LuminanceNoiseAtIso6400 * noiseAmount;
        float chromaNoise = ChromaNoiseAtIso6400 * noiseAmount;

        if (luminanceNoise <= 0f && chromaNoise <= 0f)
        {
            return;
        }

        Color32[] pixels = photo.GetPixels32();
        int width = photo.width;
        int height = photo.height;
        int seed = Environment.TickCount;
        float inverseChromaBlockSize = 1f / ChromaBlockSize;
        float luminanceNoise255 = luminanceNoise * 255f;
        float chromaNoise255 = chromaNoise * 255f;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                Color32 pixel = pixels[index];

                float r = pixel.r;
                float g = pixel.g;
                float b = pixel.b;

                float luminance = (r * 0.2126f + g * 0.7152f + b * 0.0722f) / 255f;
                float darkWeight = Mathf.Lerp(1f, 1f - Mathf.Sqrt(Mathf.Clamp01(luminance)), DarkAreaResponse);
                float sensorWeight = 0.25f + darkWeight * 0.75f;

                float fineNoise = GaussianNoise(x, y, seed) * luminanceNoise255 * sensorWeight;
                float blockX = x * inverseChromaBlockSize;
                float blockY = y * inverseChromaBlockSize;
                float chromaR = SmoothBlockNoise(blockX, blockY, seed + 31);
                float chromaB = SmoothBlockNoise(blockX, blockY, seed + 97);

                float redShift = chromaR * chromaNoise255 * sensorWeight;
                float blueShift = chromaB * chromaNoise255 * sensorWeight;
                float greenShift = -(redShift + blueShift) * 0.35f;

                r = Mathf.Clamp(r + fineNoise + redShift, 0f, 255f);
                g = Mathf.Clamp(g + fineNoise + greenShift, 0f, 255f);
                b = Mathf.Clamp(b + fineNoise + blueShift, 0f, 255f);

                pixels[index] = new Color32(
                    (byte)Mathf.RoundToInt(r),
                    (byte)Mathf.RoundToInt(g),
                    (byte)Mathf.RoundToInt(b),
                    pixel.a);
            }
        }

        photo.SetPixels32(pixels);
        photo.Apply(false);
    }

    public void ApplyRealtimeGrain()
    {
        ApplyRealtimeGrain(true);
    }

    private void ApplyRealtimeGrain(bool logWarnings)
    {
        EnsureReferences();

        if (!settings || !EnsureFilmGrain(logWarnings))
        {
            return;
        }

        filmGrain.active = true;
        filmGrain.type.overrideState = true;
        filmGrain.intensity.overrideState = true;
        filmGrain.response.overrideState = true;
        filmGrain.response.value = GrainResponse;

        float noiseAmount = ComputeNoiseAmount(settings.Iso, settings.ShutterSpeed, isoNoiseThreshold, shutterNoiseThreshold, maxSupportedIsoNoise, maxSupportedShutterNoise);
        filmGrain.type.value = GetGrainType(noiseAmount);
        filmGrain.intensity.value = Mathf.Lerp(0f, GrainIntensityAtIso6400, noiseAmount);
    }

    private void EnsureReferences()
    {
        if (!settings)
        {
            settings = GetComponent<CAMCOLCameraSettings>();
        }

        if (settings)
        {
            warnedMissingSettings = false;
        }
        else if (!warnedMissingSettings)
        {
            Debug.LogWarning($"{nameof(CAMCOLIsoController)} on {name} needs a {nameof(CAMCOLCameraSettings)} reference to read ISO.", this);
            warnedMissingSettings = true;
        }
    }

    private bool EnsureFilmGrain(bool logWarnings)
    {
        if (filmGrain)
        {
            return true;
        }

        if (!targetVolume)
        {
            if (logWarnings && !warnedMissingVolume)
            {
                Debug.LogWarning($"{nameof(CAMCOLIsoController)} on {name} needs a target Volume to apply realtime grain.", this);
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
                Debug.LogWarning($"{nameof(CAMCOLIsoController)} on {name} could not find a Volume Profile on {targetVolume.name}.", this);
                warnedMissingProfile = true;
            }

            return false;
        }

        warnedMissingProfile = false;

        if (!profile.TryGet(out filmGrain))
        {
            filmGrain = profile.Add<FilmGrain>(true);
        }

        return filmGrain;
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
            Debug.LogWarning($"{nameof(CAMCOLIsoController)} could not find photo processing shader. Falling back to CPU photo processing.", this);
            return false;
        }

        photoProcessingMaterial = new Material(shader)
        {
            hideFlags = HideFlags.HideAndDontSave
        };

        return true;
    }

    private static float ComputeNoiseAmount(
        float iso,
        float shutterSpeed,
        float isoThreshold,
        float shutterThreshold,
        float maxIsoNoise,
        float maxShutterNoise)
    {
        float isoNoise = 0f;
        if (iso > isoThreshold)
        {
            float isoStops = Mathf.Log(Mathf.Max(iso, isoThreshold) / isoThreshold, 2f);
            float isoRangeStops = Mathf.Log(maxIsoNoise / isoThreshold, 2f);
            isoNoise = Mathf.Clamp01(isoRangeStops > 0f ? isoStops / isoRangeStops : 0f);
        }

        float shutterNoise = 0f;
        if (shutterSpeed > shutterThreshold)
        {
            float slowShutterStops = Mathf.Log(Mathf.Max(shutterSpeed, shutterThreshold) / shutterThreshold, 2f);
            float slowShutterRangeStops = Mathf.Log(maxShutterNoise / shutterThreshold, 2f);
            shutterNoise = Mathf.Clamp01(slowShutterRangeStops > 0f ? slowShutterStops / slowShutterRangeStops : 0f);
        }

        return Mathf.Clamp01(Mathf.Pow(isoNoise, GrainCurvePower) + shutterNoise * 0.35f);
    }

    private static FilmGrainLookup GetGrainType(float noiseAmount)
    {
        if (noiseAmount < 0.2f)
        {
            return FilmGrainLookup.Thin1;
        }

        if (noiseAmount < 0.45f)
        {
            return FilmGrainLookup.Thin2;
        }

        if (noiseAmount < 0.7f)
        {
            return FilmGrainLookup.Medium1;
        }

        if (noiseAmount < 0.9f)
        {
            return FilmGrainLookup.Medium2;
        }

        return FilmGrainLookup.Medium3;
    }

    private static float GaussianNoise(int x, int y, int seed)
    {
        float a = Hash01(x, y, seed);
        float b = Hash01(x + 17, y - 29, seed + 101);
        float c = Hash01(x - 43, y + 53, seed + 211);
        float d = Hash01(x + 79, y + 7, seed + 307);

        return (a + b + c + d - 2f) * 0.5f;
    }

    private static float SmoothBlockNoise(float x, float y, int seed)
    {
        int x0 = Mathf.FloorToInt(x);
        int y0 = Mathf.FloorToInt(y);
        float tx = SmoothStep01(x - x0);
        float ty = SmoothStep01(y - y0);

        float a = HashSigned(x0, y0, seed);
        float b = HashSigned(x0 + 1, y0, seed);
        float c = HashSigned(x0, y0 + 1, seed);
        float d = HashSigned(x0 + 1, y0 + 1, seed);

        float top = Mathf.Lerp(a, b, tx);
        float bottom = Mathf.Lerp(c, d, tx);
        return Mathf.Lerp(top, bottom, ty);
    }

    private static float SmoothStep01(float value)
    {
        value = Mathf.Clamp01(value);
        return value * value * (3f - 2f * value);
    }

    private static float HashSigned(int x, int y, int seed)
    {
        return Hash01(x, y, seed) * 2f - 1f;
    }

    private static float Hash01(int x, int y, int seed)
    {
        unchecked
        {
            uint hash = (uint)seed;
            hash ^= (uint)x * 374761393u;
            hash = (hash << 13) | (hash >> 19);
            hash ^= (uint)y * 668265263u;
            hash *= 1274126177u;
            hash ^= hash >> 16;
            return (hash & 0x00FFFFFF) / 16777215f;
        }
    }

    private void OnDestroy()
    {
        if (photoProcessingMaterial)
        {
            Destroy(photoProcessingMaterial);
        }
    }
}
