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
    private SerializedProperty focalLength;
    private SerializedProperty focalLengthLimits;
    private SerializedProperty focusDistance;
    private SerializedProperty focusDistanceLimits;
    private SerializedProperty focusClearRange;
    private SerializedProperty focusClearRangeLimits;
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
        focalLength = serializedObject.FindProperty("focalLength");
        focalLengthLimits = serializedObject.FindProperty("focalLengthLimits");
        focusDistance = serializedObject.FindProperty("focusDistance");
        focusDistanceLimits = serializedObject.FindProperty("focusDistanceLimits");
        focusClearRange = serializedObject.FindProperty("focusClearRange");
        focusClearRangeLimits = serializedObject.FindProperty("focusClearRangeLimits");
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
        DrawFocalLength();
        EditorGUILayout.Space(6f);
        DrawFocusDistance();
        EditorGUILayout.Space(6f);
        DrawFocusClearRange();
        EditorGUILayout.Space(6f);
        DrawExposureCompensation();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawIso()
    {
        DrawLimits("ISO Limits", isoLimits, 50f);

        float min = Mathf.Max(50f, isoLimits.vector2Value.x);
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

    private void DrawFocalLength()
    {
        DrawLimits("Focal Length Limits", focalLengthLimits, 1f);

        float min = Mathf.Max(1f, focalLengthLimits.vector2Value.x);
        float max = Mathf.Max(min, focalLengthLimits.vector2Value.y);
        focalLength.floatValue = EditorGUILayout.Slider("Focal Length", Mathf.Clamp(focalLength.floatValue, min, max), min, max);
        EditorGUILayout.LabelField("Display", $"{focalLength.floatValue:0.#} mm");
    }

    private void DrawFocusDistance()
    {
        DrawLimits("Focus Distance Limits", focusDistanceLimits, 0.1f);

        float min = Mathf.Max(0.1f, focusDistanceLimits.vector2Value.x);
        float max = Mathf.Max(min, focusDistanceLimits.vector2Value.y);
        focusDistance.floatValue = EditorGUILayout.Slider("Focus Distance", Mathf.Clamp(focusDistance.floatValue, min, max), min, max);
        EditorGUILayout.LabelField("Display", $"{focusDistance.floatValue:0.##} m");
    }

    private void DrawFocusClearRange()
    {
        DrawLimits("Focus Clear Range Limits", focusClearRangeLimits, 0.1f);

        float min = Mathf.Max(0.1f, focusClearRangeLimits.vector2Value.x);
        float max = Mathf.Max(min, focusClearRangeLimits.vector2Value.y);
        focusClearRange.floatValue = EditorGUILayout.Slider("Focus Range Tuning", Mathf.Clamp(focusClearRange.floatValue, min, max), min, max);
        serializedObject.ApplyModifiedProperties();
        CAMCOLCameraSettings settings = (CAMCOLCameraSettings)target;
        EditorGUILayout.LabelField("Tuning Multiplier", $"{Mathf.Max(0.01f, focusClearRange.floatValue / 2f):0.##}x");
        EditorGUILayout.LabelField("Physical Clear Range", $"{settings.EffectiveFocusClearRange:0.##} m");
        EditorGUILayout.LabelField("Clear Zone", $"{settings.EffectiveFocusNearDistance:0.##} m - {settings.EffectiveFocusFarDistance:0.##} m");
        serializedObject.Update();
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
