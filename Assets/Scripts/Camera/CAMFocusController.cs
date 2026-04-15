using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class CAMFocusController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Button focusButton;

    [Header("Focus")]
    [SerializeField] private KeyCode focusKey = KeyCode.None;
    [SerializeField] private float maxFocusDistance = 500f;
    [SerializeField] private LayerMask focusLayers = ~0;

    private bool warnedMissingCamera;

    public bool HasFocusLock { get; private set; }
    public float FocusDistance { get; private set; } = 10f;
    public Vector3 FocusPoint { get; private set; }

    private void Reset()
    {
        targetCamera = GetComponent<Camera>();
    }

    private void OnEnable()
    {
        EnsureReferences();

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
    }

    private void OnValidate()
    {
        maxFocusDistance = Mathf.Max(0.1f, maxFocusDistance);
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
            FocusDistance = hit.distance;
            HasFocusLock = true;
            return;
        }

        FocusPoint = centerRay.origin + centerRay.direction * maxFocusDistance;
        FocusDistance = maxFocusDistance;
        HasFocusLock = false;
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
