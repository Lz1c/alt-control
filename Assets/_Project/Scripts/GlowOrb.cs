// GlowOrb.cs
// A self-illuminated sphere whose colour, emission strength and point-light intensity/range
// are all adjustable from one place in the Inspector (updates live in the editor).
// The single "color" drives both the sphere's emissive material (via a MaterialPropertyBlock,
// so it doesn't edit the shared material asset) and the attached Point Light.
//
// Setup: put this on a sphere that uses an Emission-enabled material (e.g. URP/Lit with
// Emission on), and give the same object (or a child) a Light component.

using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshRenderer))]
public class GlowOrb : MonoBehaviour
{
    [Header("Glow color (shared by sphere + light)")]
    public Color color = new Color(1f, 0.6f, 0.25f);

    [Header("Sphere self-illumination")]
    [Min(0f)] public float emissionIntensity = 4f;

    [Header("Point Light")]
    public bool driveLight = true;
    [Min(0f)] public float lightIntensity = 3f;
    [Min(0f)] public float lightRange = 8f;

    Light _light;
    MaterialPropertyBlock _mpb;
    static readonly int kEmission = Shader.PropertyToID("_EmissionColor");
    static readonly int kBaseColor = Shader.PropertyToID("_BaseColor");
    static readonly int kColorLegacy = Shader.PropertyToID("_Color");

    void OnEnable() { Apply(); }
    void OnValidate() { Apply(); }

    public void Apply()
    {
        var r = GetComponent<MeshRenderer>();
        if (r != null)
        {
            if (_mpb == null) _mpb = new MaterialPropertyBlock();
            r.GetPropertyBlock(_mpb);
            _mpb.SetColor(kBaseColor, color);
            _mpb.SetColor(kColorLegacy, color);
            _mpb.SetColor(kEmission, color * emissionIntensity);
            r.SetPropertyBlock(_mpb);
        }

        if (driveLight)
        {
            if (_light == null) _light = GetComponent<Light>();
            if (_light == null) _light = GetComponentInChildren<Light>();
            if (_light != null)
            {
                _light.color = color;
                _light.intensity = lightIntensity;
                _light.range = lightRange;
            }
        }
    }
}
