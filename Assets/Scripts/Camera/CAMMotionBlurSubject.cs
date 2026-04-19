using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class CAMMotionBlurSubject : MonoBehaviour
{
    public struct SubjectSnapshot
    {
        public bool IsValid;
        public Bounds WorldBounds;
    }

    private static readonly List<CAMMotionBlurSubject> ActiveSubjects = new List<CAMMotionBlurSubject>();

    [Header("Subject")]
    [SerializeField] private Renderer[] subjectRenderers;
    [SerializeField] private bool autoCollectChildRenderers = true;
    [SerializeField] private float blurMultiplier = 1f;
    [SerializeField] private float minimumScreenMotionPixels = 2f;
    [SerializeField] private float boundsPadding = 0.05f;

    public float BlurMultiplier => Mathf.Max(0f, blurMultiplier);
    public float MinimumScreenMotionPixels => Mathf.Max(0f, minimumScreenMotionPixels);

    private void Reset()
    {
        RefreshRenderers();
    }

    private void OnValidate()
    {
        blurMultiplier = Mathf.Max(0f, blurMultiplier);
        minimumScreenMotionPixels = Mathf.Max(0f, minimumScreenMotionPixels);
        boundsPadding = Mathf.Max(0f, boundsPadding);

        if (autoCollectChildRenderers && (subjectRenderers == null || subjectRenderers.Length == 0))
        {
            RefreshRenderers();
        }
    }

    private void OnEnable()
    {
        if (!ActiveSubjects.Contains(this))
        {
            ActiveSubjects.Add(this);
        }
    }

    private void OnDisable()
    {
        ActiveSubjects.Remove(this);
    }

    public SubjectSnapshot CaptureSnapshot()
    {
        if (!TryGetCombinedBounds(out Bounds bounds))
        {
            return default;
        }

        bounds.Expand(boundsPadding);
        return new SubjectSnapshot
        {
            IsValid = true,
            WorldBounds = bounds
        };
    }

    public static CAMMotionBlurSubject[] GetActiveSubjectsSnapshot()
    {
        ActiveSubjects.RemoveAll(subject => !subject);
        return ActiveSubjects.ToArray();
    }

    public static SubjectSnapshot[] CaptureSnapshots(CAMMotionBlurSubject[] subjects)
    {
        if (subjects == null || subjects.Length == 0)
        {
            return System.Array.Empty<SubjectSnapshot>();
        }

        SubjectSnapshot[] snapshots = new SubjectSnapshot[subjects.Length];
        for (int i = 0; i < subjects.Length; i++)
        {
            CAMMotionBlurSubject subject = subjects[i];
            snapshots[i] = subject ? subject.CaptureSnapshot() : default;
        }

        return snapshots;
    }

    private bool TryGetCombinedBounds(out Bounds combinedBounds)
    {
        Renderer[] renderers = subjectRenderers;
        if (renderers == null || renderers.Length == 0)
        {
            RefreshRenderers();
            renderers = subjectRenderers;
        }

        combinedBounds = default;
        bool hasBounds = false;
        if (renderers == null)
        {
            return false;
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (!renderer || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (!hasBounds)
            {
                combinedBounds = renderer.bounds;
                hasBounds = true;
                continue;
            }

            combinedBounds.Encapsulate(renderer.bounds);
        }

        return hasBounds;
    }

    private void RefreshRenderers()
    {
        subjectRenderers = autoCollectChildRenderers
            ? GetComponentsInChildren<Renderer>(true)
            : GetComponents<Renderer>();
    }
}
