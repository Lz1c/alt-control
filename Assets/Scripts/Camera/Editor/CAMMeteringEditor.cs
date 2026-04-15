using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CAMMetering))]
public class CAMMeteringEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();

        using (new EditorGUI.DisabledScope(!Application.isPlaying))
        {
            if (GUILayout.Button("Meter Center Now"))
            {
                CAMMetering metering = (CAMMetering)target;
                metering.MeterCenterOnce();
            }
        }

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play Mode to meter the rendered camera image.", MessageType.Info);
        }
    }
}
