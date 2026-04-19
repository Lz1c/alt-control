using UnityEngine;

[DisallowMultipleComponent]
public class PingPongHorizontalMover : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveDistance = 2f;
    [SerializeField] private float moveSpeed = 1f;
    [SerializeField] private bool useLocalPosition = true;

    private Vector3 startPosition;
    private float currentOffset;
    private float direction = 1f;

    private void Start()
    {
        startPosition = useLocalPosition ? transform.localPosition : transform.position;
    }

    private void OnEnable()
    {
        currentOffset = 0f;
        direction = 1f;
    }

    private void Update()
    {
        float distance = Mathf.Max(0f, moveDistance);
        float speed = Mathf.Max(0f, moveSpeed);

        if (distance <= 0f || speed <= 0f)
        {
            ApplyPosition(startPosition);
            return;
        }

        currentOffset += direction * speed * Time.deltaTime;

        if (currentOffset > distance)
        {
            currentOffset = distance - (currentOffset - distance);
            direction = -1f;
        }
        else if (currentOffset < -distance)
        {
            currentOffset = -distance + (-distance - currentOffset);
            direction = 1f;
        }

        Vector3 nextPosition = startPosition + Vector3.right * currentOffset;
        ApplyPosition(nextPosition);
    }

    private void ApplyPosition(Vector3 nextPosition)
    {
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
