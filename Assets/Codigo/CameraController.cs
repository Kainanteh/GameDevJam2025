using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 10f;
    public float dragSpeed = 2f;
    public float margin = 2f;

    [Header("Smooth Movement")]
    public float smoothTime = 0.15f;

    private Vector3 dragOrigin;
    private Vector3 targetPosition;
    private Vector3 velocity = Vector3.zero;

    private float minX, maxX, minY, maxY;

    void Start()
    {
        SetBounds();
        targetPosition = transform.position;
    }

    void Update()
    {
        HandleInput();
        SmoothMove();
    }

    void HandleInput()
    {
        // Drag con botón central del ratón
        if (Input.GetMouseButtonDown(2))
        {
            dragOrigin = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        }

        if (Input.GetMouseButton(2))
        {
            Vector3 difference = dragOrigin - Camera.main.ScreenToWorldPoint(Input.mousePosition);
            targetPosition = ClampPosition(transform.position + difference);
        }

        // Movimiento con teclado (WASD)
        Vector3 direction = new Vector3(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"), 0f);
        if (direction.sqrMagnitude > 0.01f)
        {
            targetPosition = ClampPosition(transform.position + direction * moveSpeed * Time.deltaTime);
        }
    }

    void SmoothMove()
    {
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
    }

    Vector3 ClampPosition(Vector3 pos)
    {
        float clampedX = Mathf.Clamp(pos.x, minX, maxX);
        float clampedY = Mathf.Clamp(pos.y, minY, maxY);
        return new Vector3(clampedX, clampedY, pos.z);
    }

    public void SetBounds()
    {
        int width = GameManager.Instance.gridGenerator.width;
        int height = GameManager.Instance.gridGenerator.height;

        minX = 0 - margin;
        maxX = (width - 1) + margin;
        minY = 0 - margin;
        maxY = (height - 1) + margin;
    }
}
