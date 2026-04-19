using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class CAMMotionBlurSubject : MonoBehaviour
{
    public struct SubjectSnapshot
    {
        public bool IsValid;
        public Bounds WorldBounds;
        public Vector3 VelocityWorld;
    }

    private static readonly List<CAMMotionBlurSubject> ActiveSubjects = new List<CAMMotionBlurSubject>();

    [Header("Subject")]
    [SerializeField] private Renderer[] subjectRenderers;
    [SerializeField] private bool autoCollectChildRenderers = true;
    [SerializeField] private float blurMultiplier = 1f;
    [SerializeField] private float minimumScreenMotionPixels = 2f;
    [SerializeField] private float boundsPadding = 0.05f;
    [SerializeField] private float velocitySmoothing = 12f;

    public float BlurMultiplier => Mathf.Max(0f, blurMultiplier);
    public float MinimumScreenMotionPixels => Mathf.Max(0f, minimumScreenMotionPixels);

    private bool hasMotionHistory;
    private Vector3 lastMotionPosition;
    private Vector3 smoothedVelocityWorld;

    private void Reset()
    {
        RefreshRenderers();
    }

    private void OnValidate()
    {
        blurMultiplier = Mathf.Max(0f, blurMultiplier);
        minimumScreenMotionPixels = Mathf.Max(0f, minimumScreenMotionPixels);
        boundsPadding = Mathf.Max(0f, boundsPadding);
        velocitySmoothing = Mathf.Max(0f, velocitySmoothing);

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

        InitializeMotionHistory();
    }

    private void OnDisable()
    {
        ActiveSubjects.Remove(this);
    }

    private void LateUpdate()
    {
        UpdateVelocityCache();
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
            WorldBounds = bounds,
            VelocityWorld = smoothedVelocityWorld
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

    private void InitializeMotionHistory()
    {
        if (!TryGetMotionPosition(out Vector3 currentPosition))
        {
            currentPosition = transform.position;
        }

        lastMotionPosition = currentPosition;
        smoothedVelocityWorld = Vector3.zero;
        hasMotionHistory = true;
    }

    private void UpdateVelocityCache()
    {
        if (!TryGetMotionPosition(out Vector3 currentPosition))
        {
            currentPosition = transform.position;
        }

        if (!hasMotionHistory)
        {
            lastMotionPosition = currentPosition;
            smoothedVelocityWorld = Vector3.zero;
            hasMotionHistory = true;
            return;
        }

        float dt = Mathf.Max(Time.deltaTime, 0.0001f);
        Vector3 instantaneousVelocity = (currentPosition - lastMotionPosition) / dt;
        float smoothingFactor = velocitySmoothing <= 0f
            ? 1f
            : 1f - Mathf.Exp(-velocitySmoothing * dt);
        smoothedVelocityWorld = Vector3.Lerp(smoothedVelocityWorld, instantaneousVelocity, smoothingFactor);
        lastMotionPosition = currentPosition;
    }

    private bool TryGetMotionPosition(out Vector3 position)
    {
        if (TryGetCombinedBounds(out Bounds bounds))
        {
            position = bounds.center;
            return true;
        }

        position = transform.position;
        return false;
    }
}
