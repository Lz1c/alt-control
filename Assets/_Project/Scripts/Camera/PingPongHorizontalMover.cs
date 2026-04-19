using UnityEngine;

[DisallowMultipleComponent]
public class PingPongHorizontalMover : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveDistance = 2f;
    [SerializeField] private float moveSpeed = 1f;
    [SerializeField] private bool useLocalPosition = true;

    private Vector3 startPosition;
    private float elapsedTime;

    private void Start()
    {
        startPosition = useLocalPosition ? transform.localPosition : transform.position;
    }

    private void OnEnable()
    {
        elapsedTime = 0f;
    }

    private void Update()
    {
        elapsedTime += Time.deltaTime * Mathf.Max(0f, moveSpeed);
        float offset = Mathf.Sin(elapsedTime) * moveDistance;
        Vector3 nextPosition = startPosition + Vector3.right * offset;

        if (useLocalPosition)
        {
            transform.localPosition = nextPosition;
            return;
        }

        transform.position = nextPosition;
    }

    private void OnValidate()
    {
        moveDistance = Mathf.Max(0f, moveDistance);
        moveSpeed = Mathf.Max(0f, moveSpeed);
    }
}
