using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CAMCOLCameraSettings))]
public class CAMCOLCameraSettingsEditor : Editor
{
    private SerializedProperty iso;
    private SerializedProperty isoLimits;
    private SerializedProperty shutterSpeed;
    private SerializedProperty shutterSpeedLimits;
    private SerializedProperty aperture;
    private SerializedProperty apertureLimits;
    private SerializedProperty exposureCompensation;
    private SerializedProperty exposureCompensationLimits;

    private void OnEnable()
    {
        iso = serializedObject.FindProperty("iso");
        isoLimits = serializedObject.FindProperty("isoLimits");
        shutterSpeed = serializedObject.FindProperty("shutterSpeed");
        shutterSpeedLimits = serializedObject.FindProperty("shutterSpeedLimits");
        aperture = serializedObject.FindProperty("aperture");
        apertureLimits = serializedObject.FindProperty("apertureLimits");
        exposureCompensation = serializedObject.FindProperty("exposureCompensation");
        exposureCompensationLimits = serializedObject.FindProperty("exposureCompensationLimits");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawIso();
        EditorGUILayout.Space(6f);
        DrawShutterSpeed();
        EditorGUILayout.Space(6f);
        DrawAperture();
        EditorGUILayout.Space(6f);
        DrawExposureCompensation();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawIso()
    {
        DrawLimits("ISO Limits", isoLimits, 1f);

        float min = Mathf.Max(1f, isoLimits.vector2Value.x);
        float max = Mathf.Max(min, isoLimits.vector2Value.y);
        int value = Mathf.RoundToInt(Mathf.Clamp(iso.floatValue, min, max));
        iso.floatValue = EditorGUILayout.IntSlider("ISO", value, Mathf.RoundToInt(min), Mathf.RoundToInt(max));
        EditorGUILayout.LabelField("Display", $"ISO {Mathf.RoundToInt(iso.floatValue)}");
    }

    private void DrawShutterSpeed()
    {
        DrawLimits("Shutter Limits", shutterSpeedLimits, 0.0001f);

        float min = Mathf.Max(0.0001f, shutterSpeedLimits.vector2Value.x);
        float max = Mathf.Max(min, shutterSpeedLimits.vector2Value.y);
        float value = Mathf.Clamp(shutterSpeed.floatValue, min, max);
        float stopMin = Mathf.Log(min, 2f);
        float stopMax = Mathf.Log(max, 2f);
        float stopValue = Mathf.Log(value, 2f);
        stopValue = EditorGUILayout.Slider("Shutter Speed", stopValue, stopMin, stopMax);
        shutterSpeed.floatValue = Mathf.Pow(2f, stopValue);
        EditorGUILayout.LabelField("Display", FormatShutterSpeed(shutterSpeed.floatValue));
    }

    private void DrawAperture()
    {
        DrawLimits("Aperture Limits", apertureLimits, 0.1f);

        float min = Mathf.Max(0.1f, apertureLimits.vector2Value.x);
        float max = Mathf.Max(min, apertureLimits.vector2Value.y);
        aperture.floatValue = EditorGUILayout.Slider("Aperture", Mathf.Clamp(aperture.floatValue, min, max), min, max);
        EditorGUILayout.LabelField("Display", $"f/{aperture.floatValue:0.#}");
    }

    private void DrawExposureCompensation()
    {
        Vector2 limits = exposureCompensationLimits.vector2Value;
        if (limits.x > limits.y)
        {
            limits = new Vector2(limits.y, limits.x);
        }

        exposureCompensationLimits.vector2Value = EditorGUILayout.Vector2Field("Exposure Comp Limits", limits);
        float min = limits.x;
        float max = Mathf.Max(min, limits.y);
        exposureCompensation.floatValue = EditorGUILayout.Slider("Exposure Compensation", Mathf.Clamp(exposureCompensation.floatValue, min, max), min, max);
        EditorGUILayout.LabelField("Display", $"{exposureCompensation.floatValue:+0.0;-0.0;0.0} EV");
    }

    private static void DrawLimits(string label, SerializedProperty limits, float floor)
    {
        Vector2 value = limits.vector2Value;
        value.x = Mathf.Max(floor, value.x);
        value.y = Mathf.Max(floor, value.y);

        if (value.x > value.y)
        {
            value = new Vector2(value.y, value.x);
        }

        limits.vector2Value = EditorGUILayout.Vector2Field(label, value);
    }

    private static string FormatShutterSpeed(float seconds)
    {
        if (seconds <= 0f)
        {
            return "0 s";
        }

        if (seconds < 1f)
        {
            return $"1/{Mathf.RoundToInt(1f / seconds)} s";
        }

        return $"{seconds:0.##} s";
    }
}
