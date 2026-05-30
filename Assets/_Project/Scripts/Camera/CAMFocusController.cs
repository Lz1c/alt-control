using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class CAMFocusController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Button focusButton;
    [SerializeField] private CAMCOLCameraSettings settings;

    [Header("Focus")]
    [SerializeField] private KeyCode focusKey = KeyCode.None;
    [SerializeField] private float maxFocusDistance = 500f;
    [SerializeField] private float focusTransitionDuration = 0.35f;
    [SerializeField] private LayerMask focusLayers = ~0;

    private bool warnedMissingCamera;
    private float targetFocusDistance = 10f;
    private float focusVelocity;

    public bool HasFocusLock { get; private set; }
    public float FocusDistance { get; private set; } = 10f;
    public Vector3 FocusPoint { get; private set; }
    public bool IsFocusTransitioning => !Mathf.Approximately(FocusDistance, targetFocusDistance);

    private void Reset()
    {
        targetCamera = GetComponent<Camera>();
        settings = GetComponent<CAMCOLCameraSettings>();
    }

    private void OnEnable()
    {
        EnsureReferences();
        InitializeFocusDistanceFromSettings();

        if (focusButton)
        {
            focusButton.onClick.AddListener(FocusCenterOnce);
        }
    }

    private void OnDisable()
    {
        if (focusButton)
        {
            focusButton.onClick.RemoveListener(FocusCenterOnce);
        }
    }

    private void Update()
    {
        if (focusKey != KeyCode.None && Input.GetKeyDown(focusKey))
        {
            FocusCenterOnce();
        }

        UpdateFocusTransition();
    }

    private void OnValidate()
    {
        maxFocusDistance = Mathf.Max(0.1f, maxFocusDistance);
        focusTransitionDuration = Mathf.Max(0f, focusTransitionDuration);
        EnsureReferences();
    }

    public void FocusCenterOnce()
    {
        EnsureReferences();

        if (!targetCamera)
        {
            WarnMissingCamera();
            return;
        }

        warnedMissingCamera = false;

        Ray centerRay = targetCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(centerRay, out RaycastHit hit, maxFocusDistance, focusLayers, QueryTriggerInteraction.Ignore))
        {
            FocusPoint = hit.point;
            SetTargetFocusDistance(hit.distance);
            HasFocusLock = true;
            return;
        }

        FocusPoint = centerRay.origin + centerRay.direction * maxFocusDistance;
        SetTargetFocusDistance(maxFocusDistance);
        HasFocusLock = false;
    }

    private void SetTargetFocusDistance(float value)
    {
        targetFocusDistance = Mathf.Max(0.1f, value);

        if (focusTransitionDuration <= 0f)
        {
            FocusDistance = targetFocusDistance;
            focusVelocity = 0f;
            SyncSettingsFocusDistance();
        }
    }

    private void UpdateFocusTransition()
    {
        if (focusTransitionDuration <= 0f)
        {
            FocusDistance = targetFocusDistance;
            SyncSettingsFocusDistance();
            return;
        }

        if (Mathf.Approximately(FocusDistance, targetFocusDistance))
        {
            return;
        }

        FocusDistance = Mathf.SmoothDamp(
            FocusDistance,
            targetFocusDistance,
            ref focusVelocity,
            focusTransitionDuration,
            Mathf.Infinity,
            Time.deltaTime);
        SyncSettingsFocusDistance();
    }

    private void EnsureReferences()
    {
        if (!targetCamera)
        {
            targetCamera = GetComponent<Camera>();
        }

        if (!targetCamera)
        {
            targetCamera = Camera.main;
        }

        if (!settings)
        {
            settings = GetComponent<CAMCOLCameraSettings>();
        }

        if (!settings && targetCamera)
        {
            settings = targetCamera.GetComponent<CAMCOLCameraSettings>();
        }
    }

    private void InitializeFocusDistanceFromSettings()
    {
        if (!settings)
        {
            return;
        }

        FocusDistance = Mathf.Max(0.1f, settings.FocusDistance);
        targetFocusDistance = FocusDistance;
        focusVelocity = 0f;
    }

    private void SyncSettingsFocusDistance()
    {
        settings?.SetFocusDistance(FocusDistance);
    }

    private void WarnMissingCamera()
    {
        if (warnedMissingCamera)
        {
            return;
        }

        Debug.LogWarning($"{nameof(CAMFocusController)} on {name} needs a target Camera to perform center focus.", this);
        warnedMissingCamera = true;
    }
}
